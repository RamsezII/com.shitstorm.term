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
                {
                    ++read_i;
                    return true;
                }
            return false;
        }

        public bool TryReadMatch(in string match, in StringComparison comparison = StringComparison.Ordinal) => TryReadMatch(match, ref read_i, comparison);
        public bool TryReadMatch(in string match, ref int read_i, in StringComparison comparison = StringComparison.Ordinal)
        {
            int temp_i = read_i;
            SkipEmpties(ref temp_i);
            int match_start_i = temp_i;

            bool matched = TryRead(ref temp_i, out string output) && output.Equals(match, comparison);

            // Completion needs the bounds of the token being tested, including on
            // a mismatch. Expose those bounds only while registering the candidate
            // so an unsuccessful look-ahead does not consume the real reader.
            int previous_start_i = start_i;
            int previous_read_i = this.read_i;
            start_i = match_start_i;
            this.read_i = temp_i;
            AddCompletions(output, match);

            if (matched)
            {
                read_i = temp_i;
                return true;
            }

            start_i = previous_start_i;
            this.read_i = previous_read_i;
            return false;
        }
    }
}
