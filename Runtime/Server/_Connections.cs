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

                    try
                    {
                        while (!token.IsCancellationRequested)
                            try
                            {
                                string json = await connection.reader.ReadLineAsync();
                                if (json == null)
                                    break;

                                lock (routines)
                                    routines.Add(EOnIncomingCommand(connection, json));
                            }
                            catch (Exception e)
                            {
                                await connection.ASend(new CmdClient.CmdResponse_exception(e));
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
                    }
                    finally
                    {
                        lock (cmd_connections)
                            cmd_connections.Remove(connection);
                        connection.Dispose();
                    }
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
            lock (cmd_connections)
            {
                foreach (TermClient connection in cmd_connections)
                    connection.Dispose();
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