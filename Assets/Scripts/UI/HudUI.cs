using UnityEngine;
using UnityEngine.UI;
using ArgentumOnline.Game;

namespace ArgentumOnline.UI
{
    /// <summary>
    /// HUD compacto: barras de HP y Mana en la esquina superior izquierda.
    /// Bordes redondeados y sombra generados en runtime via Texture2D.
    /// </summary>
    public class HudUI : MonoBehaviour
    {
        private Image _hpFill;
        private Image _manaFill;
        private Text  _hpText;
        private Text  _manaText;

        // Colores
        private static readonly Color HpColor   = new Color(0.22f, 0.82f, 0.35f, 1f);   // verde
        private static readonly Color ManaColor = new Color(0.22f, 0.50f, 0.98f, 1f);   // azul

        void Start()
        {
            BuildHud();
            GameState.Instance.OnConnected += Subscribe;
            if (GameState.Instance.LocalPlayer != null) Subscribe();
        }

        void OnDestroy()
        {
            GameState.Instance.OnConnected -= Subscribe;
            if (GameState.Instance.LocalPlayer != null)
                GameState.Instance.LocalPlayer.OnStatsChanged -= Refresh;
        }

        private void Subscribe()
        {
            GameState.Instance.LocalPlayer.OnStatsChanged += Refresh;
            Refresh();
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

            // Panel — esquina superior izquierda
            var panel     = CreateRect("StatsPanel", canvasGo.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin        = new Vector2(0, 1);
            panelRect.anchorMax        = new Vector2(0, 1);
            panelRect.pivot            = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(18, -18);
            panelRect.sizeDelta        = new Vector2(210, 70);

            // Fondo redondeado oscuro
            var bg = panel.AddComponent<Image>();
            bg.sprite = MakeRoundedRect(210, 70, 12, new Color(0.06f, 0.06f, 0.10f, 0.82f));
            bg.type   = Image.Type.Simple;

            // Sombra del panel
            var shadow = panel.AddComponent<Shadow>();
            shadow.effectColor    = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(3, -3);

            _hpFill   = BuildBar(panel.transform, 0, HpColor,   out _hpText);
            _manaFill = BuildBar(panel.transform, 1, ManaColor, out _manaText);
        }

        private Image BuildBar(Transform parent, int row, Color fillColor, out Text label)
        {
            const float rowH    = 20f;
            const float padX    = 12f;
            const float padY    = 12f;
            const float spacing = 6f;
            float offsetY = -(padY + row * (rowH + spacing));

            // Track redondeado
            var track     = CreateRect($"Track{row}", parent);
            var trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin        = new Vector2(0, 1);
            trackRect.anchorMax        = new Vector2(1, 1);
            trackRect.pivot            = new Vector2(0, 1);
            trackRect.anchoredPosition = new Vector2(padX, offsetY);
            trackRect.sizeDelta        = new Vector2(-(padX * 2), rowH);
            var trackImg = track.AddComponent<Image>();
            trackImg.sprite = MakeRoundedRect(186, 20, 10, new Color(0.12f, 0.12f, 0.16f, 1f));
            trackImg.type   = Image.Type.Simple;

            // Fill redondeado con máscara
            var maskGo = CreateRect("Mask", track.transform);
            var maskRect = maskGo.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;
            var mask = maskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var maskImg = maskGo.AddComponent<Image>();
            maskImg.sprite = MakeRoundedRect(186, 20, 10, Color.white);

            var fill     = CreateRect("Fill", maskGo.transform);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.sprite     = MakeBarGradient(186, 20, fillColor);
            fillImg.type       = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;

            // Texto sobre la barra
            var textGo   = CreateRect("Label", track.transform);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 0);
            textRect.offsetMax = new Vector2(-4, 0);
            var txt = textGo.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 11;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = new Color(1f, 1f, 1f, 0.95f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text      = "—";

            // Sombra en el texto
            var txtShadow = textGo.AddComponent<Shadow>();
            txtShadow.effectColor    = new Color(0, 0, 0, 0.8f);
            txtShadow.effectDistance = new Vector2(1, -1);

            label = txt;
            return fillImg;
        }

        // ── Helpers de textura ────────────────────────────────────────────────

        /// <summary>Rectángulo con esquinas redondeadas y anti-aliasing suave.</summary>
        private static Sprite MakeRoundedRect(int w, int h, float radius, Color color)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float alpha = RoundedAlpha(x, y, w, h, radius);
                    pixels[y * w + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        /// <summary>Gradiente horizontal: color sólido abajo, brillo sutil arriba.</summary>
        private static Sprite MakeBarGradient(int w, int h, Color baseColor)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                // Brillo: parte superior 30% más clara
                float bright = Mathf.Lerp(0.78f, 1.0f, t);
                var c = new Color(
                    Mathf.Clamp01(baseColor.r * bright),
                    Mathf.Clamp01(baseColor.g * bright),
                    Mathf.Clamp01(baseColor.b * bright),
                    baseColor.a);
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = c;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0, 0.5f));
        }

        /// <summary>Devuelve 0-1 con suavizado en las esquinas redondeadas.</summary>
        private static float RoundedAlpha(int x, int y, int w, int h, float r)
        {
            float px = x + 0.5f;
            float py = y + 0.5f;

            // Centro del corner más cercano
            float cx = px < r ? r : (px > w - r ? w - r : px);
            float cy = py < r ? r : (py > h - r ? h - r : py);

            float dist = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));

            // Anti-aliasing: suavizar 1px alrededor del borde
            return Mathf.Clamp01(r - dist + 0.5f);
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        // ── Actualización ─────────────────────────────────────────────────────

        private void Refresh()
        {
            var p = GameState.Instance.LocalPlayer;
            if (p == null) return;

            float hpPct   = p.MaxHP   > 0 ? (float)p.HP   / p.MaxHP   : 0f;
            float manaPct = p.MaxMana > 0 ? (float)p.Mana / p.MaxMana : 0f;

            if (_hpFill   != null) _hpFill.fillAmount  = hpPct;
            if (_manaFill != null) _manaFill.fillAmount = manaPct;
            if (_hpText   != null) _hpText.text         = $"{p.HP} / {p.MaxHP}";
            if (_manaText != null) _manaText.text        = $"{p.Mana} / {p.MaxMana}";
        }
    }
}
