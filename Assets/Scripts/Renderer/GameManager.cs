using System.Collections;
using UnityEngine;
using ArgentumOnline.Game;
using ArgentumOnline.Network;
using ArgentumOnline.UI;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Orquesta la inicialización: login → carga assets → conecta → render.
    /// Adjuntar al mismo GameObject que NetworkManager, GrhDatabase, MapLoader, MapRenderer,
    /// ApiClient, LoginUI, HudUI, ConsoleUI, PlayerController y JoystickUI.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        void Awake() => Instance = this;

        IEnumerator Start()
        {
            // 1. Cargar assets en paralelo con el login
            yield return StartCoroutine(GrhDatabase.Instance.Initialize());
            yield return StartCoroutine(EntityDatabase.Instance.Initialize());

            // 2. Mostrar login y esperar selección de personaje
            LoginUI.Instance.OnCharacterSelected += OnCharacterSelected;
        }

        private void OnCharacterSelected(string accountId, string characterId, string email)
        {
            SessionData.Current = new SessionData
            {
                AccountId   = accountId,
                CharacterId = characterId,
                Email       = email,
                TypeGame    = 1,
                IdChar      = 0
            };

            GameState.Instance.OnConnected += OnConnected;
            NetworkManager.Instance.Connect();
        }

        private void OnConnected()
        {
            var p = GameState.Instance.LocalPlayer;
            Debug.Log($"[GM] Conectado: {p.Name} en mapa {p.Map}");
            StartCoroutine(MapLoader.Instance.LoadMap(p.Map, OnMapLoaded));
        }

        private void OnMapLoaded(MapGrid map)
        {
            if (map == null) { Debug.LogError("[GM] Mapa no cargado"); return; }

            MapRenderer.Instance.SetMap(map);

            var p = GameState.Instance.LocalPlayer;
            MapRenderer.Instance.UpdateViewport(p.PosX, p.PosY);
            p.OnPositionChanged += OnPlayerMoved;

            ArgentumOnline.UI.MinimapUI.Instance?.SetMap(map);

            EntityRenderer.Instance.Initialize();
            GameState.Instance.OnTeleport += OnTeleport;

            Debug.Log($"[GM] Mapa {map.MapNumber} listo — renderizando viewport");
        }

        private void OnPlayerMoved()
        {
            var p = GameState.Instance.LocalPlayer;
            MapRenderer.Instance.UpdateViewport(p.PosX, p.PosY);
        }

        private void OnTeleport(ushort map, byte posX, byte posY)
        {
            Debug.Log($"[GM] Cargando mapa {map} tras teleport");
            StartCoroutine(MapLoader.Instance.LoadMap(map, loadedMap =>
            {
                if (loadedMap == null) { Debug.LogError($"[GM] Mapa {map} no encontrado"); return; }
                MapRenderer.Instance.SetMap(loadedMap);
                MapRenderer.Instance.UpdateViewport(posX, posY);
                ArgentumOnline.UI.MinimapUI.Instance?.SetMap(loadedMap);
            }));
        }
    }
}
