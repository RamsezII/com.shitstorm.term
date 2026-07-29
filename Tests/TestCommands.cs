using System.Collections.Generic;
using UnityEngine;

namespace _TERM_.Tests
{
    static partial class TestCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoad()
        {
            TermServer.root_commands.AddCommandNode(new CmdCommand(
                name: "echo",
                args: static (reader, handler) =>
                {
                    if (reader.TryRead(out string output))
                        handler.args.Add(output);
                    else
                        reader.WriteError($"expected argument");
                },
                action1: static handler =>
                {
                    return (string)handler.args[0];
                }
            ));

            TermServer.root_commands.AddCommandNode(new CmdCommand(
                name: "wait_seconds",
                args: static (reader, handler) =>
                {
                    if (reader.TryRead(out string output) && float.TryParse(output, out float seconds))
                        handler.args.Add(seconds);
                    else
                        handler.args.Add(0);
                },
                routine2: static (handler, hreader) =>
                {
                    float seconds = (float)handler.args[0];
                    return EWait(seconds);
                    static IEnumerator<CmdCommand.RoutineStatus> EWait(float seconds)
                    {
                        float timer = 0;
                        while (timer < seconds)
                        {
                            timer += Time.unscaledDeltaTime;
                            yield return default;
                        }
                    }
                }
            ));

            TermServer.root_commands.AddCommandNode(new CmdCommand(
                name: "wait_1second",
                routine2: static (handler, hreader) =>
                {
                    return EWait();
                    static IEnumerator<CmdCommand.RoutineStatus> EWait()
                    {
                        float timer = 0;
                        while (timer < 1)
                        {
                            timer += Time.unscaledDeltaTime;
                            yield return default;
                        }
                    }
                }
            ));

            TermServer.root_commands.AddCommandNode(new CmdCommand(
                name: "prompt_test",
                routine2: static (handler, hreader) =>
                {
                    return EPromptTest(hreader);

                    static IEnumerator<CmdCommand.RoutineStatus> EPromptTest(CmdCommand.ReadHandler hreader)
                    {
                        yield return new(prompt: "Ton nom", progress: 0, result: null);
                        string name = hreader.reader.line.Trim();

                        yield return new(prompt: "Ta couleur préférée", progress: 0.5f, result: null);
                        string color = hreader.reader.line.Trim();

                        yield return new(prompt: null, progress: 1, result: $"Salut {name}, ta couleur préférée est {color}.");
                    }
                }
            ));

            CmdNamespace ns = null;
            TermServer.root_commands.AddCommandNode(ns = new("ns1"));
            ns.AddCommandNode(ns = new("ns2"));
            ns.AddCommandNode(ns = new("ns3"));
            ns.AddCommandNode(new CmdCommand(
                name: "test",
                args: static (reader, handler) =>
                {
                    reader.TryRead(out string output);
                    handler.args.Add(output);
                },
                action1: static handler =>
                {
                    return (string)handler.args[0];
                }
            ));
        }
    }
}
