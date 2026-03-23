using System;
using System.Collections;
using UnityEngine;
using NativeWebSocket;

namespace ArgentumOnline.Network
{
    /// <summary>
    /// Singleton que gestiona el ciclo de vida del WebSocket.
    /// Adjuntarlo a un GameObject vacío en la escena de juego.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Configuración")]
        [SerializeField] private string serverUrl = "ws://127.0.0.1:7666";
        [SerializeField] private float  reconnectDelay = 5f;
        [SerializeField] private float  pingInterval   = 10f;

        private WebSocket         _ws;
        private PacketDispatcher  _dispatcher;
        private Coroutine         _pingCoroutine;
        private bool              _forceClose;  // true cuando el servidor manda CloseForce

        // Tiempo en que se envió el último ping (para calcular latencia)
        private float _pingStart;
        public  int   PingMs { get; private set; }

        // ── Lifecycle ────────────────────────────────────────────────────────

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _dispatcher = new PacketDispatcher();
        }

        void Update()
        {
            // NativeWebSocket requiere esto en el hilo principal para despachar mensajes
#if !UNITY_WEBGL || UNITY_EDITOR
            _ws?.DispatchMessageQueue();
#endif
        }

        async void OnApplicationQuit()
        {
            _forceClose = true;
            if (_ws != null) await _ws.Close();
        }

        // ── Conexión ─────────────────────────────────────────────────────────

        public async void Connect()
        {
            _forceClose = false;
            _ws = new WebSocket(serverUrl);

            _ws.OnOpen    += HandleOpen;
            _ws.OnClose   += HandleClose;
            _ws.OnError   += HandleError;
            _ws.OnMessage += HandleMessage;

            await _ws.Connect();
        }

        private void HandleOpen()
        {
            Debug.Log("[Net] Conectado al servidor");

            if (SessionData.Current == null)
            {
                Debug.LogError("[Net] SessionData.Current es null. Configurar antes de conectar.");
                return;
            }

            var s = SessionData.Current;
            Send(PacketSerializer.Instance.ConnectCharacter(
                s.AccountId, s.CharacterId, s.Email, s.TypeGame, s.IdChar));

            _pingCoroutine = StartCoroutine(PingLoop());
        }

        private void HandleClose(WebSocketCloseCode code)
        {
            Debug.Log($"[Net] Desconectado: {code}");

            if (_pingCoroutine != null) StopCoroutine(_pingCoroutine);

            if (!_forceClose)
            {
                // TODO: UIManager.Instance.ShowReconnectModal()
                StartCoroutine(ReconnectAfter(reconnectDelay));
            }
        }

        private void HandleError(string error)
        {
            Debug.LogError($"[Net] WebSocket error: {error}");
        }

        private void HandleMessage(byte[] data)
        {
            Debug.Log($"[Net] Paquete recibido: ID={data[0]} ({data.Length} bytes)");
            _dispatcher.Dispatch(data);
        }

        private IEnumerator ReconnectAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Debug.Log("[Net] Reconectando...");
            Connect();
        }

        // ── Envío ────────────────────────────────────────────────────────────

        public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;

        public async void Send(byte[] data)
        {
            if (_ws != null && _ws.State == WebSocketState.Open)
                await _ws.Send(data);
            else
                Debug.LogWarning("[Net] Send: WebSocket no está abierto");
        }

        // ── Ping ─────────────────────────────────────────────────────────────

        private IEnumerator PingLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(pingInterval);
                _pingStart = Time.realtimeSinceStartup;
                Send(PacketSerializer.Instance.Ping());
            }
        }

        public void OnPongReceived()
        {
            PingMs = Mathf.RoundToInt((Time.realtimeSinceStartup - _pingStart) * 1000f);
            // TODO: UIManager.Instance.SetPing(PingMs)
        }

        // ── CloseForce ───────────────────────────────────────────────────────

        /// <summary>
        /// El servidor forzó la desconexión intencionalmente (no reconectar).
        /// </summary>
        public void OnCloseForce()
        {
            Debug.Log("[Net] CloseForce recibido del servidor");
            _forceClose = true;
            // TODO: UIManager.Instance.ShowErrorModal("Desconectado por el servidor")
        }
    }
}
