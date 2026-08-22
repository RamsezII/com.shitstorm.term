using System.Collections.Generic;

namespace _TERM_
{
    public sealed class CmdContext
    {
        public readonly Queue<object> queue_args = new();
        public readonly Dictionary<CmdOption, object> dict_options = new();
        internal CmdReader reader;
        public CmdReader Reader => reader;
        internal CmdContext(in CmdReader reader) => this.reader = reader;
    }
}