using System.Collections.Generic;
using UnityEngine;

namespace _TERM_.Tests
{
    static partial class TestCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoad()
        {
            TermServer.root_namespace.AddCommand("echo", static context =>
            {
                if (context.Reader.TryRead(out string output))
                {
                    context.queue_args.Enqueue(output);
                    return new(static context => (string)context.queue_args.Dequeue());
                }

                context.Reader.Error($"expected argument");
                return null;
            });

            TermServer.root_namespace.AddCommand("wait_seconds", static context =>
            {
                if (!context.Reader.TryRead(out string output) || !float.TryParse(output, out float seconds))
                    seconds = 0;
                context.queue_args.Enqueue(seconds);

                return new(ERoutine);
                static IEnumerator<CmdStep> ERoutine(CmdContext context)
                {
                    float timer = 0;
                    float seconds = (float)context.queue_args.Dequeue();
                    while (timer < seconds)
                    {
                        timer += Time.unscaledDeltaTime;
                        yield return default;
                    }
                }
            });

            TermServer.root_namespace.AddCommand("wait_1second", static context =>
            {
                return new(ERoutine);
                static IEnumerator<CmdStep> ERoutine(CmdContext context)
                {
                    float timer = 0;
                    while (timer < 1)
                    {
                        timer += Time.unscaledDeltaTime;
                        yield return default;
                    }
                }
            });

            TermServer.root_namespace.AddCommand("prompt_test", static context =>
            {
                return new(ERoutine);
                static IEnumerator<CmdStep> ERoutine(CmdContext context)
                {
                    yield return CmdStep.Prompt("name: ", static reader =>
                    {
                        reader.TryRead(out string arg);
                        if (reader.IsOnCompletion())
                            reader.AddCompletions(arg, "Josué", "Devante", "ShittyG");
                        else
                        {
                            reader.TryRead(out arg);
                            if (reader.IsOnCompletion())
                                reader.AddCompletions(arg, "Jamaguil", "Diesel-Vincock");
                        }
                    });

                    context.Reader.TryRead(out string name1);
                    context.Reader.TryRead(out string name2);

                    yield return CmdStep.Prompt("colors: ", static reader =>
                    {
                        reader.TryRead(out var arg);
                        if (reader.IsOnCompletion())
                            reader.AddCompletions(arg, "bleu", "jaune", "rouge", "vert");
                    });

                    context.Reader.TryRead(out string color);

                    yield return CmdStep.Result($"Salut {name1} {name2}, ta couleur préférée est {color}.");
                }
            });

            TermServer.root_namespace.AddNamespace("ns1").AddNamespace("ns2").AddNamespace("ns3").AddCommand(
                name: "test",
                execution: static context =>
                {
                    context.Reader.TryRead(out string output);
                    context.queue_args.Enqueue(output);
                    return new(static context => (string)context.queue_args.Dequeue());
                }
            );
        }
    }
}
