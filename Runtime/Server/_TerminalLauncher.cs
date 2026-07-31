using _ARK_;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace _TERM_
{
    partial class TermServer
    {
        sealed class TerminalInstance
        {
            public readonly string title;
            public readonly uint windowsProcessId;
            public readonly int linuxProcessId;

            public TerminalInstance(string title, uint windowsProcessId = 0, int linuxProcessId = 0)
            {
                this.title = title;
                this.windowsProcessId = windowsProcessId;
                this.linuxProcessId = linuxProcessId;
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

            if (cmd_listener == null || log_listener == null || port_cmd == 0 || port_log == 0)
            {
                Debug.LogError("[TERM] The server is not ready to accept terminal connections.", this);
                return;
            }

            if (!forceNew && FocusExistingTerminal())
                return;

            string executable = ResolveTerminalExecutable();
            if (executable == null)
            {
                Debug.LogError($"[TERM] Terminal launch is not supported on {Application.platform}.", this);
                return;
            }

            if (!File.Exists(executable))
            {
                Debug.LogError($"[TERM] Client executable not found: {executable}", this);
                return;
            }

            string title = GetTerminalTitle(forceNew);
            TerminalInstance instance = StartTerminal(executable, title);
            if (instance != null)
                terminal_instances.Add(instance);
        }

        //----------------------------------------------------------------------------------------------------------

        string GetTerminalTitle(bool forceNew)
        {
            string title = $"Terminal for {Application.productName}";
            terminal_sequence++;
            return forceNew ? $"{title} #{terminal_sequence}" : title;
        }

        static string ResolveTerminalExecutable()
        {
            string executableName = Application.platform switch
            {
                RuntimePlatform.WindowsEditor or RuntimePlatform.WindowsPlayer => "unity-term.exe",
                RuntimePlatform.LinuxEditor or RuntimePlatform.LinuxPlayer => "unity-term.x86_64",
                _ => null,
            };
            if (executableName == null)
                return null;

#if UNITY_EDITOR
            if (Application.isEditor)
            {
                string platformDirectory = Application.platform switch
                {
                    RuntimePlatform.WindowsEditor => "Windows-x64",
                    RuntimePlatform.LinuxEditor => "Linux-x64",
                    _ => null,
                };

                return platformDirectory == null
                    ? null
                    : Path.Combine(Application.dataPath, "_TERM_", "Editor", "Binaries", platformDirectory, executableName);
            }
#endif
            return Path.Combine(ArkMachine.DFTools.FullName, executableName);
        }

        //----------------------------------------------------------------------------------------------------------

        TerminalInstance StartTerminal(string executable, string title)
        {
            string clientArguments = $"--host 127.0.0.1 --command-port {port_cmd} --log-port {port_log} --title {QuoteArgument(title)}";

            try
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                        uint processId = StartWindowsTerminal(executable, clientArguments);
                        return new TerminalInstance(title, windowsProcessId: processId);

                    case RuntimePlatform.LinuxEditor:
                    case RuntimePlatform.LinuxPlayer:
                        int linuxProcessId = StartLinuxTerminal(executable, title);
                        return linuxProcessId <= 0 ? null : new TerminalInstance(title, linuxProcessId: linuxProcessId);

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

        static uint StartWindowsTerminal(string executable, string clientArguments)
        {
            var startupInfo = new StartupInfo
            {
                size = (uint)Marshal.SizeOf(typeof(StartupInfo)),
            };
            var commandLine = new StringBuilder($"{QuoteArgument(executable)} {clientArguments}");

            if (!CreateProcessW(
                    executable,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    CreateNewConsole,
                    IntPtr.Zero,
                    Path.GetDirectoryName(executable),
                    ref startupInfo,
                    out ProcessInformation processInformation))
            {
                int error = Marshal.GetLastWin32Error();
                if (error == 0)
                    throw new InvalidOperationException("[TERM] CreateProcessW failed without a Windows error code.");

                throw new Win32Exception(error);
            }

            try
            {
                return processInformation.processId;
            }
            finally
            {
                CloseHandle(processInformation.thread);
                CloseHandle(processInformation.process);
            }
        }

        int StartLinuxTerminal(string executable, string title)
        {
            string workingDirectory = Path.GetDirectoryName(executable);
            string[] clientArguments =
            {
                executable,
                "--host",
                "127.0.0.1",
                "--command-port",
                port_cmd.ToString(),
                "--log-port",
                port_log.ToString(),
                "--title",
                title,
            };

            if (File.Exists(XdgTerminalExec))
                return StartLinuxProcess(
                    XdgTerminalExec,
                    CombineArguments(
                        new[] { $"--title={title}", $"--dir={workingDirectory}", "--" },
                        clientArguments),
                    createSession: true);

            if (File.Exists(XTerminalEmulator))
                return StartLinuxProcess(
                    XTerminalEmulator,
                    CombineArguments(new[] { "-T", title, "-e" }, clientArguments),
                    createSession: true);

            Debug.LogError("[TERM] Ubuntu terminal launcher not found. Install xdg-terminal-exec.", this);
            return 0;
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

                if (IsWindows())
                {
                    if (FocusWindowsTerminal(instance))
                        return true;

                    if (IsWindowsProcessRunning(instance.windowsProcessId))
                        return true;

                    terminal_instances.RemoveAt(i);
                    continue;
                }

                if (IsLinux())
                {
                    if (FocusLinuxTerminal(instance.title, out bool focusToolFound))
                        return true;

                    if (IsLinuxProcessRunning(instance.linuxProcessId))
                    {
                        if (!warned_about_linux_focus)
                        {
                            string detail = focusToolFound ? "No matching window was found." : "Install wmctrl or xdotool to enable focusing.";
                            Debug.LogWarning($"[TERM] The terminal is still running, but Unity could not focus it. {detail}", this);
                            warned_about_linux_focus = true;
                        }

                        return true;
                    }

                    terminal_instances.RemoveAt(i);
                    continue;
                }

                terminal_instances.RemoveAt(i);
            }

            return false;
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
            IntPtr handle = FindWindow(null, instance.title);
            if (handle == IntPtr.Zero)
                return false;

            ShowWindowAsync(handle, 9);
            SetForegroundWindow(handle);
            return true;
        }

        static bool IsWindowsProcessRunning(uint processId)
        {
            if (processId == 0)
                return false;

            IntPtr process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
            if (process == IntPtr.Zero)
                return false;

            try
            {
                return GetExitCodeProcess(process, out uint exitCode) && exitCode == StillActive;
            }
            finally
            {
                CloseHandle(process);
            }
        }

        static bool FocusLinuxTerminal(string title, out bool focusToolFound)
        {
            focusToolFound = false;

            if (RunFocusCommand(Wmctrl, new[] { "-a", title }, out bool wmctrlFound))
                return true;
            focusToolFound |= wmctrlFound;

            if (RunFocusCommand(Xdotool, new[] { "search", "--name", title, "windowactivate" }, out bool xdotoolFound))
                return true;
            focusToolFound |= xdotoolFound;

            return false;
        }

        static bool RunFocusCommand(string executable, string[] arguments, out bool executableFound)
        {
            executableFound = File.Exists(executable);
            if (!executableFound)
                return false;

            try
            {
                int processId = StartLinuxProcess(executable, arguments, createSession: false);
                return WaitPid(processId, out int status, 0) == processId
                    && (status & 0x7f) == 0
                    && ((status >> 8) & 0xff) == 0;
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        static bool IsLinuxProcessRunning(int processId)
        {
            return processId > 0 && WaitPid(processId, out _, WaitNoHang) == 0;
        }

        static int StartLinuxProcess(string executable, string[] arguments, bool createSession)
        {
            var nativeStrings = new IntPtr[arguments.Length + 1];
            IntPtr nativeArguments = IntPtr.Zero;
            int processId = -1;

            try
            {
                nativeStrings[0] = Marshal.StringToHGlobalAnsi(executable);
                for (int i = 0; i < arguments.Length; i++)
                    nativeStrings[i + 1] = Marshal.StringToHGlobalAnsi(arguments[i]);

                nativeArguments = Marshal.AllocHGlobal((nativeStrings.Length + 1) * IntPtr.Size);
                for (int i = 0; i < nativeStrings.Length; i++)
                    Marshal.WriteIntPtr(nativeArguments, i * IntPtr.Size, nativeStrings[i]);
                Marshal.WriteIntPtr(nativeArguments, nativeStrings.Length * IntPtr.Size, IntPtr.Zero);

                processId = Fork();
                if (processId == 0)
                {
                    if (createSession)
                        CreateSession();

                    ExecV(nativeStrings[0], nativeArguments);
                    LinuxExit(127);
                    return 0;
                }

                if (processId < 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return processId;
            }
            finally
            {
                if (processId != 0)
                {
                    if (nativeArguments != IntPtr.Zero)
                        Marshal.FreeHGlobal(nativeArguments);

                    foreach (IntPtr nativeString in nativeStrings)
                        if (nativeString != IntPtr.Zero)
                            Marshal.FreeHGlobal(nativeString);
                }
            }
        }

        static string[] CombineArguments(string[] prefix, string[] arguments)
        {
            var result = new string[prefix.Length + arguments.Length];
            Array.Copy(prefix, 0, result, 0, prefix.Length);
            Array.Copy(arguments, 0, result, prefix.Length, arguments.Length);
            return result;
        }

        //----------------------------------------------------------------------------------------------------------

        void DisposeTerminalLauncher()
        {
            terminal_instances.Clear();
        }

        const string
            LinuxLib = "libc.so.6",
            XdgTerminalExec = "/usr/bin/xdg-terminal-exec",
            XTerminalEmulator = "/usr/bin/x-terminal-emulator",
            Wmctrl = "/usr/bin/wmctrl",
            Xdotool = "/usr/bin/xdotool";

        const int WaitNoHang = 1;

        [DllImport(LinuxLib, EntryPoint = "fork", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        static extern int Fork();

        [DllImport(LinuxLib, EntryPoint = "setsid", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        static extern int CreateSession();

        [DllImport(LinuxLib, EntryPoint = "execv", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        static extern int ExecV(IntPtr path, IntPtr arguments);

        [DllImport(LinuxLib, EntryPoint = "_exit", CallingConvention = CallingConvention.Cdecl)]
        static extern void LinuxExit(int status);

        [DllImport(LinuxLib, EntryPoint = "waitpid", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        static extern int WaitPid(int processId, out int status, int options);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll")]
        static extern bool ShowWindowAsync(IntPtr window, int command);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindow(string className, string windowName);

        const uint
            CreateNewConsole = 0x00000010,
            ProcessQueryLimitedInformation = 0x00001000,
            StillActive = 259;

        [StructLayout(LayoutKind.Sequential)]
        struct StartupInfo
        {
            public uint size;
            public IntPtr reserved;
            public IntPtr desktop;
            public IntPtr title;
            public uint x;
            public uint y;
            public uint xSize;
            public uint ySize;
            public uint xCountChars;
            public uint yCountChars;
            public uint fillAttribute;
            public uint flags;
            public ushort showWindow;
            public ushort reservedSize;
            public IntPtr reservedBytes;
            public IntPtr standardInput;
            public IntPtr standardOutput;
            public IntPtr standardError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ProcessInformation
        {
            public IntPtr process;
            public IntPtr thread;
            public uint processId;
            public uint threadId;
        }

        [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateProcessW(
            string applicationName,
            StringBuilder commandLine,
            IntPtr processAttributes,
            IntPtr threadAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
            uint creationFlags,
            IntPtr environment,
            string currentDirectory,
            ref StartupInfo startupInfo,
            out ProcessInformation processInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr handle);
    }
}
