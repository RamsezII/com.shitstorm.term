#if UNITY_EDITOR
using _ARK_;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

namespace _TERM_.Editor
{
    static class BuildProcessor
    {
        [PostProcessBuild(0)]
        static void CopyTermClient(BuildTarget target, string builtPlayerPath)
        {
            if (target != BuildTarget.StandaloneWindows64)
                return;

            const string executableName = "unity-term.exe";
            string source = Path.Combine(
                Application.dataPath,
                "_TERM_",
                "Editor",
                "Binaries",
                "Windows-x64",
                executableName);

            if (!File.Exists(source))
                throw new BuildFailedException($"TERM client absent : {source}");

            string buildRoot = Path.GetDirectoryName(builtPlayerPath) ?? throw new BuildFailedException($"Chemin de build invalide : {builtPlayerPath}");

            string toolsDirectory = Path.Combine(buildRoot, ArkMachine.dname_tools);
            Directory.CreateDirectory(toolsDirectory);

            string destination = Path.Combine(toolsDirectory, executableName);

            File.Copy(source, destination, overwrite: true);

            Debug.Log($"[TERM] Client copié : {destination}");
        }
    }
}
#endif
