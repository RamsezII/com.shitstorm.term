using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace _TERM_
{
    partial class TermServer
    {
        void OnUnityLog(string message, string stackTrace, LogType logType)
        {
            ClientConnection[] snapshot;

            // Seules les connexions du port de logs reçoivent le broadcast.
            lock (connectionsLock)
                snapshot = logConnections.ToArray();

            var response = new JObject
            {
                ["type"] = "log",
                ["text"] = $"[{logType}] {message}",
            };

            foreach (ClientConnection connection in snapshot)
                _ = connection.SendAsync(response);
        }

        //----------------------------------------------------------------------------------------------------------

        async Task AcceptLogConnectionsAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    TcpClient tcpClient = await log_listener.AcceptTcpClientAsync();
                    var connection = new ClientConnection(tcpClient);

                    lock (connectionsLock)
                        logConnections.Add(connection);

                    // Le canal est serveur -> client. Cette tâche détecte seulement sa fermeture par le terminal.
                    _ = WatchLogConnectionAsync(connection, token);

                    await connection.SendAsync(new JObject
                    {
                        ["type"] = "info",
                        ["text"] = "Unity log stream connected.",
                    });
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

        async Task WatchLogConnectionAsync(ClientConnection connection, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                    if (await connection.reader.ReadLineAsync() == null)
                        break;
            }
            catch (Exception exception) when (
                token.IsCancellationRequested ||
                exception is IOException ||
                exception is ObjectDisposedException ||
                exception is SocketException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                lock (connectionsLock)
                    logConnections.Remove(connection);

                connection.Dispose();
            }
        }
    }
}
