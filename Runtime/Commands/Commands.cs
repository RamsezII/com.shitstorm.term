using System;
using System.Collections.Generic;

namespace _TERM_
{
    enum CmdStepTypes : byte
    {
        None,
        Prompt,
        Status,
        Result,
    }

    public delegate void CmdCompleter(CmdReader reader);

    public readonly struct CmdError
    {
        public readonly string message;
        public CmdError(in string message) => this.message = message;
        public static implicit operator CmdError(in string error) => new(error);
    }

    public readonly struct CmdStep
    {
        internal readonly CmdStepTypes type;
        internal readonly CmdCompleter completer;
        public readonly string text;
        public readonly float progress;

        //----------------------------------------------------------------------------------------------------------

        CmdStep(in CmdStepTypes type, in string text, in float progress, in CmdCompleter completer)
        {
            this.type = type;
            this.text = text;
            this.progress = progress;
            this.completer = completer;
        }

        public static CmdStep Prompt(in string prompt, CmdCompleter completer = null) => new(CmdStepTypes.Prompt, prompt, 0, completer);
        public static CmdStep Status(in string message, in float progress = 0) => new(CmdStepTypes.Status, message, progress, null);
        public static CmdStep Result(in string result = null) => new(CmdStepTypes.Result, result, 1, null);
    }

    public sealed class CmdContext
    {
        public readonly List<object> list_args = new();
        public readonly Dictionary<string, object> dict_options = new();
        internal CmdReader reader;
        public CmdReader Reader => reader;
        internal CmdContext(in CmdReader reader) => this.reader = reader;
    }

    public abstract class CmdNode
    {
        internal abstract CmdExecution TryParseCommand_term(in CmdReader reader, in CmdContext context);
    }

    public sealed class CmdNamespace : CmdNode
    {
        public interface IUser
        {
            CmdExecution TryParseCommand_term(in string arg0, in CmdReader reader, in CmdContext context);
        }

        public readonly HashSet<IUser> users = new();
        public readonly Dictionary<string, CmdNode> tree = new(StringComparer.OrdinalIgnoreCase);

        //----------------------------------------------------------------------------------------------------------

        public CmdCommand AddCommand(in string name, in Func<CmdReader, CmdContext, CmdExecution> execution)
        {
            var cmd = new CmdCommand(execution);
            tree.Add(name, cmd);
            return cmd;
        }

        public CmdNamespace AddNamespace(in string name)
        {
            CmdNamespace ns = new();
            tree.Add(name, ns);
            return ns;
        }

        //----------------------------------------------------------------------------------------------------------

        internal override CmdExecution TryParseCommand_term(in CmdReader reader, in CmdContext context)
        {
            bool arg0_b = reader.TryRead(out var arg0);

            if (reader.IsOnCompletion())
                reader.AddCompletions(arg0, tree.Keys);

            foreach (var user in users)
            {
                var execution = user.TryParseCommand_term(arg0, reader, context);
                if (execution.ready)
                    return execution;
            }

            if (arg0_b)
                if (tree.TryGetValue(arg0, out var node))
                    return node.TryParseCommand_term(reader, context);

            return null;
        }
    }

    public sealed class CmdCommand : CmdNode
    {
        readonly Func<CmdReader, CmdContext, CmdExecution> execution;
        internal CmdCommand(in Func<CmdReader, CmdContext, CmdExecution> execution) => this.execution = execution;
        internal override CmdExecution TryParseCommand_term(in CmdReader reader, in CmdContext context) => execution(reader, context);
    }

    public sealed class CmdExecution
    {
        internal readonly Action<CmdContext> _action;
        internal readonly Func<CmdContext, string> _function;
        internal readonly Func<CmdContext, IEnumerator<CmdStep>> _routine;
        internal readonly CmdError error;
        internal readonly bool ready;

        //----------------------------------------------------------------------------------------------------------

        public CmdExecution(in Action<CmdContext> action) : this(_action: action) { }
        public CmdExecution(in Func<CmdContext, string> function) : this(_function: function) { }
        public CmdExecution(in Func<CmdContext, IEnumerator<CmdStep>> routine) : this(_routine: routine) { }

        public static implicit operator CmdExecution(in Action<CmdContext> action) => new(_action: action);
        public static implicit operator CmdExecution(in Func<CmdContext, string> function) => new(_function: function);
        public static implicit operator CmdExecution(in Func<CmdContext, IEnumerator<CmdStep>> routine) => new(_routine: routine);
        public static implicit operator CmdExecution(in CmdError error) => new(error: error);

        CmdExecution(
            in Action<CmdContext> _action = null,
            in Func<CmdContext, string> _function = null,
            in Func<CmdContext, IEnumerator<CmdStep>> _routine = null,
            in CmdError error = default
        )
        {
            this._action = _action;
            this._function = _function;
            this._routine = _routine;
            this.error = error;
            ready = error.message == null;
        }
    }
}