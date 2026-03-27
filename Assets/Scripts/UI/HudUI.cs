using UnityEngine;
using UnityEngine.UI;
using ArgentumOnline.Game;

namespace ArgentumOnline.UI
{
    /// <summary>
    /// HUD estilo Eternum: barras HP/Mana flotantes (sin panel), ícono circular a la izquierda,
    /// nombre del mapa debajo. Esquina superior izquierda.
    /// </summary>
    public class HudUI : MonoBehaviour
    {
        public static HudUI Instance { get; private set; }

        private Image  _hpFill;
        private Image  _manaFill;
        private Text   _hpText;
        private Text   _manaText;
        private Text   _mapNameText;
        private Text   _coordText;

        private static readonly Color HpColor   = new Color(0.82f, 0.15f, 0.15f, 1f);
        private static readonly Color ManaColor = new Color(0.18f, 0.70f, 0.80f, 1f);

        void Awake() => Instance = this;

        void Start()
        {
            BuildHud();
            GameState.Instance.OnConnected    += Subscribe;
            GameState.Instance.OnMapNameChanged += OnMapName;
        }

        void OnDestroy()
        {
            GameState.Instance.OnConnected     -= Subscribe;
            GameState.Instance.OnMapNameChanged -= OnMapName;
            if (GameState.Instance.LocalPlayer != null)
            {
                GameState.Instance.LocalPlayer.OnStatsChanged    -= Refresh;
                GameState.Instance.LocalPlayer.OnPositionChanged -= RefreshCoords;
            }
        }

        private void Subscribe()
        {
            GameState.Instance.LocalPlayer.OnStatsChanged    += Refresh;
            GameState.Instance.LocalPlayer.OnPositionChanged += RefreshCoords;
            Refresh();
            RefreshCoords();
        }

        private void OnMapName(string name)
        {
            if (_mapNameText != null) _mapNameText.text = name;
        }

        // ── Construcción ──────────────────────────────────────────────────────

        private void BuildHud()
        {
            var canvasGo = new GameObject("HudCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);

            // Contenedor principal — esquina superior izquierda
            var container   = MakeRect("HudContainer", canvasGo.transform);
            var containerRt = container.GetComponent<RectTransform>();
            containerRt.anchorMin        = new Vector2(0, 1);
            containerRt.anchorMax        = new Vector2(0, 1);
            containerRt.pivot            = new Vector2(0, 1);
            containerRt.anchoredPosition = new Vector2(16, -16);
            containerRt.sizeDelta        = new Vector2(240, 114);

            _hpFill   = BuildBar(container.transform, 0, HpColor,   "#", out _hpText);
            _manaFill = BuildBar(container.transform, 1, ManaColor, "~", out _manaText);

            // Nombre del mapa debajo de las barras
            var mapNameGo = MakeRect("MapName", container.transform);
            var mapNameRt = mapNameGo.GetComponent<RectTransform>();
            mapNameRt.anchorMin        = new Vector2(0, 0);
            mapNameRt.anchorMax        = new Vector2(1, 0);
            mapNameRt.pivot            = new Vector2(0, 1);
            mapNameRt.anchoredPosition = new Vector2(0, -4);
            mapNameRt.sizeDelta        = new Vector2(0, 24);
            _mapNameText = mapNameGo.AddComponent<Text>();
            _mapNameText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _mapNameText.fontSize  = 16;
            _mapNameText.color     = new Color(1f, 1f, 1f, 0.80f);
            _mapNameText.alignment = TextAnchor.MiddleLeft;
            var mapSh = mapNameGo.AddComponent<Shadow>();
            mapSh.effectColor    = new Color(0, 0, 0, 0.9f);
            mapSh.effectDistance = new Vector2(1, -1);

            // Coordenadas X/Y debajo del nombre del mapa
            var coordGo = MakeRect("Coords", container.transform);
            var coordRt = coordGo.GetComponent<RectTransform>();
            coordRt.anchorMin        = new Vector2(0, 0);
            coordRt.anchorMax        = new Vector2(1, 0);
            coordRt.pivot            = new Vector2(0, 1);
            coordRt.anchoredPosition = new Vector2(0, -28);
            coordRt.sizeDelta        = new Vector2(0, 20);
            _coordText = coordGo.AddComponent<Text>();
            _coordText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _coordText.fontSize  = 14;
            _coordText.color     = new Color(0.80f, 0.80f, 0.80f, 0.70f);
            _coordText.alignment = TextAnchor.MiddleLeft;
            var coordSh = coordGo.AddComponent<Shadow>();
            coordSh.effectColor    = new Color(0, 0, 0, 0.9f);
            coordSh.effectDistance = new Vector2(1, -1);
        }

        /// <summary>
        /// Una barra: círculo de ícono a la izquierda + barra redondeada con texto.
        /// row = 0 (HP) o 1 (Mana).
        /// </summary>
        private Image BuildBar(Transform parent, int row, Color color,
                               string icon, out Text label)
        {
            const float barH    = 28f;
            const float iconD   = 32f;
            const float spacing = 8f;
            const float gap     = 6f;
            float y = -(row * (barH + gap));

            // Ícono circular
            var iconGo = MakeRect($"Icon{row}", parent);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin        = new Vector2(0, 1);
            iconRt.anchorMax        = new Vector2(0, 1);
            iconRt.pivot            = new Vector2(0, 1);
            iconRt.anchoredPosition = new Vector2(0, y);
            iconRt.sizeDelta        = new Vector2(iconD, iconD);

            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = MakeCircle(32, new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, 0.9f));
            iconImg.type   = Image.Type.Simple;

            var iconLbl = MakeRect($"IconLbl{row}", iconGo.transform);
            var iconLblRt = iconLbl.GetComponent<RectTransform>();
            iconLblRt.anchorMin = Vector2.zero;
            iconLblRt.anchorMax = Vector2.one;
            var iconTxt = iconLbl.AddComponent<Text>();
            iconTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconTxt.text      = icon;
            iconTxt.fontSize  = 16;
            iconTxt.fontStyle = FontStyle.Bold;
            iconTxt.color     = Color.white;
            iconTxt.alignment = TextAnchor.MiddleCenter;

            // Track (fondo de la barra)
            float barX = iconD + spacing;
            float barW = 240f - barX;

            var trackGo = MakeRect($"Track{row}", parent);
            var trackRt = trackGo.GetComponent<RectTransform>();
            trackRt.anchorMin        = new Vector2(0, 1);
            trackRt.anchorMax        = new Vector2(0, 1);
            trackRt.pivot            = new Vector2(0, 1);
            trackRt.anchoredPosition = new Vector2(barX, y + (iconD - barH) / 2f);
            trackRt.sizeDelta        = new Vector2(barW, barH);

            var trackImg = trackGo.AddComponent<Image>();
            trackImg.sprite = MakeRounded((int)barW, (int)barH, barH / 2f,
                                          new Color(0f, 0f, 0f, 0.55f));
            trackImg.type = Image.Type.Simple;

            // Fill con máscara
            var maskGo  = MakeRect($"Mask{row}", trackGo.transform);
            var maskRt  = maskGo.GetComponent<RectTransform>();
            maskRt.anchorMin = Vector2.zero;
            maskRt.anchorMax = Vector2.one;
            maskRt.offsetMin = maskRt.offsetMax = Vector2.zero;
            var mask    = maskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var maskImg = maskGo.AddComponent<Image>();
            maskImg.sprite = MakeRounded((int)barW, (int)barH, barH / 2f, Color.white);

            var fillGo  = MakeRect($"Fill{row}", maskGo.transform);
            var fillRt  = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite     = MakeBarGradient((int)barW, (int)barH, color);
            fillImg.type       = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;

            // Texto HP/Mana sobre la barra
            var lblGo = MakeRect($"Label{row}", trackGo.transform);
            var lblRt = lblGo.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = new Vector2(8, 0);
            lblRt.offsetMax = new Vector2(-8, 0);
            var txt = lblGo.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text      = "— / —";
            var sh = lblGo.AddComponent<Shadow>();
            sh.effectColor    = new Color(0, 0, 0, 0.9f);
            sh.effectDistance = new Vector2(1, -1);

            label = txt;
            return fillImg;
        }

