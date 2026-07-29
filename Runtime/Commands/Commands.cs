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

        internal abstract IEnumerator HandleRequest(CmdClient connection, ReadHandler hreader);
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

        internal override IEnumerator HandleRequest(CmdClient connection, ReadHandler hreader)
        {
            bool read_b = hreader._reader.TryRead(out string read);

            if (hreader._reader.IsOnCompletion())
            {
                hreader._reader.compl_candidates.AddRange(cmd_nodes.Keys);
                hreader._reader.compl_candidates.Sort();
                return null;
            }
            else if (!read_b)
                hreader._reader.WriteError($"expected command or namespace name (in namespace '{name}')");
            else if (!cmd_nodes.TryGetValue(read, out var node))
                hreader._reader.WriteError($"no command or namespace called '{read}' in namespace '{name}'");
            else
                return node.HandleRequest(connection, hreader);

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

        public readonly Action<CmdReader, CmdHandler> args;
        public readonly Func<CmdHandler, string> action1;
        public readonly Func<CmdHandler, ReadHandler, IEnumerator<RoutineStatus>> routine2;

        //----------------------------------------------------------------------------------------------------------

        public CmdCommand(in string name, in Action<CmdReader, CmdHandler> args = null, in Func<CmdHandler, string> action1 = null, in Func<CmdHandler, ReadHandler, IEnumerator<RoutineStatus>> routine2 = null) : base(name)
        {
            this.args = args;
            this.action1 = action1;
            this.routine2 = routine2;
        }

        //----------------------------------------------------------------------------------------------------------

        internal override IEnumerator HandleRequest(CmdClient connection, ReadHandler hreader)
        {
            CmdHandler handler = new();

            args?.Invoke(hreader._reader, handler);

            if (!hreader._reader.HasError)
                if (hreader._reader.type == CmdTypes.Execute)
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
                        using var routine = routine2(handler, hreader);
                        RoutineStatus last_status = default;
                        bool interrupted = false;

                        while (routine.MoveNext())
                        {
                            last_status = routine.Current;

                            if (last_status.prompt != null)
                            {
                                var input_task = connection.WaitForPromptInputAsync();
                                var estatus = connection.ESend(new CmdClient.CmdResponse_prompt(last_status.prompt));
                                while (estatus.MoveNext())
                                    yield return null;

                                while (!input_task.IsCompleted)
                                    yield return null;

                                CmdClient.CmdRequest input = input_task.Result;
                                if (input == null)
                                {
                                    interrupted = true;
                                    break;
                                }

                                if (!string.Equals(input.type, "input", StringComparison.OrdinalIgnoreCase))
                                {
                                    var eerror = connection.ESend(new CmdClient.CmdResponse_error($"Expected prompt input, received '{input.type}'."));
                                    while (eerror.MoveNext())
                                        yield return null;

                                    interrupted = true;
                                    break;
                                }

                                hreader._reader = new(type: CmdTypes.Execute, line: input.cmdline);
                            }
                            else
                                yield return null;
                        }

                        if (!interrupted)
                        {
                            var eresult = connection.ESend(new CmdClient.CmdResponse_result(last_status.result));
                            while (eresult.MoveNext())
                                yield return null;
                        }
                    }
                }
        }
    }
}
