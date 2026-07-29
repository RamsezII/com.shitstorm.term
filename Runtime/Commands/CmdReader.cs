using System.Collections.Generic;
using System.Text;

namespace _TERM_
{
    public enum CmdTypes : byte
    {
        Complete,
        Check,
        Execute,
    }

    public struct CmdReader
    {
        internal CmdTypes type;
        public readonly string line;
        public readonly int cursor;
        public int start_i, read_i;
        public int compl_start, compl_end;
        public readonly List<string> compl_candidates;
        readonly StringBuilder error_sb;
        public readonly bool HasError => error_sb != null && error_sb.Length > 0;
        public readonly string GetError => HasError ? error_sb.ToString() : null;

        //----------------------------------------------------------------------------------------------------------

        public CmdReader(in CmdTypes type, in string line, in int cursor = 0)
        {
            this.type = type;
            this.line = line;

            start_i = 0;
            read_i = 0;
            compl_start = 0;
            compl_end = line.Length;
            compl_candidates = new List<string>();
            error_sb = new();

            this.cursor = type switch
            {
                CmdTypes.Check or CmdTypes.Execute => line.Length,
                _ => cursor,
            };
        }

        //----------------------------------------------------------------------------------------------------------

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

            if (read_i < cursor)
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

        public bool IsOnCompletion()
        {
            if (type == CmdTypes.Complete)
                if (start_i <= cursor && cursor <= read_i)
                {
                    compl_start = start_i;
                    compl_end = read_i;
                    return true;
                }
            return false;
        }
    }
}