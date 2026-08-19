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

    public sealed class CmdReader
    {
        internal readonly CmdTypes type;
        public bool ShouldBeComplete => type switch
        {
            CmdTypes.Check or CmdTypes.Execute => true,
            _ => false,
        };

        public readonly string line;
        public readonly int cursor;
        int start_i, read_i;
        internal int compl_start;
        internal readonly List<string> compl_candidates;
        readonly StringBuilder error_sb;
        public bool HasError => error_sb.Length > 0;
        public string GetError => HasError ? error_sb.ToString() : null;

        //----------------------------------------------------------------------------------------------------------

        public CmdReader(in CmdTypes type, in string line, in int cursor = 0)
        {
            this.type = type;
            this.line = line ?? string.Empty;

            start_i = 0;
            read_i = 0;
            compl_start = 0;
            compl_candidates = new List<string>();
            error_sb = new();

            this.cursor = type switch
            {
                CmdTypes.Check or CmdTypes.Execute => this.line.Length,
                _ when cursor < 0 => 0,
                _ when cursor > this.line.Length => this.line.Length,
                _ => cursor,
            };
        }

        //----------------------------------------------------------------------------------------------------------

        public void AppendError(in string error)
        {
            if (error != null)
                error_sb.Append(error);
        }

        public void WriteError(in string error, in bool force = false)
        {
            if (error == null)
                return;

            if (error_sb.Length == 0)
                error_sb.Append(error);
            else if (force)
            {
                error_sb.Clear();
                error_sb.Append(error);
            }
        }

        public void SkipEmpties() => SkipEmpties(ref read_i);
        public void SkipEmpties(ref int read_i)
        {
            while (read_i < line.Length && line[read_i] == ' ')
                ++read_i;
        }

        public void SkipNoneEmpties() => SkipNoneEmpties(ref read_i);
        public void SkipNoneEmpties(ref int read_i)
        {
            while (read_i < line.Length && line[read_i] != ' ')
                ++read_i;
        }

        public bool TryRead(out string output)
        {
            SkipEmpties();
            start_i = read_i;

            if (read_i < cursor)
            {
                SkipNoneEmpties();

                if (read_i > start_i)
                {
                    output = line[start_i..read_i];
                    return true;
                }
            }

            output = null;
            return false;
        }

        public void AddCompletions(params string[] candidates) => AddCompletions((IEnumerable<string>)candidates);
        public void AddCompletions(IEnumerable<string> candidates)
        {
            if (candidates == null || !IsOnCompletion())
                return;

            foreach (string candidate in candidates)
                if (!string.IsNullOrEmpty(candidate) && !compl_candidates.Contains(candidate))
                    compl_candidates.Add(candidate);
        }

        public bool IsOnCompletion()
        {
            if (type == CmdTypes.Complete)
                if (start_i <= cursor && cursor <= read_i)
                {
                    compl_start = start_i;
                    return true;
                }
            return false;
        }
    }
}
