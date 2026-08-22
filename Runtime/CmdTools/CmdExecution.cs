using System;
using System.Collections.Generic;

namespace _TERM_
{
    public sealed class CmdExecution
    {
        internal readonly Action<CmdContext> _action;
        internal readonly Func<CmdContext, string> _function;
        internal readonly Func<CmdContext, IEnumerator<CmdStep>> _routine;
        internal readonly string _error;
        internal readonly bool ready;
        public static CmdExecution CodeNotImplemented(in object code) => new($"{code.GetType()} '{code}' not implemented");

        //----------------------------------------------------------------------------------------------------------

        public CmdExecution(in Action<CmdContext> action) : this(_action: action) { }
        public CmdExecution(in Func<CmdContext, string> function) : this(_function: function) { }
        public CmdExecution(in Func<CmdContext, IEnumerator<CmdStep>> routine) : this(_routine: routine) { }
        public CmdExecution(in string error) : this(_error: error) { }

        CmdExecution(
            in Action<CmdContext> _action = null,
            in Func<CmdContext, string> _function = null,
            in Func<CmdContext, IEnumerator<CmdStep>> _routine = null,
            in string _error = default
        )
        {
            this._action = _action;
            this._function = _function;
            this._routine = _routine;
            this._error = _error;
            ready = _error == null;
        }
    }
}