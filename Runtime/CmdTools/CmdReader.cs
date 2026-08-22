using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace _TERM_
{
    public enum CmdTypes : byte
    {
        Complete,
        Execute,
    }

    public sealed class CmdReader
    {
        internal readonly CmdTypes type;
        public readonly string line;
        public readonly int cursor;
        int start_i, read_i;
        internal string read_last;
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

            if (type == CmdTypes.Execute)
                this.cursor = line.Length;
            else
                this.cursor = Mathf.Clamp(cursor, 0, line.Length);
        }

        //----------------------------------------------------------------------------------------------------------

        public void Error(in string error)
        {
            if (error != null)
                error_sb.Append(error);
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

        public bool TryRead(out string output) => TryRead(ref read_i, out output);
        public bool TryRead(ref int read_i, out string output)
        {
            SkipEmpties(ref read_i);
            start_i = read_i;

            if (read_i < cursor)
            {
                SkipNoneEmpties(ref read_i);
                if (read_i > start_i)
                {
                    read_last = output = line[start_i..read_i];
                    return true;
                }
            }

            output = null;
            return false;
        }

        public bool TryReadMatch(in string option, in StringComparison comparison = StringComparison.Ordinal)
        {
            SkipEmpties();
            int read_i = this.read_i;

            if (TryRead(ref read_i, out string output))
                if (output.Equals(option, comparison))
                {
                    this.read_i = read_i;
                    AddCompletions(output, option);
                    return true;
                }

            AddCompletions(output, option);
            return false;
        }

        public void AddCompletions<T>(in string prefixe) where T : Enum => AddCompletions(prefixe, Enum.GetNames(typeof(T)));
        public void AddCompletions(in string prefixe, params string[] candidates) => AddCompletions(prefixe, (IEnumerable<string>)candidates);
        public void AddCompletions(in string argument, IEnumerable<string> candidates)
        {
            if (candidates == null || !IsOnCompletion())
                return;

            foreach (string candidate in candidates)
                if (!string.IsNullOrEmpty(candidate))
                    if (!compl_candidates.Contains(candidate))
                        if (string.IsNullOrWhiteSpace(argument) || Util.IsMatchChars(argument, candidate))
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
