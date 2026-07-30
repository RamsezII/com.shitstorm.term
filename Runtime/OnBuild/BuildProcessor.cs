#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

namespace _TERM_
{
    static class BuildProcesseur
    {
        [PostProcessBuild(0)]
        static void CopyTermClient(BuildTarget target, string builtPlayerPath)
        {
            string executableName = target switch
            {
                BuildTarget.StandaloneWindows64 => "unity-term.exe",
                BuildTarget.StandaloneLinux64 => "unity-term",
                _ => null,
            };

            if (executableName == null)
                return;

            string projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));

            string repositoriesRoot =
                Directory.GetParent(projectRoot)?.FullName
                ?? throw new BuildFailedException(
                    "Impossible de trouver la racine des dépôts.");

            string source = Path.Combine(
                repositoriesRoot,
                "TERM_python",
                "dist",
                executableName);

            if (!File.Exists(source))
                throw new BuildFailedException(
                    $"TERM client absent : {source}");

            string buildRoot =
                Path.GetDirectoryName(builtPlayerPath)
                ?? throw new BuildFailedException(
                    $"Chemin de build invalide : {builtPlayerPath}");

            string toolsDirectory = Path.Combine(buildRoot, "Tools");
            Directory.CreateDirectory(toolsDirectory);

            string destination =
                Path.Combine(toolsDirectory, executableName);

            File.Copy(source, destination, overwrite: true);

            Debug.Log($"[TERM] Client copié : {destination}");
        }
    }
}
#endif