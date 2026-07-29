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
                action1: static reader =>
                {
                    if (!reader.TryRead(out string output))
                        output = string.Empty;
                    return output;
                }
            ));

            TermServer.root_commands.AddCommandNode(new CmdCommand(
                name: "wait_seconds",
                action1: static reader =>
                {
                    if (!reader.TryRead(out string output))
                        output = string.Empty;
                    return output;
                }
            ));

            TermServer.root_commands.AddCommandNode(new CmdCommand(
                name: "wait_1second",
                routine2: static reader =>
                {
                    return EWait(reader);
                    static IEnumerator<CmdCommand.RoutineStatus> EWait(CmdLineReader reader)
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
        }
    }
}