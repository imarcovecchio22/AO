using UnityEngine;
using UnityEngine.UI;
using ArgentumOnline.Game;
using ArgentumOnline.Renderer;

namespace ArgentumOnline.UI
{
    /// <summary>
    /// Minimapa esquina superior derecha.
    /// Base terrain: Texture2D 100×100 (1px = 1 tile) pintada al cargar el mapa.
    /// Dots (jugador + entidades): se sobreimprimen en Update sobre una copia de la base.
    /// </summary>
    public class MinimapUI : MonoBehaviour
    {
        public static MinimapUI Instance { get; private set; }

        private RawImage   _rawImage;
        private Texture2D  _baseTex;   // terreno sin dots
        private Texture2D  _dispTex;   // lo que se muestra (base + dots)
        private Color32[]  _basePixels;

        // Colores de terreno
        private static readonly Color32 ColBlocked  = new Color32(30,  30,  30,  255);
        private static readonly Color32 ColWater    = new Color32(40,  90,  180, 255);
        private static readonly Color32 ColGrass    = new Color32(55,  105, 45,  255);
        private static readonly Color32 ColSafe     = new Color32(65,  120, 55,  255); // zona segura (trigger 6)
        private static readonly Color32 ColRoad     = new Color32(105, 85,  55,  255);
        private static readonly Color32 ColBuilding = new Color32(80,  65,  50,  255);
        private static readonly Color32 ColEmpty    = new Color32(15,  15,  15,  255);

        // Colores de dots
        private static readonly Color32 DotPlayer = new Color32(255, 230, 50,  255);
        private static readonly Color32 DotBorder = new Color32(0,   0,   0,   200);

        private const int MAP_SIZE    = 100;
        private const int DISPLAY_W   = 160;
        private const int DISPLAY_H   = 160;

        void Awake() => Instance = this;

        void Start()
        {
            BuildUI();
            GameState.Instance.OnTeleport += (map, x, y) => ClearDots();
        }

        void Update()
        {
            if (_dispTex == null || _basePixels == null) return;
            RedrawDots();
        }

        // ── API pública ───────────────────────────────────────────────────────

        /// <summary>Llama al cargar el mapa para pintar el terreno base.</summary>
        public void SetMap(MapGrid map)
        {
            if (map == null) return;

            _baseTex   = new Texture2D(MAP_SIZE, MAP_SIZE, TextureFormat.RGBA32, false);
            _dispTex   = new Texture2D(MAP_SIZE, MAP_SIZE, TextureFormat.RGBA32, false);
            _baseTex.filterMode = FilterMode.Point;
            _dispTex.filterMode = FilterMode.Point;

            _basePixels = new Color32[MAP_SIZE * MAP_SIZE];

            for (int y = 1; y <= MAP_SIZE; y++)
            for (int x = 1; x <= MAP_SIZE; x++)
            {
                var cell  = map.GetCell(x, y);
                int px    = x - 1;
                int py    = MAP_SIZE - y; // Y invertido: tile y=1 arriba en minimap

                _basePixels[py * MAP_SIZE + px] = cell == null ? ColEmpty : TileColor(cell);
            }

            _baseTex.SetPixels32(_basePixels);
            _baseTex.Apply();

            if (_rawImage != null) _rawImage.texture = _dispTex;
        }

        // ── Terreno ───────────────────────────────────────────────────────────

        private static Color32 TileColor(MapCell cell)
        {
            if (cell.blocked) return ColBlocked;

            // Zona segura
            if (cell.trigger == 6) return ColSafe;

            int grh = cell.graphics.TryGetValue("1", out int g) ? g : 0;
            if (grh == 0) return ColEmpty;

            // Agua (rangos del protocolo)
            if ((grh >= 1505 && grh <= 1520) ||
                (grh >= 5665 && grh <= 5680) ||
                (grh >= 13547 && grh <= 13562))
                return ColWater;

            // Caminos / suelo interior (rangos típicos de AO)
            if ((grh >= 2650 && grh <= 2700) ||
                (grh >= 4700 && grh <= 4900))
                return ColRoad;

            // Paredes / edificios
            if (grh >= 3500 && grh <= 4500)
                return ColBuilding;

            return ColGrass;
        }

