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
