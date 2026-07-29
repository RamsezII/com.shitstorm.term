using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace _TERM_
{
    public abstract class CmdNode
    {
        public readonly string name;

        //----------------------------------------------------------------------------------------------------------

        protected CmdNode(in string name)
        {
            this.name = name;
        }

        //----------------------------------------------------------------------------------------------------------

        internal abstract IEnumerator HandleRequest(CmdClient connection, CmdReader reader);
    }

    public sealed class CmdNamespace : CmdNode
    {
        public readonly Dictionary<string, CmdNode> cmd_nodes = new(StringComparer.OrdinalIgnoreCase);

        //----------------------------------------------------------------------------------------------------------

        public CmdNamespace(in string name) : base(name)
        {
        }

        //----------------------------------------------------------------------------------------------------------

        public void AddCommandNode(in CmdNode node)
        {
            cmd_nodes.Add(node.name, node);
        }

        //----------------------------------------------------------------------------------------------------------

        internal override IEnumerator HandleRequest(CmdClient connection, CmdReader reader)
        {
            string error = null;

            if (!reader.TryRead(out string read))
                error ??= $"";
            else if (!cmd_nodes.TryGetValue(read, out var node))
            {
                var routine = node.HandleRequest(connection, reader);
                if (routine != null)
                    while (routine.MoveNext())
                        yield return null;
            }

            if (error != null)
                yield return new JObject()
                {
                    ["type"] = "error",
                    ["error_message"] = error,
                };
        }
    }

    public sealed class CmdCommand : CmdNode
    {
        public readonly struct RoutineStatus
        {
            public readonly float progress;
            public readonly string prompt, result;
            public readonly bool assigned;

            //----------------------------------------------------------------------------------------------------------

            public RoutineStatus(in string prompt, in float progress, in string result)
            {
                this.progress = progress;
                this.prompt = prompt;
                this.result = result;
                assigned = true;
            }
        }

        public readonly List<string> completions = null;
        public readonly Action<CmdReader, CmdCommand> onRefreshCompletions;
        public readonly Func<CmdReader, string> action1;
        public readonly Func<CmdReader, IEnumerator<RoutineStatus>> routine2;

        //----------------------------------------------------------------------------------------------------------

        public CmdCommand(
            in string name,
            in Func<CmdReader, string> action1,
            in Func<CmdReader, IEnumerator<RoutineStatus>> routine2,
            in List<string> completions = null,
            in Action<CmdReader, CmdCommand> onRefreshCompletions = null
        ) : base(name)
        {
            this.action1 = action1;
            this.routine2 = routine2;
            this.completions = completions;
            this.onRefreshCompletions = onRefreshCompletions;
        }

        //----------------------------------------------------------------------------------------------------------

        internal override IEnumerator HandleRequest(CmdClient connection, CmdReader reader)
        {
            switch (reader.type)
            {
                case CmdTypes.Complete:
                    {
                        onRefreshCompletions?.Invoke(reader, this);

                        var routine = connection.ESend(new CmdClient.CmdResponse_completion(completions));

                        while (routine.MoveNext())
                            yield return null;
                    }
                    break;

                case CmdTypes.Execute:
                    if (action1 != null)
                    {
                        string result = action1(reader);
                        if (result != null)
                        {
                            var esend = connection.ESend(new CmdClient.CmdResponse_end(result));
                            while (esend.MoveNext())
                                yield return null;
                        }
                    }
                    if (routine2 != null)
                    {
                        using var routine = routine2(reader);

                        while (routine.MoveNext())
                            if (routine.Current.assigned)
                            {
                                var esend = connection.ESend(new CmdClient.CmdResponse_status(routine.Current));
                                while (esend.MoveNext())
                                    yield return null;
                            }
                            else
                                yield return null;

                        if (routine.Current.assigned)
                        {
                            var esend = connection.ESend(new CmdClient.CmdResponse_end(routine.Current.result));
                            while (esend.MoveNext())
                                yield return null;
                        }
                    }
                    break;

                default:
                    {
                        var esend = connection.ESend(new CmdClient.CmdResponse_error($"Unexpected {typeof(CmdTypes).FullName} '{reader.type}'"));
                        while (esend.MoveNext())
                            yield return null;
                    }
                    break;
            }
        }
    }
}