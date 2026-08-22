using System;

namespace _TERM_
{
    partial class CmdReader
    {
        public bool TryReadMatch(in char match) => TryReadMatch(match, ref read_i);
        public bool TryReadMatch(in char match, ref int read_i)
        {
            SkipEmpties(ref read_i);
            if (read_i < cursor && read_i < line.Length)
                if (line[read_i] == match)
                    return true;
            return false;
        }

        public bool TryReadMatch(in string match, in StringComparison comparison = StringComparison.Ordinal) => TryReadMatch(match, ref read_i, comparison);
        public bool TryReadMatch(in string match, ref int read_i, in StringComparison comparison = StringComparison.Ordinal)
        {
            int temp_i = read_i;

            if (TryRead(ref temp_i, out string output))
                if (output.Equals(match, comparison))
                {
                    this.read_i = temp_i;
                    AddCompletions(output, match);
                    return true;
                }

            AddCompletions(output, match);
            return false;
        }
    }
}