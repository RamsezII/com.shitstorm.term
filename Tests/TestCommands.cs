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
                parse: static (reader, context) =>
                {
                    if (reader.TryRead(out string output))
                        context.args.Add(output);
                    else
                        reader.WriteError($"expected argument");
                },
                execute: static context =>
                {
                    return (string)context.args[0];
                }
            ));

            TermServer.root_commands.AddCommandNode(new CmdCommand(
                name: "wait_seconds",
                parse: static (reader, context) =>
                {
                    if (reader.TryRead(out string output) && float.TryParse(output, out float seconds))
                        context.args.Add(seconds);
                    else
                        context.args.Add(0);
                },
                routine: static context =>
                {
                    float seconds = (float)context.args[0];
                    return EWait(seconds);
                    static IEnumerator<CmdStep> EWait(float seconds)
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
                routine: static context =>
                {
                    return EWait();
                    static IEnumerator<CmdStep> EWait()
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
                routine: static context =>
                {
                    return EPromptTest(context);

                    static IEnumerator<CmdStep> EPromptTest(CmdContext context)
                    {
                        yield return CmdStep.Prompt("Ton nom", CompleteNames);
                        context.reader.TryRead(out string name);

                        yield return CmdStep.Prompt("Ta couleur préférée", CompleteColors);
                        context.reader.TryRead(out string color);

                        yield return CmdStep.Result($"Salut {name}, ta couleur préférée est {color}.");

                        static void CompleteNames(CmdReader reader)
                        {
                            reader.TryRead(out _);

                            if (reader.IsOnCompletion())
                                reader.AddCompletions(new[] { "Josué", "Devante", "ShittyG" });
                        }

                        static void CompleteColors(CmdReader reader)
                        {
                            reader.TryRead(out _);

                            if (reader.IsOnCompletion())
                                reader.AddCompletions(new[] { "bleu", "jaune", "rouge", "vert" });
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
                parse: static (reader, context) =>
                {
                    reader.TryRead(out string output);
                    context.args.Add(output);
                },
                execute: static context =>
                {
                    return (string)context.args[0];
                }
            ));
        }
    }
}
