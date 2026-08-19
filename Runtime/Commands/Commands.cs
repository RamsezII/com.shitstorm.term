using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        public readonly List<object> args = new();
        public readonly Dictionary<string, object> options = new(StringComparer.Ordinal);
        internal CmdReader reader;
        public CmdReader Reader => reader;

        //----------------------------------------------------------------------------------------------------------

        internal CmdContext(in CmdReader reader)
        {
            this.reader = reader;
        }
    }

    public abstract class CmdNode
    {
        public readonly string name;
        public readonly object owner;

        //----------------------------------------------------------------------------------------------------------

        protected CmdNode(in string name, in object owner)
        {
            this.name = name;
            this.owner = owner;
        }

        //----------------------------------------------------------------------------------------------------------

        internal abstract IEnumerator HandleRequest(CmdClient connection, CmdReader reader);
    }

    public sealed class CmdNamespace : CmdNode
    {
        readonly Dictionary<string, CmdNode> cmd_nodes = new(StringComparer.OrdinalIgnoreCase);

        //----------------------------------------------------------------------------------------------------------

        public CmdNamespace(in string name, in object owner) : base(name, owner)
        {
        }

        //----------------------------------------------------------------------------------------------------------

        public void AddCommand(in CmdCommand node) => AddCmdNode(node);
        public void AddNamespace(in CmdNamespace node) => AddCmdNode(node);
        void AddCmdNode(in CmdNode node) => cmd_nodes.Add(node.name, node);
        public bool RemoveByKey(in string key) => cmd_nodes.Remove(key);
        public void RemoveByOwner(in object owner)
        {
            foreach (var pair in cmd_nodes.ToList())
                if (owner == pair.Value.owner)
                    cmd_nodes.Remove(pair.Key);
        }

        //----------------------------------------------------------------------------------------------------------

        internal override IEnumerator HandleRequest(CmdClient connection, CmdReader reader)
        {
            bool read_b = reader.TryRead(out string read);

            if (reader.IsOnCompletion())
            {
                reader.AddCompletions(cmd_nodes.Keys);
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
        public readonly Func<CmdReader, CmdContext, string> parse;
        public readonly Action<CmdContext> action;
        public readonly Func<CmdContext, string> function;
        public readonly Func<CmdContext, IEnumerator<CmdStep>> routine;

        //----------------------------------------------------------------------------------------------------------

        public CmdCommand(in string name, in object owner, in Func<CmdReader, CmdContext, string> parse = null, in Action<CmdContext> action = null, in Func<CmdContext, string> function = null, in Func<CmdContext, IEnumerator<CmdStep>> routine = null) : base(name, owner)
        {
            this.parse = parse;
            this.action = action;
            this.function = function;
            this.routine = routine;
        }

        //----------------------------------------------------------------------------------------------------------

        internal override IEnumerator HandleRequest(CmdClient connection, CmdReader reader)
        {
            CmdContext context = new(reader);

            reader.AppendError(parse?.Invoke(reader, context));

            if (reader.HasError || reader.type != CmdTypes.Execute)
                yield break;

            if (action != null)
            {
                action(context);
                var esend = connection.ESend(new CmdClient.CmdResponse_result(null));
                while (esend.MoveNext())
                    yield return null;
                yield break;
            }

            if (function != null)
            {
                string result = function(context);
                var esend = connection.ESend(new CmdClient.CmdResponse_result(result));
                while (esend.MoveNext())
                    yield return null;
                yield break;
            }

            using IEnumerator<CmdStep> command_routine = routine(context);
            string routine_result = null;
            bool cancelled = false;
            bool failed = false;
            bool finished = false;

            while (!finished && !cancelled && !failed && !connection.IsClosed)
            {
                bool moved = false;
                CmdStep step = default;
                Exception exception = null;

                try
                {
                    moved = command_routine.MoveNext();
                    if (moved)
                        step = command_routine.Current;
                }
                catch (Exception e)
                {
                    exception = e;
                }

                if (exception != null)
                {
                    var eexception = connection.ESend(new CmdClient.CmdResponse_exception(exception));
                    while (eexception.MoveNext())
                        yield return null;
                    yield break;
                }

                if (!moved)
                    break;

                if (step.type == CmdStepTypes.Prompt)
                {
                    var eprompt = connection.ESend(new CmdClient.CmdResponse_prompt(step.text));
                    while (eprompt.MoveNext())
                        yield return null;

                    bool received_input = false;

                    while (!received_input && !cancelled && !failed && !connection.IsClosed)
                    {
                        if (!connection.TryDequeue(out CmdClient.CmdRequest request))
                        {
                            yield return null;
                            continue;
                        }

                        if (request.error != null)
                        {
                            var eerror = connection.ESend(new CmdClient.CmdResponse_error(request.error));
                            while (eerror.MoveNext())
                                yield return null;
                            failed = true;
                        }
                        else if (string.Equals(request.type, "input", StringComparison.OrdinalIgnoreCase))
                        {
                            context.reader = new(type: CmdTypes.Execute, line: request.cmdline);
                            received_input = true;
                        }
                        else if (string.Equals(request.type, "complete", StringComparison.OrdinalIgnoreCase))
                        {
                            CmdReader completion_reader = new(type: CmdTypes.Complete, line: request.cmdline, cursor: request.cursor);
                            Exception completion_exception = null;

                            try
                            {
                                step.completer?.Invoke(completion_reader);
                            }
                            catch (Exception e)
                            {
                                completion_exception = e;
                            }

                            if (completion_exception == null)
                            {
                                var ecompletion = connection.ESend(new CmdClient.CmdResponse_completions(completion_reader));
                                while (ecompletion.MoveNext())
                                    yield return null;
                            }
                            else
                            {
                                var eexception = connection.ESend(new CmdClient.CmdResponse_exception(completion_exception));
                                while (eexception.MoveNext())
                                    yield return null;
                            }
                        }
                        else if (string.Equals(request.type, "cancel", StringComparison.OrdinalIgnoreCase))
                            cancelled = true;
                        else
                        {
                            var eerror = connection.ESend(new CmdClient.CmdResponse_error($"Expected prompt input, completion or cancellation, received '{request.type}'."));
                            while (eerror.MoveNext())
                                yield return null;
                            failed = true;
                        }
                    }
                }
                else if (step.type == CmdStepTypes.Status)
                {
                    var estatus = connection.ESend(new CmdClient.CmdResponse_status(step.text, step.progress));
                    while (estatus.MoveNext())
                        yield return null;
                }
                else if (step.type == CmdStepTypes.Result)
                {
                    routine_result = step.text;
                    finished = true;
                }
                else
                {
                    if (connection.TryDequeue(out CmdClient.CmdRequest request))
                    {
                        if (request.error != null)
                        {
                            var eerror = connection.ESend(new CmdClient.CmdResponse_error(request.error));
                            while (eerror.MoveNext())
                                yield return null;
                            failed = true;
                        }
                        else if (string.Equals(request.type, "cancel", StringComparison.OrdinalIgnoreCase))
                            cancelled = true;
                        else
                        {
                            var eerror = connection.ESend(new CmdClient.CmdResponse_error($"Command is not waiting for input; received '{request.type}'."));
                            while (eerror.MoveNext())
                                yield return null;
                            failed = true;
                        }
                    }

                    if (!cancelled && !failed)
                        yield return null;
                }
            }

            if (connection.IsClosed || failed)
                yield break;

            if (cancelled)
            {
                var ecancelled = connection.ESend(new CmdClient.CmdResponse_cancelled());
                while (ecancelled.MoveNext())
                    yield return null;
            }
            else
            {
                var eresult = connection.ESend(new CmdClient.CmdResponse_result(routine_result));
                while (eresult.MoveNext())
                    yield return null;
            }
        }
    }
}
