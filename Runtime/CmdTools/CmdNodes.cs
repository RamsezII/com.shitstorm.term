using System;
using System.Collections.Generic;

namespace _TERM_
{
    public abstract class CmdNode
    {
        internal abstract CmdExecution TryParseCommand_term(in CmdContext context);
    }

    public sealed class CmdNamespace : CmdNode
    {
        public interface IUser
        {
            CmdExecution OnTryCommand(in string arg0, in CmdContext context);
        }

        public readonly HashSet<IUser> users = new();
        public readonly Dictionary<string, CmdNode> tree = new(StringComparer.OrdinalIgnoreCase);

        //----------------------------------------------------------------------------------------------------------

        public CmdCommand AddCommand(in string name, in Func<CmdContext, CmdExecution> execution)
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

        internal override CmdExecution TryParseCommand_term(in CmdContext context)
        {
            bool arg0_b = context.reader.TryRead(out var arg0);

            if (context.reader.IsOnCompletion())
                context.reader.AddCompletions(arg0, tree.Keys);

            foreach (var user in users)
            {
                var execution = user.OnTryCommand(arg0, context);
                if (execution.ready)
                    return execution;
            }

            if (arg0_b)
                if (tree.TryGetValue(arg0, out var node))
                    return node.TryParseCommand_term(context);

            return null;
        }
    }

    public sealed class CmdCommand : CmdNode
    {
        readonly Func<CmdContext, CmdExecution> execution;
        internal CmdCommand(in Func<CmdContext, CmdExecution> execution) => this.execution = execution;
        internal override CmdExecution TryParseCommand_term(in CmdContext context) => execution(context);
    }
}