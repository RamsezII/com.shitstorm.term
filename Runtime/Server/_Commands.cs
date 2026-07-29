using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        static IEnumerator EOnIncomingCommand(CmdClient connection, string json)
        {
            JObject jrequest = JsonConvert.DeserializeObject<JObject>(json);
            string error = null;

            if (!jrequest.TryGetValue("type", StringComparison.OrdinalIgnoreCase, out var _type))
                error = $"no {typeof(CmdTypes).FullName} specified";
            else if (!Enum.TryParse((string)_type, true, out CmdTypes type))
                error = $"Unknown type '{(string)_type}'";
            else
            {
                CmdLineReader reader = new(
                    line: (string)jrequest["cmdline"],
                    cursor: jrequest.TryGetValue("cursor", out var _cursor) ? (int)_cursor : 0
                );

                var routine = root_commands.HandleRequest(connection, type, reader);
                while (routine.MoveNext())
                    yield return null;

                error ??= reader.GetError;
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
