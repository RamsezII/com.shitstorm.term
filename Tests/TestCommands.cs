using System.Collections.Generic;
using UnityEngine;

namespace _TERM_.Tests
{
    static partial class TestCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoad()
        {
            TermServer.root_namespace.AddCommand("echo", static (reader, context) =>
            {
                if (reader.TryRead(out string output))
                {
                    context.list_args.Add(output);
                    return (CmdExecution)(static context => (string)context.list_args[0]);
                }

                reader.Error($"expected argument");
                return null;
            });

            TermServer.root_namespace.AddCommand("wait_seconds", static (reader, context) =>
            {
                if (!reader.TryRead(out string output) || !float.TryParse(output, out float seconds))
                    seconds = 0;
                context.list_args.Add(seconds);

                return (CmdExecution)EWait;
                static IEnumerator<CmdStep> EWait(CmdContext context)
                {
                    float timer = 0;
                    float seconds = (float)context.list_args[0];
                    while (timer < seconds)
                    {
                        timer += Time.unscaledDeltaTime;
                        yield return default;
                    }
                }
            });

            TermServer.root_namespace.AddCommand("wait_1second", static (reader, context) =>
            {
                return (CmdExecution)EWait;
                static IEnumerator<CmdStep> EWait(CmdContext context)
                {
                    float timer = 0;
                    while (timer < 1)
                    {
                        timer += Time.unscaledDeltaTime;
                        yield return default;
                    }
                }
            });

            TermServer.root_namespace.AddCommand("prompt_test", static (reader, context) =>
            {
                return (CmdExecution)EPromptTest;
                static IEnumerator<CmdStep> EPromptTest(CmdContext context)
                {
                    yield return CmdStep.Prompt("name: ", static reader =>
                    {
                        reader.TryRead(out _);
                        if (reader.IsOnCompletion())
                            reader.AddCompletions((IEnumerable<string>)(new[] { "Josué", "Devante", "ShittyG", }));
                        else
                        {
                            reader.TryRead(out _);
                            if (reader.IsOnCompletion())
                                reader.AddCompletions((IEnumerable<string>)(new[] { "Jamaguil", "Vinquoas-Copernicus-Cock", }));
                        }
                    });

                    context.Reader.TryRead(out string name1);
                    context.Reader.TryRead(out string name2);

                    yield return CmdStep.Prompt("colors: ", static reader =>
                    {
                        reader.TryRead(out _);
                        if (reader.IsOnCompletion())
                            reader.AddCompletions((IEnumerable<string>)(new[] { "bleu", "jaune", "rouge", "vert" }));
                    });

                    context.Reader.TryRead(out string color);

                    yield return CmdStep.Result($"Salut {name1} {name2}, ta couleur préférée est {color}.");
                }
            });

            TermServer.root_namespace.AddNamespace("ns1").AddNamespace("ns2").AddNamespace("ns3").AddCommand(
                name: "test",
                execution: static (reader, context) =>
                {
                    reader.TryRead(out string output);
                    context.list_args.Add(output);
                    return (CmdExecution)(static context => (string)context.list_args[0]);
                }
            );
        }
    }
}
