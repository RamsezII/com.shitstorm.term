using System;

namespace _TERM_
{
    public readonly struct CmdStep
    {
        internal enum Types : byte
        {
            None,
            Prompt,
            Status,
            Result,
        }

        internal readonly Types type;
        internal readonly Action<CmdReader> completer;
        public readonly string text;
        public readonly float progress;

        //----------------------------------------------------------------------------------------------------------

        CmdStep(in Types type, in string text, in float progress, in Action<CmdReader> completer)
        {
            this.type = type;
            this.text = text;
            this.progress = progress;
            this.completer = completer;
        }

        public static CmdStep Prompt(in string prompt, Action<CmdReader> completer = null) => new(Types.Prompt, prompt, 0, completer);
        public static CmdStep Status(in string message, in float progress = 0) => new(Types.Status, message, progress, null);
        public static CmdStep Result(in string result = null) => new(Types.Result, result, 1, null);
    }
}