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
                CmdReader reader = new(
                    type: type,
                    line: request.cmdline,
                    cursor: request.cursor
                );

                var routine = root_commands.HandleRequest(connection, reader);
                if (routine != null)
                    while (routine.MoveNext())
                        yield return null;

                error ??= reader.GetError;

                if (error == null && type == CmdTypes.Complete)
                {
                    var esend = connection.ESend(new CmdClient.CmdResponse_completions(
                        start: reader.compl_start,
                        end: reader.compl_end,
                        candidates: reader.compl_candidates
                    ));
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
