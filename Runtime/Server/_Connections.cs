using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace _TERM_
{
    partial class TermServer
    {
        sealed class ClientConnection : IDisposable
        {
            readonly TcpClient tcpClient;
            readonly StreamWriter writer;
            readonly SemaphoreSlim writeLock = new(1, 1);

            public readonly StreamReader reader;

            //----------------------------------------------------------------------------------------------------------

            public ClientConnection(TcpClient tcpClient)
            {
                this.tcpClient = tcpClient;

                NetworkStream stream = tcpClient.GetStream();

                reader = new StreamReader(stream, new UTF8Encoding(false), false, 1024, true);

                writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true)
                {
                    AutoFlush = true
                };
            }

            //----------------------------------------------------------------------------------------------------------

            public async Task SendAsync(JObject response)
            {
                string json = JsonConvert.SerializeObject(response, Formatting.None);

                await writeLock.WaitAsync();

                try
                {
                    await writer.WriteLineAsync(json);
                }
                catch (Exception exception) when (
                    exception is IOException ||
                    exception is ObjectDisposedException ||
                    exception is SocketException)
                {
                }
                finally
                {
                    writeLock.Release();
                }
            }

            //----------------------------------------------------------------------------------------------------------

            public void Dispose()
            {
                tcpClient.Close();
            }
        }

        readonly List<ClientConnection> commandConnections = new();
        readonly List<ClientConnection> logConnections = new();
        readonly object connectionsLock = new();

        //----------------------------------------------------------------------------------------------------------

        async Task AcceptCommandConnectionsAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    TcpClient tcpClient = await cmd_listener.AcceptTcpClientAsync();
                    var connection = new ClientConnection(tcpClient);

                    lock (connectionsLock)
                        commandConnections.Add(connection);

                    _ = HandleCommandConnectionAsync(connection, token);
                }
            }
            catch (ObjectDisposedException) when (token.IsCancellationRequested)
            {
            }
            catch (SocketException) when (token.IsCancellationRequested)
            {
            }
        }

        //----------------------------------------------------------------------------------------------------------

        void CloseAllConnections()
        {
            lock (connectionsLock)
            {
                foreach (ClientConnection connection in commandConnections)
                    connection.Dispose();

                foreach (ClientConnection connection in logConnections)
                    connection.Dispose();

                commandConnections.Clear();
                logConnections.Clear();
            }
        }
    }
}
