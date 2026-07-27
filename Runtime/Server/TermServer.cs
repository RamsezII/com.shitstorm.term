using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace _TERM_
{
    [DisallowMultipleComponent]
    public partial class TermServer : MonoBehaviour
    {
        [Header("TCP Server")]
        [SerializeField] string address = "127.0.0.1";
        [SerializeField, Min(1)] int port = 5050;

        TcpListener listener;
        TcpClient activeClient;
        CancellationTokenSource cancellation;
        Task listenerTask;

        //----------------------------------------------------------------------------------------------------------

        private void Start()
        {
            StartServer();
        }

        private void OnDestroy()
        {
            StopServer();
        }

        [ContextMenu(nameof(StartServer))]
        void StartServer()
        {
            if (listener != null)
                return;

            if (!IPAddress.TryParse(address, out IPAddress ipAddress))
            {
                Debug.LogError($"[TERM] Invalid server address: {address}", this);
                return;
            }

            try
            {
                cancellation = new CancellationTokenSource();
                listener = new TcpListener(ipAddress, port);
                listener.Start();
                listenerTask = AcceptClientsAsync(listener, cancellation.Token);
                Debug.Log($"[TERM] TCP server listening on {address}:{port}", this);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                StopServer();
            }
        }

        [ContextMenu(nameof(StopServer))]
        void StopServer()
        {
            CancellationTokenSource currentCancellation = cancellation;
            TcpListener currentListener = listener;

            cancellation = null;
            listener = null;
            listenerTask = null;

            currentCancellation?.Cancel();
            currentListener?.Stop();

            activeClient?.Close();
            activeClient = null;
            currentCancellation?.Dispose();
        }

        async Task AcceptClientsAsync(TcpListener activeListener, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    activeClient = await activeListener.AcceptTcpClientAsync();
                    await HandleClientAsync(activeClient, token);
                    activeClient = null;
                }
            }
            catch (ObjectDisposedException) when (token.IsCancellationRequested)
            {
            }
            catch (SocketException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                if (!token.IsCancellationRequested)
                    Debug.LogException(exception, this);
            }
        }

        async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (var reader = new StreamReader(stream, new UTF8Encoding(false), false, 1024, true))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true) { AutoFlush = true })
                {
                    await SendAsync(writer, new ServerMessage
                    {
                        type = "welcome",
                        text = "Connected to TERM. Type 'help' to list commands."
                    });

                    while (!token.IsCancellationRequested && client.Connected)
                    {
                        string line = await reader.ReadLineAsync();
                        if (line == null)
                            break;

                        ClientMessage request;
                        try
                        {
                            request = JsonUtility.FromJson<ClientMessage>(line);
                        }
                        catch (ArgumentException)
                        {
                            await SendAsync(writer, Error("Invalid JSON request."));
                            continue;
                        }

                        if (request == null || request.type != "command")
                        {
                            await SendAsync(writer, Error("Expected a message of type 'command'."));
                            continue;
                        }

                        ServerMessage response = ExecuteCommand(request.text ?? string.Empty);
                        await SendAsync(writer, response);

                        if (response.close)
                            break;
                    }
                }
            }
            catch (Exception exception) when (
                token.IsCancellationRequested ||
                exception is IOException ||
                exception is ObjectDisposedException ||
                exception is SocketException)
            {
            }
            finally
            {
                if (ReferenceEquals(activeClient, client))
                    activeClient = null;
            }
        }

        static Task SendAsync(StreamWriter writer, ServerMessage message)
        {
            return writer.WriteLineAsync(JsonUtility.ToJson(message));
        }

        static ServerMessage ExecuteCommand(string input)
        {
            string commandLine = input.Trim();
            if (commandLine.Length == 0)
                return Result(string.Empty);

            int separator = commandLine.IndexOf(' ');
            string command = (separator < 0 ? commandLine : commandLine.Substring(0, separator)).ToLowerInvariant();
            string arguments = separator < 0 ? string.Empty : commandLine.Substring(separator + 1).Trim();

            switch (command)
            {
                case "help":
                    return Result(
                        "Commands:\n" +
                        "  help              Show this help\n" +
                        "  echo <text>       Return text\n" +
                        "  upper <text>      Convert text to uppercase\n" +
                        "  add <a> <b>       Add two numbers\n" +
                        "  time              Show server local time\n" +
                        "  quit              Disconnect");
                case "echo":
                    return Result(arguments);
                case "upper":
                    return Result(arguments.ToUpperInvariant());
                case "add":
                    return Add(arguments);
                case "time":
                    return Result(DateTimeOffset.Now.ToString("O"));
                case "quit":
                case "exit":
                    return new ServerMessage { type = "result", text = "Goodbye.", close = true };
                default:
                    return Error($"Unknown command: {command}. Type 'help' for available commands.");
            }
        }

        private static ServerMessage Add(string arguments)
        {
            string[] values = arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length != 2 ||
                !double.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double a) ||
                !double.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double b))
            {
                return Error("Usage: add <a> <b>");
            }

            return Result((a + b).ToString(CultureInfo.InvariantCulture));
        }

        private static ServerMessage Result(string text)
        {
            return new ServerMessage { type = "result", text = text };
        }

        private static ServerMessage Error(string text)
        {
            return new ServerMessage { type = "error", text = text };
        }

        [Serializable]
        private sealed class ClientMessage
        {
            public string type = string.Empty;
            public string text = string.Empty;
        }

        [Serializable]
        private sealed class ServerMessage
        {
            public string type = string.Empty;
            public string text = string.Empty;
            public bool close;
        }
    }
}
