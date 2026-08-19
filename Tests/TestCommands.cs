using System.Collections.Generic;
using UnityEngine;

namespace _TERM_.Tests
{
    static partial class TestCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoad()
        {
            TermServer.root_commands.AddCommand(new(
                name: "echo",
                owner: null,
                parse: static (reader, context) =>
                {
                    if (reader.TryRead(out string output))
                        context.args.Add(output);
                    else
                        return $"expected argument";
                    return null;
                },
                function: static context =>
                {
                    return (string)context.args[0];
                }
            ));

            TermServer.root_commands.AddCommand(new(
                name: "wait_seconds",
                owner: null,
                parse: static (reader, context) =>
                {
                    if (reader.TryRead(out string output) && float.TryParse(output, out float seconds))
                        context.args.Add(seconds);
                    else
                        context.args.Add(0);
                    return null;
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

            TermServer.root_commands.AddCommand(new(
                name: "wait_1second",
                owner: null,
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

            TermServer.root_commands.AddCommand(new(
                name: "prompt_test",
                owner: null,
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
            TermServer.root_commands.AddNamespace(ns = new("ns1", owner: null));
            ns.AddNamespace(ns = new("ns2", owner: null));
            ns.AddNamespace(ns = new("ns3", owner: null));
            ns.AddCommand(new(
                name: "test",
                owner: null,
                parse: static (reader, context) =>
                {
                    reader.TryRead(out string output);
                    context.args.Add(output);
                    return null;
                },
                function: static context =>
                {
                    return (string)context.args[0];
                }
            ));
        }
    }
}
