using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        //----------------------------------------------------------------------------------------------------------

        async Task HandleCommandConnectionAsync(ClientConnection connection, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    string json = await connection.reader.ReadLineAsync();
                    if (json == null)
                        break;

                    JObject jrequest;
                    try
                    {
                        jrequest = JsonConvert.DeserializeObject<JObject>(json);
                    }
                    catch (ArgumentException)
                    {
                        await connection.SendAsync(Error("Invalid JSON."));
                        continue;
                    }

                    JObject jresponse;

                    switch ((string)jrequest["type"])
                    {
                        // TAB demande des propositions sans exécuter.
                        case "complete":
                            jresponse = new JObject()
                            {
                                ["type"] = "completion",
                                ["candidates"] = JsonConvert.SerializeObject(Complete((string)jrequest["text"]), Formatting.None),
                            };
                            break;

                        // ENTRÉE exécute et attend le résultat.
                        case "execute":
                            jresponse = await ExecuteCommandAsync((string)jrequest["text"]);
                            break;

                        default:
                            jresponse = Error("Expected 'complete' or 'execute'.");
                            break;
                    }

                    // Réponse directe sur la connexion qui a émis la requête.
                    await connection.SendAsync(jresponse);

                    if (jresponse.ContainsKey("close"))
                        break;
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
                lock (connectionsLock)
                    commandConnections.Remove(connection);

                connection.Dispose();
            }
        }

        //----------------------------------------------------------------------------------------------------------

        static string[] Complete(string text)
        {
            string input = (text ?? string.Empty).TrimStart();

            // Démo simple : complétion du premier mot seulement.
            if (input.Contains(" "))
                return Array.Empty<string>();

            string[] commands = { "help", "ping", "echo", "wait", "quit" };
            var matches = new List<string>();

            foreach (string command in commands)
            {
                if (command.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    matches.Add(command);
            }

            return matches.ToArray();
        }

        //----------------------------------------------------------------------------------------------------------

        static async Task<JObject> ExecuteCommandAsync(string text)
        {
            string commandLine = (text ?? string.Empty).Trim();
            int separator = commandLine.IndexOf(' ');
            string command = (separator < 0 ? commandLine : commandLine[..separator]).ToLowerInvariant();
            string arguments = separator < 0 ? string.Empty : commandLine[(separator + 1)..];

            switch (command)
            {
                case "help":
                    return Result("Commands: help, ping, echo <text>, wait, quit");

                case "ping":
                    return Result("pong");

                case "echo":
                    return Result(arguments);

                case "wait":
                    // Le prompt attend, mais le canal logs continue indépendamment.
                    Debug.Log("[TERM test] wait started");
                    await Task.Delay(2000);
                    Debug.Log("[TERM test] wait finished");
                    return Result("wait finished");

                case "quit":
                case "exit":
                    return new JObject
                    {
                        ["type"] = "result",
                        ["text"] = "Goodbye.",
                        ["close"] = true,
                    };

                default:
                    return Error($"Unknown command: {command}");
            }
        }

        //----------------------------------------------------------------------------------------------------------

        static JObject Result(string text) => new()
        {
            ["type"] = "result",
            ["text"] = text,
        };

        static JObject Error(string text) => new()
        {
            ["type"] = "error",
            ["text"] = text,
        };
    }
}
