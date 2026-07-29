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

        internal abstract IEnumerator HandleRequest(CmdClient connection, CmdTypes type, CmdLineReader reader);
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

        internal override IEnumerator HandleRequest(CmdClient connection, CmdTypes type, CmdLineReader reader)
        {
            bool read_b = reader.TryRead(out string read);

            if (reader.IsOnCompletion(out var range))
            {
                string[] candidates = cmd_nodes.Keys.ToArray();
                Array.Sort(candidates);
                return connection.ESend(new CmdClient.CmdResponse_completion(range, candidates));
            }
            else if (!read_b)
                reader.WriteError($"expected command or namespace name (in namespace '{name}')");
            else if (!cmd_nodes.TryGetValue(read, out var node))
                reader.WriteError($"no command or namespace called '{read}' in namespace '{name}'");
            else
                return node.HandleRequest(connection, type, reader);

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

        public class Opts : Dictionary<string, object>
        {
            public Opts() : base(StringComparer.Ordinal)
            {
            }
        }

        public class Args : List<object>
        {
        }

        public readonly Action<CmdLineReader, Opts> opts;
        public readonly Action<CmdLineReader, Args> args;
        public readonly Func<Opts, Args, string> action1;
        public readonly Func<Opts, Args, IEnumerator<RoutineStatus>> routine2;

        //----------------------------------------------------------------------------------------------------------

        public CmdCommand(
            in string name,
            in Action<CmdLineReader, Opts> opts = null,
            in Action<CmdLineReader, Args> args = null,
            in Func<Opts, Args, string> action1 = null,
            in Func<Opts, Args, IEnumerator<RoutineStatus>> routine2 = null
        ) : base(name)
        {
            this.opts = opts;
            this.args = args;
            this.action1 = action1;
            this.routine2 = routine2;
        }

        //----------------------------------------------------------------------------------------------------------

        internal override IEnumerator HandleRequest(CmdClient connection, CmdTypes type, CmdLineReader reader)
        {
            Opts opts = new();
            this.opts?.Invoke(reader, opts);

            Args args = new();
            this.args?.Invoke(reader, args);

            if (type == CmdTypes.Execute)
            {
                if (action1 != null)
                {
                    string result = action1(opts, args);
                    var esend = connection.ESend(new CmdClient.CmdResponse_result(result));
                    while (esend.MoveNext())
                        yield return null;
                }

                if (routine2 != null)
                {
                    using var routine = routine2(opts, args);
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