        // ── Actualización ─────────────────────────────────────────────────────

        private void Refresh()
        {
            var p = GameState.Instance.LocalPlayer;
            if (p == null) return;

            if (_hpFill != null)
                _hpFill.fillAmount = p.MaxHP   > 0 ? (float)p.HP   / p.MaxHP   : 0f;
            if (_manaFill != null)
                _manaFill.fillAmount = p.MaxMana > 0 ? (float)p.Mana / p.MaxMana : 0f;
            if (_hpText != null)
                _hpText.text   = $"{p.HP} / {p.MaxHP}";
            if (_manaText != null)
                _manaText.text = $"{p.Mana} / {p.MaxMana}";
        }

        private void RefreshCoords()
        {
            var p = GameState.Instance.LocalPlayer;
            if (p == null || _coordText == null) return;
            _coordText.text = $"Mapa:{p.Map}  X:{p.PosX}  Y:{p.PosY}";
        }

        // ── Helpers de textura ────────────────────────────────────────────────

        private static Sprite MakeRounded(int w, int h, float r, Color color)
        {
            var tex    = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float cx = Mathf.Clamp(px, r, w - r);
                float cy = Mathf.Clamp(py, r, h - r);
                float d  = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                float a  = Mathf.Clamp01(r - d + 0.5f);
                pixels[y * w + x] = new Color(color.r, color.g, color.b, color.a * a);
            }
            tex.SetPixels(pixels); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakeCircle(int size, Color color)
        {
            float r = size / 2f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                float a  = Mathf.Clamp01(r - d + 0.5f);
                pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * a);
            }
            tex.SetPixels(pixels); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakeBarGradient(int w, int h, Color base_)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t      = y / (float)(h - 1);
                float bright = Mathf.Lerp(0.75f, 1.05f, t);
                var c = new Color(Mathf.Clamp01(base_.r * bright),
                                  Mathf.Clamp01(base_.g * bright),
                                  Mathf.Clamp01(base_.b * bright), 1f);
                for (int x = 0; x < w; x++) pixels[y * w + x] = c;
            }
            tex.SetPixels(pixels); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0, 0.5f));
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
