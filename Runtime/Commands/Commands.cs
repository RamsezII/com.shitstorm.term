using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
            bool read_b = reader.TryRead(out string read);

            if (reader.IsOnCompletion())
            {
                reader.compl_candidates.AddRange(cmd_nodes.Keys);
                reader.compl_candidates.Sort();
                return null;
            }
            else if (!read_b)
                reader.WriteError($"expected command or namespace name (in namespace '{name}')");
            else if (!cmd_nodes.TryGetValue(read, out var node))
                reader.WriteError($"no command or namespace called '{read}' in namespace '{name}'");
            else
                return node.HandleRequest(connection, reader);

            return null;
        }

        //----------------------------------------------------------------------------------------------------------

        internal void Reset()
        {
            cmd_nodes.Clear();
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

        public sealed class CmdHandler
        {
            public readonly List<object> args = new();
            public readonly Dictionary<string, object> options = new(StringComparer.Ordinal);
        }

        public readonly Action<CmdReader, CmdHandler> arguments;
        public readonly Func<CmdHandler, string> action1;
        public readonly Func<CmdHandler, IEnumerator<RoutineStatus>> routine2;

        //----------------------------------------------------------------------------------------------------------

        public CmdCommand(
            in string name,
            in Action<CmdReader, CmdHandler> args = null,
            in Func<CmdHandler, string> action1 = null,
            in Func<CmdHandler, IEnumerator<RoutineStatus>> routine2 = null
        ) : base(name)
        {
            this.arguments = args;
            this.action1 = action1;
            this.routine2 = routine2;
        }

        //----------------------------------------------------------------------------------------------------------

        internal override IEnumerator HandleRequest(CmdClient connection, CmdReader reader)
        {
            CmdHandler handler = new();

            arguments?.Invoke(reader, handler);

            if (!reader.HasError)
                if (reader.type == CmdTypes.Execute)
                {
                    if (action1 != null)
                    {
                        string result = action1(handler);
                        var esend = connection.ESend(new CmdClient.CmdResponse_result(result));
                        while (esend.MoveNext())
                            yield return null;
                    }

                    if (routine2 != null)
                    {
                        using var routine = routine2(handler);
                        RoutineStatus last_status = default;

                        while (routine.MoveNext())
                        {
                            last_status = routine.Current;

                            if (last_status.assigned)
                            {
                                var estatus = connection.ESend(new CmdClient.CmdResponse_status(last_status));
                                while (estatus.MoveNext())
                                    yield return null;
                            }
                            else
                                yield return null;
                        }

                        var eresult = connection.ESend(new CmdClient.CmdResponse_result(last_status.assigned ? last_status.result : null));
                        while (eresult.MoveNext())
                            yield return null;
                    }
                }
        }
    }
}