        // ── Dots ──────────────────────────────────────────────────────────────

        private void ClearDots()
        {
            if (_basePixels == null || _dispTex == null) return;
            _dispTex.SetPixels32(_basePixels);
            _dispTex.Apply();
        }

        private void RedrawDots()
        {
            // Copiar base
            var pixels = (Color32[])_basePixels.Clone();

            var p = GameState.Instance.LocalPlayer;

            PaintDot(pixels, p.PosX, p.PosY, DotPlayer, 2);

            _dispTex.SetPixels32(pixels);
            _dispTex.Apply();
        }

        /// <summary>Pinta un punto de radio r centrado en (mapX, mapY).</summary>
        private static void PaintDot(Color32[] pixels, int mapX, int mapY, Color32 color, int r)
        {
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                int px = (mapX - 1) + dx;
                int py = (MAP_SIZE - mapY) + dy;
                if (px < 0 || px >= MAP_SIZE || py < 0 || py >= MAP_SIZE) continue;
                if (dx * dx + dy * dy > r * r) continue;

                // Borde oscuro cuando r > 1
                bool isBorder = r > 1 &&
                    (Mathf.Abs(dx) == r || Mathf.Abs(dy) == r ||
                     dx * dx + dy * dy >= (r - 0.5f) * (r - 0.5f));
                pixels[py * MAP_SIZE + px] = isBorder ? DotBorder : color;
            }
        }

        // ── Construcción UI ───────────────────────────────────────────────────

        private void BuildUI()
        {
            var canvasGo = new GameObject("MinimapCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 7;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);

            // Marco exterior — esquina superior derecha
            var frame   = MakeRect("MinimapFrame", canvasGo.transform);
            var frameRt = frame.GetComponent<RectTransform>();
            frameRt.anchorMin        = new Vector2(1, 1);
            frameRt.anchorMax        = new Vector2(1, 1);
            frameRt.pivot            = new Vector2(1, 1);
            frameRt.anchoredPosition = new Vector2(-16, -16);
            frameRt.sizeDelta        = new Vector2(DISPLAY_W + 8, DISPLAY_H + 8);

            var frameBg = frame.AddComponent<Image>();
            frameBg.color = new Color(0.06f, 0.06f, 0.08f, 0.88f);
            var frameSh = frame.AddComponent<Shadow>();
            frameSh.effectColor    = new Color(0, 0, 0, 0.7f);
            frameSh.effectDistance = new Vector2(3, -3);

            // Imagen del mapa
            var mapGo = MakeRect("MapImage", frame.transform);
            var mapRt = mapGo.GetComponent<RectTransform>();
            mapRt.anchorMin        = new Vector2(0.5f, 0.5f);
            mapRt.anchorMax        = new Vector2(0.5f, 0.5f);
            mapRt.pivot            = new Vector2(0.5f, 0.5f);
            mapRt.anchoredPosition = Vector2.zero;
            mapRt.sizeDelta        = new Vector2(DISPLAY_W, DISPLAY_H);

            _rawImage            = mapGo.AddComponent<RawImage>();
            _rawImage.color      = Color.white;

            // Placeholder oscuro hasta que cargue el mapa
            var placeholder = new Texture2D(1, 1);
            placeholder.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.12f, 1f));
            placeholder.Apply();
            _rawImage.texture = placeholder;

            // Borde interior del marco
            var border   = MakeRect("Border", frame.transform);
            var borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(2, 2);
            borderRt.offsetMax = new Vector2(-2, -2);
            var borderImg = border.AddComponent<Image>();
            borderImg.color    = new Color(0.4f, 0.35f, 0.20f, 0.35f);
            borderImg.raycastTarget = false;
        }

        private static GameObject MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }
    }
}
