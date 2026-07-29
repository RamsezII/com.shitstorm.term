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
        public readonly CmdTypes type;
        public readonly int cursor;
        public int start_i, read_i;
        readonly StringBuilder error_sb;
        public readonly string GetError => error_sb != null && error_sb.Length > 0 ? error_sb.ToString() : null;
        public readonly bool IsCompletionType => type switch
        {
            CmdTypes.Complete => true,
            _ => false,
        };

        //----------------------------------------------------------------------------------------------------------

        public CmdLineReader(in string line, in CmdTypes type, in int cursor = 0)
        {
            this.line = line;
            this.type = type;
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

        public void SkipEmpties()
        {
            while (read_i < line.Length && line[read_i] == ' ')
                ++read_i;
        }

        public void SkipNoneEmpties()
        {
            while (read_i < line.Length && line[read_i] != ' ')
                ++read_i;
        }

        public bool TryRead(out string output)
        {
            SkipEmpties();

            if (read_i < line.Length)
            {
                int old_read_i = read_i;
                SkipNoneEmpties();

                if (read_i > old_read_i)
                {
                    output = line[old_read_i..read_i];
                    SkipEmpties();
                    return true;
                }
            }

            output = null;
            return false;
        }

        public readonly bool HasNext()
        {
            int read_i = this.read_i;
            while (read_i < line.Length && line[read_i] == ' ')
                ++read_i;
            return read_i < line.Length;
        }

        public readonly bool IsOnCompletion(out Range range)
        {
            range = new(0, line.Length);

            if (IsCompletionType)
            {
                return true;
            }

            return false;
        }
    }
}