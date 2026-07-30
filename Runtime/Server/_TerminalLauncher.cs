using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace _TERM_
{
    partial class TermServer
    {
        sealed class TerminalInstance
        {
            public readonly Process process;
            public readonly string title;

            public TerminalInstance(Process process, string title)
            {
                this.process = process;
                this.title = title;
            }
        }

        readonly List<TerminalInstance> terminal_instances = new();
        int terminal_sequence;
        bool warned_about_linux_focus;

        //----------------------------------------------------------------------------------------------------------

        void TickTerminalLauncher()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Application.isBatchMode || !Input.GetKeyDown(terminal_key))
                return;

            bool forceNew = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            OpenTerminal(forceNew);
#endif
        }

        //----------------------------------------------------------------------------------------------------------

        public void OpenTerminal(bool forceNew = false)
        {
            if (Application.isBatchMode)
                return;

            if (!forceNew && FocusExistingTerminal())
                return;

            string executable = ResolveTerminalExecutable(out string searchedPaths);
            if (executable == null)
            {
                Debug.LogError($"[TERM] Client executable not found. Build it with TERM_python/build.py.\nSearched:\n{searchedPaths}", this);
                return;
            }

            string title = GetTerminalTitle(forceNew);
            Process process = StartTerminal(executable, title);
            if (process != null)
                terminal_instances.Add(new TerminalInstance(process, title));
        }

        //----------------------------------------------------------------------------------------------------------

        string GetTerminalTitle(bool forceNew)
        {
            string title = $"Terminal for {Application.productName}";
            terminal_sequence++;
            return forceNew ? $"{title} #{terminal_sequence}" : title;
        }

        string ResolveTerminalExecutable(out string searchedPaths)
        {
            string executableName = GetTerminalExecutableName();
            if (executableName == null)
            {
                searchedPaths = $"Unsupported platform: {Application.platform}";
                return null;
            }

            string root = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var candidates = new List<string>();

            AddExecutableCandidate(candidates, Environment.GetEnvironmentVariable("TERM_CLIENT_PATH"), root);
            AddExecutableCandidate(candidates, terminal_executable, root);
            AddExecutableCandidate(candidates, Path.Combine(root, "TERM", executableName), root);
            AddExecutableCandidate(candidates, Path.Combine(Application.streamingAssetsPath, "TERM", executableName), root);

#if UNITY_EDITOR
            string repositoriesRoot = Directory.GetParent(root)?.FullName;
            if (repositoriesRoot != null)
                AddExecutableCandidate(candidates, Path.Combine(repositoriesRoot, "TERM_python", "dist", executableName), root);
#endif

            searchedPaths = string.Join("\n", candidates);

            foreach (string candidate in candidates)
                if (File.Exists(candidate))
                    return candidate;

            return null;
        }

        static void AddExecutableCandidate(List<string> candidates, string path, string root)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
                path = Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(root, path));

                if (!candidates.Contains(path))
                    candidates.Add(path);
            }
            catch (Exception)
            {
            }
        }

        static string GetTerminalExecutableName()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "unity-term.exe";

                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxPlayer:
                    return "unity-term";

                default:
                    return null;
            }
        }

        //----------------------------------------------------------------------------------------------------------

        Process StartTerminal(string executable, string title)
        {
            string clientArguments = BuildClientArguments(title);

            try
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                        return Process.Start(new ProcessStartInfo(executable, clientArguments)
                        {
                            UseShellExecute = true,
                            WorkingDirectory = Path.GetDirectoryName(executable),
                        });

                    case RuntimePlatform.LinuxEditor:
                    case RuntimePlatform.LinuxPlayer:
                        return StartLinuxTerminal(executable, clientArguments, title);

                    default:
                        Debug.LogError($"[TERM] Terminal launch is not supported on {Application.platform}.", this);
                        return null;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return null;
            }
        }

        string BuildClientArguments(string title)
        {
            return $"--host 127.0.0.1 --command-port {port_cmd} --log-port {port_log} --title {QuoteArgument(title)}";
        }

        Process StartLinuxTerminal(string executable, string clientArguments, string title)
        {
            string command = $"{QuoteArgument(executable)} {clientArguments}";
            var launchers = new (string executable, string arguments)[]
            {
                ("x-terminal-emulator", $"-T {QuoteArgument(title)} -e {command}"),
                ("gnome-terminal", $"--wait --title {QuoteArgument(title)} -- {command}"),
                ("konsole", $"--separate -p {QuoteArgument($"tabtitle={title}")} -e {command}"),
                ("xfce4-terminal", $"--disable-server --title {QuoteArgument(title)} --execute {command}"),
                ("kitty", $"--title {QuoteArgument(title)} {command}"),
                ("alacritty", $"--title {QuoteArgument(title)} -e {command}"),
                ("xterm", $"-T {QuoteArgument(title)} -e {command}"),
            };

            foreach ((string launcher, string arguments) in launchers)
                try
                {
                    return Process.Start(new ProcessStartInfo(launcher, arguments)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(executable),
                    });
                }
                catch (Win32Exception)
                {
                }

            Debug.LogError("[TERM] No supported terminal emulator found.", this);
            return null;
        }

        static string QuoteArgument(string value)
        {
            return $"\"{value.Replace("\"", "\\\"")}\"";
        }

        //----------------------------------------------------------------------------------------------------------

        bool FocusExistingTerminal()
        {
            for (int i = terminal_instances.Count - 1; i >= 0; i--)
            {
                TerminalInstance instance = terminal_instances[i];

                if (IsWindows() && FocusWindowsTerminal(instance))
                    return true;

                if (IsLinux())
                {
                    if (FocusLinuxTerminal(instance.title, out bool focusToolFound))
                        return true;

                    if (IsProcessRunning(instance.process))
                    {
                        if (!warned_about_linux_focus)
                        {
                            string detail = focusToolFound ? "No matching window was found." : "Install wmctrl or xdotool to enable focusing.";
                            Debug.LogWarning($"[TERM] The terminal is still running, but Unity could not focus it. {detail}", this);
                            warned_about_linux_focus = true;
                        }

                        return true;
                    }
                }

                if (!IsProcessRunning(instance.process))
                {
                    instance.process?.Dispose();
                    terminal_instances.RemoveAt(i);
                }
            }

            return false;
        }

        static bool IsProcessRunning(Process process)
        {
            if (process == null)
                return false;

            try
            {
                return !process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        static bool IsWindows()
        {
            return Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer;
        }

        static bool IsLinux()
        {
            return Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer;
        }

        //----------------------------------------------------------------------------------------------------------

        static bool FocusWindowsTerminal(TerminalInstance instance)
        {
            IntPtr handle = IntPtr.Zero;

            if (IsProcessRunning(instance.process))
                try
                {
                    instance.process.Refresh();
                    handle = instance.process.MainWindowHandle;
                }
                catch (InvalidOperationException)
                {
                }

            if (handle == IntPtr.Zero)
                handle = FindWindow(null, instance.title);
            if (handle == IntPtr.Zero)
                return false;

            ShowWindowAsync(handle, 9);
            SetForegroundWindow(handle);
            return true;
        }

        static bool FocusLinuxTerminal(string title, out bool focusToolFound)
        {
            focusToolFound = false;

            if (RunFocusCommand("wmctrl", $"-a {QuoteArgument(title)}", out bool wmctrlFound))
                return true;
            focusToolFound |= wmctrlFound;

            if (RunFocusCommand("xdotool", $"search --name {QuoteArgument(title)} windowactivate", out bool xdotoolFound))
                return true;
            focusToolFound |= xdotoolFound;

            return false;
        }

        static bool RunFocusCommand(string executable, string arguments, out bool executableFound)
        {
            executableFound = false;

            try
            {
                using Process process = Process.Start(new ProcessStartInfo(executable, arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });

                executableFound = process != null;
                return process != null && process.WaitForExit(500) && process.ExitCode == 0;
            }
            catch (Win32Exception)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                executableFound = true;
                return false;
            }
        }

        //----------------------------------------------------------------------------------------------------------

        void DisposeTerminalLauncher()
        {
            foreach (TerminalInstance instance in terminal_instances)
                instance.process?.Dispose();

            terminal_instances.Clear();
        }

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll")]
        static extern bool ShowWindowAsync(IntPtr window, int command);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindow(string className, string windowName);
    }
}
