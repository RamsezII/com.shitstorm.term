using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace _TERM_
{
    sealed partial class CmdClient : TermClient
    {
        internal sealed class CmdRequest
        {
            internal readonly string type, cmdline;
            internal readonly int cursor;

            //----------------------------------------------------------------------------------------------------------

            CmdRequest(in string type, in string cmdline, in int cursor)
            {
                this.type = type;
                this.cmdline = cmdline;
                this.cursor = cursor;
            }

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
                        request = new(
                            type: (string)type,
                            cmdline: (string)json["cmdline"] ?? string.Empty,
                            cursor: json.TryGetValue("cursor", out var cursor) ? (int)cursor : 0
                        );
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

        readonly object prompt_input_lock = new();
        TaskCompletionSource<CmdRequest> prompt_input;

        //----------------------------------------------------------------------------------------------------------

        public CmdClient(in TcpClient tcpClient) : base(tcpClient)
        {
        }

        //----------------------------------------------------------------------------------------------------------

        internal Task<CmdRequest> WaitForPromptInputAsync()
        {
            lock (prompt_input_lock)
            {
                if (prompt_input != null)
                    throw new InvalidOperationException("This command is already waiting for prompt input.");

                prompt_input = new(TaskCreationOptions.RunContinuationsAsynchronously);
                return prompt_input.Task;
            }
        }

        internal bool TryDeliverPromptInput(CmdRequest request)
        {
            TaskCompletionSource<CmdRequest> input;

            lock (prompt_input_lock)
            {
                if (prompt_input == null)
                    return false;

                input = prompt_input;
                prompt_input = null;
            }

            return input.TrySetResult(request);
        }

        internal void ClosePromptInput()
        {
            TaskCompletionSource<CmdRequest> input;

            lock (prompt_input_lock)
            {
                input = prompt_input;
                prompt_input = null;
            }

            input?.TrySetResult(null);
        }

        //----------------------------------------------------------------------------------------------------------

        internal IEnumerator ESend(CmdResponse response) => base.ESend(response);
        internal Task ASend(CmdResponse response) => base.ASend(response);
    }
}
