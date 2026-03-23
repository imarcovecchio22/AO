using System.Collections.Generic;
using UnityEngine;
using ArgentumOnline.Game;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Renderiza el mapa visible alrededor del jugador.
    /// Viewport: 19x19 tiles centrado en el jugador (igual que el cliente web).
    /// Usa pool de SpriteRenderers para evitar allocations.
    ///
    /// Capas (sortingOrder):
    ///   graphics["1"] → 0  (piso base)
    ///   graphics["2"] → 1  (piso decoración)
    ///   graphics["3"] → 2  (objetos, paredes bajas)
    ///   graphics["4"] → 10 (techos — se ocultan cerca del jugador)
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        public static MapRenderer Instance { get; private set; }

        [Header("Configuración")]
        [SerializeField] private int   viewportRadius  = 9;    // 9 = 19x19 tiles visibles
        [SerializeField] private float tileSize        = 1f;   // 1 unidad Unity = 1 tile

        [Header("Offset capas 3/4 (paredes y techos)")]
        [SerializeField] private float overlayOffsetX  = 0.4f;
        [SerializeField] private float overlayOffsetY  = -1.3f;

        public int ViewportRadius => viewportRadius;

        // Pool de SpriteRenderers por capa
        private readonly Dictionary<string, List<SpriteRenderer>> _pool =
            new Dictionary<string, List<SpriteRenderer>>();
        private readonly string[] _layers    = { "1", "2", "3", "4" };
        private readonly int[]    _sortOrders = { 0, 1, 2, 10 };

        private MapGrid _currentMap;
        private int     _lastCenterX = -1;
        private int     _lastCenterY = -1;

        void Awake() => Instance = this;

        // ── API pública ───────────────────────────────────────────────────────

        public void SetMap(MapGrid map)
        {
            _currentMap  = map;
            _lastCenterX = -1;
            _lastCenterY = -1;
        }

        /// <summary>
        /// Llamar cuando el jugador se mueve. Solo redibuja si el tile central cambió.
        /// </summary>
        public void UpdateViewport(int centerX, int centerY)
        {
            if (_currentMap == null) return;
            if (centerX == _lastCenterX && centerY == _lastCenterY) return;

            _lastCenterX = centerX;
            _lastCenterY = centerY;

            RenderViewport(centerX, centerY);
        }

        // ── Render ────────────────────────────────────────────────────────────

        private void RenderViewport(int centerX, int centerY)
        {
            foreach (var layer in _layers)
                foreach (var sr in GetPool(layer))
                    sr.enabled = false;

            int poolIdx      = 0;
            int renderRadius = viewportRadius + 1;

            for (int dy = -renderRadius; dy <= renderRadius; dy++)
            for (int dx = -renderRadius; dx <= renderRadius; dx++)
            {
                int mapX = centerX + dx;
                int mapY = centerY + dy;

                var cell = _currentMap.GetCell(mapX, mapY);
                if (cell == null) { poolIdx++; continue; }

                for (int li = 0; li < _layers.Length; li++)
                {
                    string layerKey = _layers[li];
                    if (!cell.graphics.TryGetValue(layerKey, out int grhIndex) || grhIndex <= 0)
                        continue;

                    var sr = GetPooledRenderer(layerKey, poolIdx);
                    sr.sortingOrder = _sortOrders[li];
                    sr.enabled      = true;

                    int            capturedGrh = grhIndex;
                    SpriteRenderer capturedSr  = sr;
                    float          capturedDx  = dx * tileSize;
                    float          capturedDy  = dy * tileSize;
                    bool           isOverlay   = li >= 2;   // layer "3" o "4"
                    GrhDatabase.Instance.GetSprite(capturedGrh, sprite =>
                    {
                        if (capturedSr == null) return;
                        if (sprite == null) { capturedSr.enabled = false; return; }
                        capturedSr.sprite               = sprite;
                        capturedSr.transform.localScale = Vector3.one;

                        float w = sprite.rect.width  / 32f;
                        float h = sprite.rect.height / 32f;
                        float posX, posY;
                        if (isOverlay)
                        {
                            // El cliente web dibuja capas 3/4 desplazadas +2 tiles al SE
                            // respecto a la capa 1 en la misma posición de mapa:
                            //   X = ScreenX*32 - W/2 + 48  → centro siempre en dx+2
                            //   Y = ScreenY*32 - H  + 64   → centro en -dy - 2.5 + h/2
                            posX = capturedDx + overlayOffsetX;
                            posY = -capturedDy + overlayOffsetY + h * 0.5f;
                        }
                        else
                        {
                            // Capas 1/2: top-left del sprite en top-left del tile
                            posX = capturedDx - 0.5f + w * 0.5f;
                            posY = -capturedDy + 0.5f - h * 0.5f;
                        }
                        capturedSr.transform.localPosition = new Vector3(posX, posY, 0);
                    });
                }
                poolIdx++;
            }
        }

        // ── Pool de SpriteRenderers ───────────────────────────────────────────

        private List<SpriteRenderer> GetPool(string layer)
        {
            if (!_pool.TryGetValue(layer, out var list))
            {
                list = new List<SpriteRenderer>();
                _pool[layer] = list;
            }
            return list;
        }

        private SpriteRenderer GetPooledRenderer(string layer, int index)
        {
            var list = GetPool(layer);

            if (index < list.Count)
                return list[index];

            var go = new GameObject($"Tile_{layer}_{index}");
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            list.Add(sr);
            return sr;
        }
    }
}
