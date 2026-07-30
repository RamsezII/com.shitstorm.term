using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace _TERM_
{
    sealed partial class CmdClient : TermClient
    {
        internal sealed class CmdRequest
        {
            internal readonly string type, cmdline, error;
            internal readonly int cursor;

            //----------------------------------------------------------------------------------------------------------

            CmdRequest(in string type, in string cmdline, in int cursor)
            {
                this.type = type;
                this.cmdline = cmdline;
                this.cursor = cursor;
                error = null;
            }

            CmdRequest(in string error)
            {
                type = null;
                cmdline = string.Empty;
                cursor = 0;
                this.error = error;
            }

            internal static CmdRequest Invalid(in string error) => new(error);

            internal static bool TryDeserialize(in string rawtext, out CmdRequest request, out string error)
            {
                request = null;
                error = null;

                try
                {
                    JObject json = JsonConvert.DeserializeObject<JObject>(rawtext);

                    if (json == null)
                        error = "empty JSON request";
                    else if (!json.TryGetValue("type", StringComparison.OrdinalIgnoreCase, out var type))
                        error = "no request type specified";
                    else if (string.IsNullOrWhiteSpace((string)type))
                        error = "empty request type";
                    else
                    {
                        json.TryGetValue("cmdline", StringComparison.OrdinalIgnoreCase, out var cmdline);
                        request = new(type: (string)type, cmdline: (string)cmdline ?? string.Empty, cursor: json.TryGetValue("cursor", StringComparison.OrdinalIgnoreCase, out var cursor) ? (int)cursor : 0);
                    }
                }
                catch (Exception e) when (
                    e is JsonException ||
                    e is FormatException ||
                    e is InvalidCastException ||
                    e is OverflowException)
                {
                    error = e.Message;
                }

                return request != null;
            }
        }

        readonly ConcurrentQueue<CmdRequest> requests = new();
        int closed;
        internal bool IsClosed => Volatile.Read(ref closed) != 0;

        //----------------------------------------------------------------------------------------------------------

        public CmdClient(in TcpClient tcpClient) : base(tcpClient)
        {
        }

        //----------------------------------------------------------------------------------------------------------

        internal void Enqueue(CmdRequest request) => requests.Enqueue(request);
        internal bool TryDequeue(out CmdRequest request) => requests.TryDequeue(out request);
        internal void CloseRequests() => Interlocked.Exchange(ref closed, 1);

        //----------------------------------------------------------------------------------------------------------

        internal IEnumerator ESend(CmdResponse response) => base.ESend(response);
        internal Task ASend(CmdResponse response) => base.ASend(response);
    }
}
