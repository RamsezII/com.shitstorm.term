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
                args: static (reader, args) =>
                {
                    if (!reader.TryRead(out string output))
                        output = string.Empty;
                    args.Add(output);
                },
                action1: static (opts, args) =>
                {
                    return (string)args[0];
                }
            ));

            TermServer.root_commands.AddCommandNode(new CmdCommand(
                name: "wait_seconds",
                args: static (reader, args) =>
                {
                    if (reader.TryRead(out string output) && float.TryParse(output, out float seconds))
                        args.Add(seconds);
                    else
                        args.Add(0);
                },
                routine2: static (opts, args) =>
                {
                    float seconds = (float)args[0];
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
                routine2: static (opts, args) =>
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

            CmdNamespace ns = null;
            TermServer.root_commands.AddCommandNode(ns = new("ns1"));
            ns.AddCommandNode(ns = new("ns2"));
            ns.AddCommandNode(ns = new("ns3"));
            ns.AddCommandNode(new CmdCommand(
                name: "test",
                args: static (reader, args) =>
                {
                    reader.TryRead(out string output);
                    args.Add(output);
                },
                action1: static (opts, args) =>
                {
                    return (string)args[0];
                }
            ));
        }
    }
}