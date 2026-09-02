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
            (string platformDirectory, string executableName) = target switch
            {
                BuildTarget.StandaloneWindows64 => ("Windows-x64", "unity-term.exe"),
                BuildTarget.StandaloneLinux64 => ("Linux-x64", "unity-term.x86_64"),
                _ => (null, null),
            };

            if (platformDirectory == null)
                return;

            string source = Path.Combine(
                Application.dataPath,
                "_TERM_",
                "Editor",
                "Binaries",
                platformDirectory,
                executableName);

            if (!File.Exists(source))
                throw new BuildFailedException($"TERM client absent : {source}");

            string buildRoot = Path.GetDirectoryName(builtPlayerPath) ?? throw new BuildFailedException($"Chemin de build invalide : {builtPlayerPath}");

            string toolsDirectory = Path.Combine(buildRoot, NUCLEOR.dname_tools);
            Directory.CreateDirectory(toolsDirectory);

            string destination = Path.Combine(toolsDirectory, executableName);

            File.Copy(source, destination, overwrite: true);
            EnsureLinuxExecutable(target, destination);

            Debug.Log($"[TERM] Client copié : {destination}");
        }

        static void EnsureLinuxExecutable(BuildTarget target, string executable)
        {
            if (target != BuildTarget.StandaloneLinux64 || Application.platform != RuntimePlatform.LinuxEditor)
                return;

            string escapedExecutable = executable.Replace("\"", "\\\"");
            using System.Diagnostics.Process chmod = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo("chmod", $"+x \"{escapedExecutable}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });

            if (chmod == null || !chmod.WaitForExit(5000) || chmod.ExitCode != 0)
                throw new BuildFailedException($"Impossible de rendre le client TERM exécutable : {executable}");
        }
    }
}
#endif
