using System;
using System.Collections.Generic;
using System.Linq;

namespace _TERM_
{
    partial class CmdReader
    {
        public class OptionsInput
        {
            internal readonly Dictionary<char, CmdOption> short_options = new();
            internal readonly Dictionary<string, CmdOption> long_options = new(StringComparer.OrdinalIgnoreCase);

            //----------------------------------------------------------------------------------------------------------

            public OptionsInput(params CmdOption[] options)
            {
                foreach (var option in options)
                {
                    if (option.short_name != 0)
                        short_options.Add(option.short_name, option);
                    long_options.Add(option.long_name, option);
                }
            }
        }

        //----------------------------------------------------------------------------------------------------------

        internal int TryReadOptions(Dictionary<CmdOption, object> output, in OptionsInput input)
        {
            output.Clear();

            int count = 0;
            bool read_b = true;

            do
            {
                SkipEmpties(true);
                int read_i = this.read_i;
                read_b = TryRead(ref read_i, out string read);

                if (!read_b || read.Length == 0 || read[0] == '-')
                {
                    this.read_i = read_i;
                    if (IsOnCompletion())
                        AddCompletions(read, input.long_options.Keys.Where(long_name => !output.ContainsKey(long_name)).Select(long_name => $"--{long_name}"));
                }

                if (read == null || read.Length <= 1)
                    return count;

                if (read.Length > 0 && read[0] == '-')
                {
                    if (output.Count == input.long_options.Count)
                    {
                        Error($"Did not expect anymore options");
                        return count;
                    }

                    // --
                    if (read.Length > 1 && read[1] == '-')
                    {
                        string long_name = read[2..];
                        if (input.long_options.TryGetValue(long_name, out var option))
                        {
                            if (output.ContainsKey(option))
                            {
                                Error($"Option '{long_name}' already present");
                                return count;
                            }
                            this.read_i = read_i;
                            ClearCompletions();
                            output.Add(option, option.function?.Invoke(this));
                            ++count;
                            if (IsOnCompletion())
                                return count;
                        }
                        else
                        {
                            Error($"Did not expect option '{long_name}'");
                            return count;
                        }
                    }
                    // -
                    else
                    {
                        string flags = read[1..];
                        HashSet<char> chars = new(flags);
                        if (flags.Length != chars.Count)
                        {
                            Error($"Duplicate flags in '{flags}'");
                            return count;
                        }

                        this.read_i = read_i;
                        ClearCompletions();

                        // Validate the complete group before invoking callbacks so
                        // "-fz" cannot apply "f" before discovering an unknown "z".
                        for (int flag_i = 0; flag_i < flags.Length; ++flag_i)
                        {
                            char c = flags[flag_i];
                            if (!input.short_options.TryGetValue(c, out var option))
                            {
                                Error($"unexpected option '{c}'");
                                return count;
                            }
                            else if (output.ContainsKey(option))
                            {
                                Error($"Option '{c}' already present");
                                return count;
                            }
                            else if (option.function != null && flag_i < flags.Length - 1)
                            {
                                // Once a callback starts reading, the following token belongs
                                // to that option. Requiring it to be last keeps grouped flags
                                // deterministic: "-fp value" is valid, "-pf value" is not.
                                Error($"Option '{c}' must be the last flag in '{read}' because it reads a value");
                                return count;
                            }
                        }

                        foreach (char c in flags)
                        {
                            var option = input.short_options[c];
                            output.Add(option, option.function?.Invoke(this));
                            ++count;
                        }

                        // The whole group, and the last flag's value if it has one,
                        // are now registered. Do not reinterpret the cursor as a new option.
                        if (IsOnCompletion())
                            return count;
                    }
                }
                else
                    return count;
            }
            while (read_b);

            return count;
        }
    }
}
