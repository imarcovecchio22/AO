using UnityEngine;
using UnityEngine.UI;

namespace ArgentumOnline.UI
{
    /// <summary>
    /// Barra superior de botones circulares semitransparentes (estilo Eternum).
    /// Posición: top-center/right.
    /// Botones: Chat | Inventario | Hechizos
    /// </summary>
    public class TopBarUI : MonoBehaviour
    {
        public static TopBarUI Instance { get; private set; }

        void Awake() => Instance = this;

        void Start() => BuildBar();

        private void BuildBar()
        {
            var canvasGo = new GameObject("TopBarCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);

            // Contenedor de botones — top center
            var bar   = MakeRect("Bar", canvasGo.transform);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin        = new Vector2(0.5f, 1);
            barRt.anchorMax        = new Vector2(0.5f, 1);
            barRt.pivot            = new Vector2(0.5f, 1);
            barRt.anchoredPosition = new Vector2(0, -16);
            barRt.sizeDelta        = new Vector2(300, 64);

            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing            = 16;
            layout.childAlignment     = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth  = false;
            layout.childControlHeight = false;

            // Botones
            AddButton(bar.transform, "✉", "Chat",  new Color(0.20f, 0.20f, 0.25f, 0.82f), OnChat);
            AddButton(bar.transform, "⚔", "Inv",   new Color(0.20f, 0.20f, 0.25f, 0.82f), OnInventory);
            AddButton(bar.transform, "✦", "Spell", new Color(0.20f, 0.20f, 0.25f, 0.82f), OnSpells);
        }

        private static void AddButton(Transform parent, string icon, string name,
                                       Color color, UnityEngine.Events.UnityAction action)
        {
            const float D = 58f;

            var go = MakeRect(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(D, D);

            var img = go.AddComponent<Image>();
            img.sprite = MakeCircle(64, color);
            img.type   = Image.Type.Simple;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor    = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(2, -2);

            var btn    = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.42f, 0.9f);
            colors.pressedColor     = new Color(0.10f, 0.10f, 0.14f, 0.9f);
            btn.colors = colors;
            btn.onClick.AddListener(action);

            // Ícono
            var lblGo = MakeRect("Icon", go.transform);
            var lblRt = lblGo.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            var txt = lblGo.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text      = icon;
            txt.fontSize  = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = new Color(1f, 1f, 1f, 0.92f);
            txt.alignment = TextAnchor.MiddleCenter;
            var sh = lblGo.AddComponent<Shadow>();
            sh.effectColor    = new Color(0, 0, 0, 0.8f);
            sh.effectDistance = new Vector2(1, -1);
        }

        // ── Acciones ──────────────────────────────────────────────────────────

        private void OnChat()
        {
            // TODO: toggle ConsoleUI input
        }

        private void OnInventory()
        {
            InventoryUI.Instance?.ToggleFromTopBar();
        }

        private void OnSpells()
        {
            SpellsUI.Instance?.Toggle();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Sprite MakeCircle(int size, Color color)
        {
            float r   = size / 2f;
            var   tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
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

        private static GameObject MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }
    }
}
