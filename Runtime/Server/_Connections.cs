using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace _TERM_
{
    partial class TermServer
    {
        readonly List<CmdClient> cmd_connections = new();
        readonly List<LogClient> log_connections = new();

        //----------------------------------------------------------------------------------------------------------

        async Task AcceptCommandConnectionsAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    TcpClient tcpClient = await cmd_listener.AcceptTcpClientAsync();
                    var connection = new CmdClient(tcpClient);

                    lock (cmd_connections)
                        cmd_connections.Add(connection);

                    _ = HandleCommandConnectionAsync(connection, token);
                }
            }
            catch (ObjectDisposedException) when (token.IsCancellationRequested)
            {
            }
            catch (SocketException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        //----------------------------------------------------------------------------------------------------------

        async Task HandleCommandConnectionAsync(CmdClient connection, CancellationToken token)
        {
            try
            {
                await connection.ASend(new CmdClient.CmdResponse_intro());

                lock (routines)
                    routines.Add(ECommandSession(connection));

                while (!token.IsCancellationRequested)
                {
                    string rawtext = await connection.reader.ReadLineAsync();
                    if (rawtext == null)
                        break;

                    if (!CmdClient.CmdRequest.TryDeserialize(rawtext, out var request, out string error))
                    {
                        connection.Enqueue(CmdClient.CmdRequest.Invalid(error));
                        continue;
                    }

                    connection.Enqueue(request);
                }
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
                await connection.ASend(new CmdClient.CmdResponse_exception(e));
            }
            finally
            {
                connection.CloseRequests();

                lock (cmd_connections)
                    cmd_connections.Remove(connection);

                connection.Dispose();
            }
        }

        //----------------------------------------------------------------------------------------------------------

        void CloseAllConnections()
        {
            lock (cmd_connections)
            {
                foreach (CmdClient connection in cmd_connections)
                {
                    connection.CloseRequests();
                    connection.Dispose();
                }
                cmd_connections.Clear();
            }

            lock (log_connections)
            {
                foreach (TermClient connection in log_connections)
                    connection.Dispose();
                log_connections.Clear();
            }
        }
    }
}
