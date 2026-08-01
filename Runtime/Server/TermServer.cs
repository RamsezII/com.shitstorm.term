using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace _TERM_
{
    public partial class TermServer : MonoBehaviour
    {
        [Header("External terminal")]
        [SerializeField] KeyCode terminal_key = KeyCode.P;
        [SerializeField, Range(0, ushort.MaxValue)] ushort port_cmd_override, port_log_override;

        TcpListener cmd_listener, log_listener;
        [SerializeField, Range(0, ushort.MaxValue)] int port_cmd, port_log;
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
            LoadRSettings();
            LoadHSettings();
            Application.logMessageReceivedThreaded += OnUnityLog;
            StartServer();
        }

        //----------------------------------------------------------------------------------------------------------

        private void OnApplicationFocus(bool focus)
        {
            if (focus)
                LoadHSettings();
            else
                SaveHSettings();
        }

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
                cmd_listener = new TcpListener(IPAddress.Any, port_cmd_override);

                // Canal 2 : flux indépendant des logs Unity.
                log_listener = new TcpListener(IPAddress.Any, port_log_override);

                cmd_listener.Start();
                log_listener.Start();

                port_cmd = ((IPEndPoint)cmd_listener.LocalEndpoint).Port;
                port_log = ((IPEndPoint)log_listener.LocalEndpoint).Port;

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
            port_cmd = 0;
            port_log = 0;

            CloseAllConnections();
        }

        //----------------------------------------------------------------------------------------------------------

        void OnDestroy()
        {
#if UNITY_EDITOR
            SaveRSettings();
#endif
            SaveHSettings();
            Application.logMessageReceivedThreaded -= OnUnityLog;
            DisposeTerminalLauncher();
            StopServer();
        }
    }
}
