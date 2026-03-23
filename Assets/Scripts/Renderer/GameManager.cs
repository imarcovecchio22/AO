using System.Collections;
using UnityEngine;
using ArgentumOnline.Game;
using ArgentumOnline.Network;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Orquesta la inicialización: carga assets → conecta al servidor → arranca el render.
    /// Adjuntar al mismo GameObject que NetworkManager, GrhDatabase, MapLoader y MapRenderer.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        void Awake() => Instance = this;

        [Header("Credenciales de prueba")]
        public string accountId   = "69c0a6c336a1fe21e4f99c0c";
        public string characterId = "69c0a79ab2406d190cf6d928";
        public string email       = "test@test.com";
        public byte   typeGame    = 1;
        public byte   idChar      = 0;

        IEnumerator Start()
        {
            // 1. Setear credenciales
            SessionData.Current = new SessionData
            {
                AccountId   = accountId,
                CharacterId = characterId,
                Email       = email,
                TypeGame    = typeGame,
                IdChar      = idChar
            };

            // 2. Inicializar GrhDatabase
            yield return StartCoroutine(GrhDatabase.Instance.Initialize());

            // 3. Conectar al servidor
            GameState.Instance.OnConnected += OnConnected;
            NetworkManager.Instance.Connect();
        }

        private void OnConnected()
        {
            var p = GameState.Instance.LocalPlayer;
            Debug.Log($"[GM] Conectado: {p.Name} en mapa {p.Map}");

            // 3. Cargar el mapa
            StartCoroutine(MapLoader.Instance.LoadMap(p.Map, OnMapLoaded));
        }

        private void OnMapLoaded(MapGrid map)
        {
            if (map == null) { Debug.LogError("[GM] Mapa no cargado"); return; }

            MapRenderer.Instance.SetMap(map);

            // 4. Arrancar el loop de render
            var p = GameState.Instance.LocalPlayer;
            MapRenderer.Instance.UpdateViewport(p.PosX, p.PosY);

            // Suscribirse a cambios de posición del jugador
            p.OnPositionChanged += OnPlayerMoved;

            Debug.Log($"[GM] Mapa {map.MapNumber} listo — renderizando viewport");
        }

        private void OnPlayerMoved()
        {
            var p = GameState.Instance.LocalPlayer;
            MapRenderer.Instance.UpdateViewport(p.PosX, p.PosY);
        }
    }
}
