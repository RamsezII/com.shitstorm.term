using System;
using System.Collections;
using System.Collections.Generic;

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
                    if (!routines[i].MoveNext())
                    {
                        routines[i] = routines[^1];
                        routines.RemoveAt(routines.Count - 1);
                    }
        }

        static IEnumerator EOnIncomingCommand(CmdClient connection, CmdClient.CmdRequest request)
        {
            string error = null;

            if (!Enum.TryParse(request.type, true, out CmdTypes type))
                error = $"Unknown type '{request.type}'";
            else
            {
                ReadHandler hreader = new(new(
                    type: type,
                    line: request.cmdline,
                    cursor: request.cursor
                ));

                var routine = root_commands.HandleRequest(connection, hreader);
                if (routine != null)
                    while (routine.MoveNext())
                        yield return null;

                error ??= hreader._reader.GetError;

                if (error == null && type == CmdTypes.Complete)
                {
                    var esend = connection.ESend(new CmdClient.CmdResponse_completions(hreader._reader));
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
