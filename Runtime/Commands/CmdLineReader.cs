using System;
using System.Text;

namespace _TERM_
{
    public enum CmdTypes : byte
    {
        Complete,
        Check,
        Execute,
    }

    public struct CmdLineReader
    {
        public readonly string line;
        public readonly int cursor;
        public int start_i, read_i;
        readonly StringBuilder error_sb;
        public readonly string GetError => error_sb != null && error_sb.Length > 0 ? error_sb.ToString() : null;

        //----------------------------------------------------------------------------------------------------------

        public CmdLineReader(in string line, in int cursor = 0)
        {
            this.line = line;
            this.cursor = cursor;
            start_i = 0;
            read_i = 0;
            error_sb = new();
        }

        //----------------------------------------------------------------------------------------------------------

        public readonly void AddError(in string error) => error_sb.AppendLine(error);
        public readonly void WriteError(in string error, in bool force = false)
        {
            if (error_sb.Length == 0)
                error_sb.Append(error);
            else if (force)
            {
                error_sb.Clear();
                error_sb.Append(error);
            }
        }

        public void SkipEmpties() => SkipEmpties(ref read_i);
        public readonly void SkipEmpties(ref int read_i)
        {
            while (read_i < line.Length && line[read_i] == ' ')
                ++read_i;
        }

        public void SkipNoneEmpties() => SkipNoneEmpties(ref read_i);
        public readonly void SkipNoneEmpties(ref int read_i)
        {
            while (read_i < line.Length && line[read_i] != ' ')
                ++read_i;
        }

        public bool TryRead(out string output)
        {
            SkipEmpties();

            if (read_i < line.Length)
            {
                start_i = read_i;
                SkipNoneEmpties();

                if (read_i > start_i)
                {
                    output = line[start_i..read_i];
                    SkipEmpties();
                    return true;
                }
            }

            output = null;
            return false;
        }

        public readonly bool HasNext() => HasNext(out _);
        public readonly bool HasNext(out int next_i)
        {
            next_i = read_i;
            while (next_i < line.Length && line[next_i] == ' ')
                ++next_i;
            return next_i < line.Length;
        }

        public readonly bool IsOnCompletion(out Range range)
        {
            range = new(0, line.Length);

            if (start_i <= cursor && cursor <= read_i)
            {
                range = new(start_i, read_i);
                return true;
            }

            return false;
        }
    }
}