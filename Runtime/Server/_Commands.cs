using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _TERM_
{

    partial class TermServer
    {
        public static readonly CmdNamespace root_commands = new(nameof(root_commands));

        readonly List<IEnumerator> routines = new();

        //----------------------------------------------------------------------------------------------------------

        void TickRoutines()
        {
            lock (routines)
                for (int i = routines.Count - 1; i >= 0; i--)
                {
                    bool moved = false;

                    try
                    {
                        moved = routines[i].MoveNext();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }

                    if (!moved)
                    {
                        (routines[i] as IDisposable)?.Dispose();
                        routines[i] = routines[^1];
                        routines.RemoveAt(routines.Count - 1);
                    }
                }
        }

        static IEnumerator ECommandSession(CmdClient connection)
        {
            while (!connection.IsClosed)
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
                    continue;
                }

                IEnumerator request_routine = EOnIncomingCommand(connection, request);
                bool running = true;

                while (running && !connection.IsClosed)
                {
                    Exception exception = null;

                    try
                    {
                        running = request_routine.MoveNext();
                    }
                    catch (Exception e)
                    {
                        exception = e;
                        running = false;
                    }

                    if (exception != null)
                    {
                        var eexception = connection.ESend(new CmdClient.CmdResponse_exception(exception));
                        while (eexception.MoveNext())
                            yield return null;
                    }
                    else if (running)
                        yield return null;
                }

                (request_routine as IDisposable)?.Dispose();
            }
        }

        static IEnumerator EOnIncomingCommand(CmdClient connection, CmdClient.CmdRequest request)
        {
            string error = null;

            if (!Enum.TryParse(request.type, true, out CmdTypes type))
                error = $"Unknown type '{request.type}'";
            else
            {
                CmdReader reader = new(type: type, line: request.cmdline, cursor: request.cursor);

                var routine = root_commands.HandleRequest(connection, reader);
                if (routine != null)
                    try
                    {
                        while (routine.MoveNext())
                            yield return null;
                    }
                    finally
                    {
                        (routine as IDisposable)?.Dispose();
                    }

                error ??= reader.GetError;

                if (error == null && type == CmdTypes.Complete)
                {
                    var esend = connection.ESend(new CmdClient.CmdResponse_completions(reader));
                    while (esend.MoveNext())
                        yield return null;
                }
            }

            if (error != null)
            {
                var esend = connection.ESend(new CmdClient.CmdResponse_error(error));
                while (esend.MoveNext())
                    yield return null;
            }
        }
    }
}
