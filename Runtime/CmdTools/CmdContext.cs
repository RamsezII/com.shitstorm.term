using System.Collections.Generic;

namespace _TERM_
{
    public sealed class CmdContext
    {
        internal readonly Queue<object> queue_args = new();
        public readonly Dictionary<CmdOption, object> dict_options = new();
        internal CmdReader reader;
        public CmdReader Reader => reader;

        //----------------------------------------------------------------------------------------------------------

        internal CmdContext(in CmdReader reader) => this.reader = reader;

        //----------------------------------------------------------------------------------------------------------

        /// <returns>true if no error</returns>
        public int TryReadOptions(in CmdReader.OptionsInput input) => reader.TryReadOptions(dict_options, input);

        public int ArgsCount => queue_args.Count;

        public void EnqueueArg(in object arg) => queue_args.Enqueue(arg);
        public void EnqueueArgs<T>(IEnumerable<T> args)
        {
            foreach (var arg in args)
                queue_args.Enqueue(arg);
        }

        public T DequeueArg<T>() => (T)queue_args.Dequeue();

        public bool TryPeekArg<T>(out T value)
        {
            if (queue_args.TryPeek(out object _o))
            {
                value = (T)_o;
                return true;
            }
            value = default;
            return false;
        }

        public bool TryDequeueArg<T>(out T value)
        {
            if (queue_args.TryDequeue(out object _o))
            {
                value = (T)_o;
                return true;
            }
            value = default;
            return false;
        }
    }
}