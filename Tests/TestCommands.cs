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
        }
    }
}