using System.Collections;
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
        [SerializeField] private int viewportRadius = 9;   // 9 = 19x19 tiles visibles
        [SerializeField] private float tileSize     = 1f;  // 1 unidad Unity = 1 tile

        // Pool de SpriteRenderers por capa
        private readonly Dictionary<string, List<SpriteRenderer>> _pool =
            new Dictionary<string, List<SpriteRenderer>>();
        private readonly string[] _layers = { "1", "2", "3", "4" };
        private readonly int[]    _sortOrders = { 0, 1, 2, 10 };

        private MapGrid   _currentMap;
        private int       _lastCenterX = -1;
        private int       _lastCenterY = -1;

        void Awake() => Instance = this;

        // ── API pública ───────────────────────────────────────────────────────

        public void SetMap(MapGrid map)
        {
            _currentMap  = map;
            _lastCenterX = -1;
            _lastCenterY = -1;
        }

        /// <summary>
        /// Llamar cada frame (o cuando el jugador se mueve) con la posición actual.
        /// Solo redibuja si el tile central cambió.
        /// </summary>
        public void UpdateViewport(int centerX, int centerY)
        {
            if (_currentMap == null) return;
            if (centerX == _lastCenterX && centerY == _lastCenterY) return;

            _lastCenterX = centerX;
            _lastCenterY = centerY;

            StartCoroutine(RenderViewport(centerX, centerY));
        }

        // ── Render ────────────────────────────────────────────────────────────

        private IEnumerator RenderViewport(int centerX, int centerY)
        {
            // Ocultar todos los renderers del pool
            foreach (var layer in _layers)
                foreach (var sr in GetPool(layer))
                    sr.enabled = false;

            int poolIdx = 0;

            for (int dy = -viewportRadius; dy <= viewportRadius; dy++)
            {
                for (int dx = -viewportRadius; dx <= viewportRadius; dx++)
                {
                    int mapX = centerX + dx;
                    int mapY = centerY + dy;

                    var cell = _currentMap.GetCell(mapX, mapY);
                    if (cell == null) continue;

                    // Posición en mundo Unity. Pivot centro → multiplicar por tileSize directo.
                    // Y invertido: mapa Y crece hacia abajo, Unity Y crece hacia arriba.
                    float worldX =  dx * tileSize;
                    float worldY = -dy * tileSize;

                    for (int li = 0; li < _layers.Length; li++)
                    {
                        string layerKey = _layers[li];
                        if (!cell.graphics.TryGetValue(layerKey, out int grhIndex)) continue;
                        if (grhIndex <= 0) continue;

                        var sr = GetPooledRenderer(layerKey, poolIdx);
                        sr.sortingOrder = _sortOrders[li];
                        sr.transform.localPosition = new Vector3(worldX, worldY, 0);
                        sr.enabled = true;

                        // Cargar sprite async
                        int capturedGrh = grhIndex;
                        SpriteRenderer capturedSr = sr;
                        GrhDatabase.Instance.GetSprite(capturedGrh, sprite =>
                        {
                            if (capturedSr != null && sprite != null)
                            {
                                capturedSr.sprite = sprite;
                                // Siempre mostrar en 1x1 tile (32px = 1 unidad)
                                // El sprite tiene 32 PPU → su tamaño natural en Unity es width/32 unidades
                                // Para forzar 1x1: escala = 32/width, 32/height
                                float scaleX = 32f / sprite.rect.width;
                                float scaleY = 32f / sprite.rect.height;
                                capturedSr.transform.localScale = new Vector3(scaleX, scaleY, 1);
                            }
                        });
                    }
                    poolIdx++;
                }

                yield return null; // distribuir el trabajo en varios frames
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

            // Crear nuevo renderer
            var go = new GameObject($"Tile_{layer}_{index}");
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            list.Add(sr);
            return sr;
        }
    }
}
