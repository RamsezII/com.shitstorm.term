using System;
using System.Collections.Generic;

namespace _TERM_
{
    partial class TermServer
    {
        static IEnumerator<float> HandleRequest(CmdClient connection, CmdReader reader)
        {
            var context = new CmdContext(reader);
            CmdExecution execution = root_namespace.TryParseCommand_term(reader, context);

            if (!execution.ready)
            {
                reader.Error(execution.error.message);
                if (execution == null || reader.HasError || reader.type != CmdTypes.Execute)
                    yield break;
            }

            if (execution._action != null)
            {
                execution._action(context);
                var esend = connection.ESend(new CmdClient.CmdResponse_result(null));
                while (esend.MoveNext())
                    yield return 1;
            }

            if (execution._function != null)
            {
                string result = execution._function(context);
                var esend = connection.ESend(new CmdClient.CmdResponse_result(result));
                while (esend.MoveNext())
                    yield return 1;
            }

            if (execution._routine != null)
            {
                using IEnumerator<CmdStep> command_routine = execution._routine(context);
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
                            yield return 1;
                        yield break;
                    }

                    if (!moved)
                        break;

                    if (step.type == CmdStepTypes.Prompt)
                    {
                        var eprompt = connection.ESend(new CmdClient.CmdResponse_prompt(step.text));
                        while (eprompt.MoveNext())
                            yield return 0;

                        bool received_input = false;

                        while (!received_input && !cancelled && !failed && !connection.IsClosed)
                        {
                            if (!connection.TryDequeue(out CmdClient.CmdRequest request))
                            {
                                yield return 0;
                                continue;
                            }

                            if (request.error != null)
                            {
                                var eerror = connection.ESend(new CmdClient.CmdResponse_error(request.error));
                                while (eerror.MoveNext())
                                    yield return 0;
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
                                        yield return 0;
                                }
                                else
                                {
                                    var eexception = connection.ESend(new CmdClient.CmdResponse_exception(completion_exception));
                                    while (eexception.MoveNext())
                                        yield return 0;
                                }
                            }
                            else if (string.Equals(request.type, "cancel", StringComparison.OrdinalIgnoreCase))
                                cancelled = true;
                            else
                            {
                                var eerror = connection.ESend(new CmdClient.CmdResponse_error($"Expected prompt input, completion or cancellation, received '{request.type}'."));
                                while (eerror.MoveNext())
                                    yield return 0;
                                failed = true;
                            }
                        }
                    }
                    else if (step.type == CmdStepTypes.Status)
                    {
                        var estatus = connection.ESend(new CmdClient.CmdResponse_status(step.text, step.progress));
                        while (estatus.MoveNext())
                            yield return 0;
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
                                    yield return 0;
                                failed = true;
                            }
                            else if (string.Equals(request.type, "cancel", StringComparison.OrdinalIgnoreCase))
                                cancelled = true;
                            else
                            {
                                var eerror = connection.ESend(new CmdClient.CmdResponse_error($"Command is not waiting for input; received '{request.type}'."));
                                while (eerror.MoveNext())
                                    yield return 0;
                                failed = true;
                            }
                        }

                        if (!cancelled && !failed)
                            yield return 1;
                    }
                }

                if (connection.IsClosed || failed)
                    yield break;

                if (cancelled)
                {
                    var ecancelled = connection.ESend(new CmdClient.CmdResponse_cancelled());
                    while (ecancelled.MoveNext())
                        yield return 0;
                }
                else
                {
                    var eresult = connection.ESend(new CmdClient.CmdResponse_result(routine_result));
                    while (eresult.MoveNext())
                        yield return 0;
                }
            }
        }
    }
}