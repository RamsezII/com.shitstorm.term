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
            lock (log_connections)
                foreach (var conn in log_connections)
                    if (logType == LogType.Exception)
                        _ = conn.ASend(new LogClient.LogResponse_exception(message, stackTrace));
                    else
                        _ = conn.ASend(new LogClient.LogResponse((LogClient.LogTypes)logType, message));
        }

        //----------------------------------------------------------------------------------------------------------

        async Task AcceptLogConnectionsAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    TcpClient tcpClient = await log_listener.AcceptTcpClientAsync();
                    var connection = new LogClient(tcpClient);

                    lock (log_connections)
                        log_connections.Add(connection);

                    // Le canal est serveur -> client. Cette tâche détecte seulement sa fermeture par le terminal.
                    _ = WatchLogConnectionAsync(connection, token);

                    await connection.ASend(new LogClient.LogResponse(
                        type: LogClient.LogTypes.log,
                        message: "Unity log stream connected."
                    ));
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

        async Task WatchLogConnectionAsync(LogClient connection, CancellationToken token)
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
                lock (log_connections)
                    log_connections.Remove(connection);

                connection.Dispose();
            }
        }
    }
}
