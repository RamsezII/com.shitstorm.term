using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace _TERM_
{
    public partial class TermServer : MonoBehaviour
    {
        [Header("TCP Server")]
        [SerializeField, Range(1, ushort.MaxValue)] ushort port_log = 5051, port_cmd = 5050;

        [Header("External terminal")]
        [SerializeField] KeyCode terminal_key = KeyCode.P;
        [SerializeField] string terminal_executable = "";

        TcpListener cmd_listener, log_listener;
        CancellationTokenSource cancellation;

        //----------------------------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnResetStatics()
        {
            root_commands.Reset();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoad()
        {
            new GameObject(typeof(TermServer).FullName).AddComponent<TermServer>();
        }

        //----------------------------------------------------------------------------------------------------------

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        //----------------------------------------------------------------------------------------------------------

        void Start()
        {
            LoadSettings();
            Application.logMessageReceivedThreaded += OnUnityLog;
            StartServer();
        }

        //----------------------------------------------------------------------------------------------------------

        private void Update()
        {
            TickRoutines();
            TickTerminalLauncher();
        }

        [ContextMenu(nameof(StartServer))]
        public void StartServer()
        {
            if (cmd_listener != null || log_listener != null)
                return;

            try
            {
                cancellation = new CancellationTokenSource();

                // Canal 1 : complétion et exécution des commandes.
                cmd_listener = new TcpListener(IPAddress.Any, port_cmd);

                // Canal 2 : flux indépendant des logs Unity.
                log_listener = new TcpListener(IPAddress.Any, port_log);

                cmd_listener.Start();
                log_listener.Start();

                _ = AcceptCommandConnectionsAsync(cancellation.Token);
                _ = AcceptLogConnectionsAsync(cancellation.Token);

                Debug.Log(
                    $"[TERM] {nameof(port_cmd)}: {port_cmd} | {nameof(port_log)}: {port_log}",
                    this);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                StopServer();
            }
        }

        //----------------------------------------------------------------------------------------------------------

        [ContextMenu(nameof(StopServer))]
        public void StopServer()
        {
            cancellation?.Cancel();
            cmd_listener?.Stop();
            log_listener?.Stop();

            cancellation?.Dispose();
            cancellation = null;
            cmd_listener = null;
            log_listener = null;

            CloseAllConnections();
        }

        //----------------------------------------------------------------------------------------------------------

        void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnUnityLog;
            DisposeTerminalLauncher();
            StopServer();
        }
    }
}
