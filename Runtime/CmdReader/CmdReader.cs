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

    public sealed partial class CmdReader
    {
        internal readonly CmdTypes type;
        public readonly string line;
        public readonly int cursor;
        [SerializeField] internal int start_i, read_i;
        internal string read_last;
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
    }
}
