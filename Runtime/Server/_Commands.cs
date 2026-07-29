using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        internal abstract IEnumerator<JObject> HandleRequest(TermServer.ClientConnection connection, CmdReader reader);
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

        internal override IEnumerator<JObject> HandleRequest(TermServer.ClientConnection connection, CmdReader reader)
        {
            string error = null;

            if (!reader.TryRead(out string read))
                error ??= $"";
            else if (!cmd_nodes.TryGetValue(read, out var node))
            {
                IEnumerator<JObject> routine = node.HandleRequest(connection, reader);
                if (routine != null)
                    while (routine.MoveNext())
                        yield return routine.Current;
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
        public readonly struct CmdStatus
        {
            public readonly float progress;
            public readonly string prompt, result;
            public readonly bool assigned;

            //----------------------------------------------------------------------------------------------------------

            public CmdStatus(in string prompt, in float progress, in string result)
            {
                this.progress = progress;
                this.prompt = prompt;
                this.result = result;
                assigned = true;
            }
        }

        public readonly List<object> completions = null;
        public readonly Action<CmdReader, CmdCommand> onRefreshCompletions;
        public readonly Func<CmdReader, string> action;
        public readonly Func<CmdReader, IEnumerator<CmdStatus>> routine;

        //----------------------------------------------------------------------------------------------------------

        CmdCommand(
            in string name,
            in Func<CmdReader, string> action,
            in Func<CmdReader, IEnumerator<CmdStatus>> routine,
            in List<object> completions = null,
            in Action<CmdReader, CmdCommand> onRefreshCompletions = null
        ) : base(name)
        {
            this.action = action;
            this.routine = routine;
            this.completions = completions;
            this.onRefreshCompletions = onRefreshCompletions;
        }

        public CmdCommand(
            in string name,
            in Func<CmdReader, string> action,
            in List<object> completions = null,
            in Action<CmdReader, CmdCommand> onRefreshCompletions = null
        ) : this(name, action, null, completions, onRefreshCompletions)
        {
        }

        public CmdCommand(
            in string name,
            in Func<CmdReader, IEnumerator<CmdStatus>> routine,
            in List<object> completions = null,
            in Action<CmdReader, CmdCommand> onRefreshCompletions = null
        ) : this(name, null, routine, completions, onRefreshCompletions)
        {
        }

        //----------------------------------------------------------------------------------------------------------

        internal override IEnumerator<JObject> HandleRequest(TermServer.ClientConnection connection, CmdReader reader)
        {
            switch (reader.type)
            {
                case CmdTypes.Complete:
                    onRefreshCompletions?.Invoke(reader, this);
                    yield return new()
                    {
                        ["type"] = "completion",
                        ["candidates"] = JsonConvert.SerializeObject(completions, Formatting.None)
                    };
                    break;

                case CmdTypes.Execute:
                    {
                        string result = default;

                        if (action != null)
                            result = action(reader);
                        else
                        {
                            using var routine = this.routine(reader);

                            while (routine.MoveNext())
                                if (routine.Current.assigned)
                                    yield return new()
                                    {
                                        ["type"] = "prompt",
                                        ["prompt"] = routine.Current.prompt,
                                    };
                                else
                                    yield return null;

                            result = routine.Current.result;
                        }

                        if (result != null)
                            yield return new()
                            {
                                ["type"] = "result",
                                ["result"] = result.ToString(),
                            };
                    }
                    break;

                default:
                    yield return new()
                    {
                        ["type"] = "error",
                        ["error_message"] = $"Unexpected {typeof(CmdTypes).FullName} '{reader.type}'",
                    };
                    break;
            }
        }
    }

    public enum CmdTypes : byte
    {
        Complete,
        Check,
        Execute,
    }

    public struct CmdReader
    {
        public readonly string line;
        public readonly CmdTypes type;
        public readonly int cursor;
        public int start_i, read_i;
        public string error;

        //----------------------------------------------------------------------------------------------------------

        public CmdReader(in string line, in CmdTypes type, in int cursor = 0)
        {
            this.line = line;
            this.type = type;
            this.cursor = cursor;
            start_i = 0;
            read_i = 0;
            error = null;
        }

        //----------------------------------------------------------------------------------------------------------

        public void SkipEmpties()
        {
            while (read_i < line.Length && line[read_i] switch
            {
                ' ' or '\n' => true,
                _ => false,
            })
                ++read_i;
        }

        public void SkipNoneEmpties()
        {
            while (read_i < line.Length && line[read_i] switch
            {
                ' ' or '\n' => false,
                _ => true,
            })
                ++read_i;
        }

        public bool TryRead(out string output)
        {
            SkipEmpties();

            if (read_i < line.Length)
            {
                int old_read_i = read_i;
                SkipNoneEmpties();

                if (read_i > old_read_i)
                {
                    output = line[old_read_i..read_i];
                    SkipEmpties();
                    return true;
                }
            }

            output = null;
            return false;
        }
    }

    partial class TermServer
    {
        public static readonly CmdNamespace root_commands = new(nameof(root_commands));

        readonly List<IEnumerator> routines = new();

        //----------------------------------------------------------------------------------------------------------

        void TickRoutines()
        {
            lock (routines)
                for (int i = routines.Count - 1; i >= 0; i--)
                    if (!routines[i].MoveNext())
                    {
                        routines[i] = routines[^1];
                        routines.RemoveAt(routines.Count - 1);
                    }
        }

        static IEnumerator EOnIncomingCommand(ClientConnection connection, string json)
        {
            JObject jrequest = JsonConvert.DeserializeObject<JObject>(json);
            JObject jresponse = null;
            string error = null;

            if (!jrequest.TryGetValue("type", StringComparison.OrdinalIgnoreCase, out var _type))
                error = "no type specified";
            else if (!Enum.TryParse((string)_type, true, out CmdTypes type))
                error = $"Unknown type '{(string)_type}'";
            else
            {
                CmdReader reader = new(
                    line: (string)jrequest["cmdline"],
                    type: type,
                    cursor: jrequest.TryGetValue("cursor", out var _cursor) ? (int)_cursor : 0
                );

                var routine = root_commands.HandleRequest(connection, reader);
                while (routine.MoveNext())
                    yield return null;
            }

            if (error != null)
                jresponse = new()
                {
                    ["type"] = "error",
                    ["message"] = "no type specified",
                };

            if (jresponse != null)
            {
                Task task = connection.SendAsync(jresponse);
                while (!task.IsCompleted)
                    yield return null;
            }
        }
    }
}
