using UnityEngine;
using UnityEngine.UI;
using ArgentumOnline.Game;

namespace ArgentumOnline.UI
{
    /// <summary>
    /// Panel lateral derecho con los hechizos del jugador.
    /// Tap en un hechizo lo selecciona; el siguiente click en el mundo lo lanza.
    /// </summary>
    public class SpellsUI : MonoBehaviour
    {
        public static SpellsUI Instance { get; private set; }

        /// <summary>-1 = ninguno seleccionado, 0-27 = slot del hechizo activo.</summary>
        public int SelectedSpellSlot { get; private set; } = -1;

        private GameObject _panel;
        private Transform  _listContent;
        private bool       _isOpen;

        // Colores
        private static readonly Color BgPanel    = new Color(0.05f, 0.05f, 0.05f, 0.55f);
        private static readonly Color BgRow      = new Color(0.10f, 0.10f, 0.10f, 0.60f);
        private static readonly Color BgSelected = new Color(0.20f, 0.20f, 0.20f, 0.85f);
        private static readonly Color BgInactive = new Color(0.08f, 0.08f, 0.08f, 0.45f);
        private static readonly Color ColGold    = new Color(0.92f, 0.80f, 0.45f, 1f);
        private static readonly Color ColMana    = new Color(0.50f, 0.75f, 1.00f, 1f);
        private static readonly Color ColWhite   = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color ColGray    = new Color(0.55f, 0.55f, 0.55f, 1f);

        private const float PanelW  = 230f;
        private const float RowH    = 52f;
        private const float RowGap  = 4f;
        private const float PadX    = 10f;
        private const float HeaderH = 48f;
        private const float PadBot  = 10f;
        private const float MaxPanelH = 600f;

        // Referencias a los elementos de cada fila para refrescar sin reconstruir
        private Image[] _rowBgs;
        private Text[]  _rowNames;
        private Text[]  _rowManas;

        void Awake() => Instance = this;

        void Start()
        {
            BuildUI();
            GameState.Instance.OnConnected += OnConnected;
        }

        void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.OnConnected -= OnConnected;
        }

        private void OnConnected()
        {
            GameState.Instance.LocalPlayer.OnSpellsChanged += Refresh;
        }

        // ── Construcción ──────────────────────────────────────────────────────

        private void BuildUI()
        {
            var canvasGo = new GameObject("SpellsCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 18;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);

            // Panel principal — derecha, centrado verticalmente
            _panel = MakeRect("SpellsPanel", canvasGo.transform);
            var panelRt = _panel.GetComponent<RectTransform>();
            panelRt.anchorMin        = new Vector2(1f, 0.5f);
            panelRt.anchorMax        = new Vector2(1f, 0.5f);
            panelRt.pivot            = new Vector2(1f, 0.5f);
            panelRt.anchoredPosition = new Vector2(-8f, 0f);
            panelRt.sizeDelta        = new Vector2(PanelW, MaxPanelH);

            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = BgPanel;
            panelImg.raycastTarget = true;

            AddOutline(_panel, new Color(0.30f, 0.30f, 0.30f, 0.5f));

            // Header "HECHIZOS"
            var header   = MakeRect("Header", _panel.transform);
            var headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin        = new Vector2(0, 1);
            headerRt.anchorMax        = new Vector2(1, 1);
            headerRt.pivot            = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta        = new Vector2(0, HeaderH);

            var headerImg = header.AddComponent<Image>();
            headerImg.color = new Color(0.10f, 0.10f, 0.10f, 0.75f);

            var title = MakeRect("Title", header.transform);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            var titleTxt = title.AddComponent<Text>();
            titleTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.text      = "HECHIZOS";
            titleTxt.fontSize  = 18;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color     = ColGold;
            titleTxt.alignment = TextAnchor.MiddleCenter;

            // Botón cerrar
            var closeGo = MakeRect("Close", header.transform);
            var closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin        = new Vector2(1, 0.5f);
            closeRt.anchorMax        = new Vector2(1, 0.5f);
            closeRt.pivot            = new Vector2(1, 0.5f);
            closeRt.anchoredPosition = new Vector2(-6f, 0f);
            closeRt.sizeDelta        = new Vector2(32, 32);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.6f, 0.1f, 0.1f, 0.8f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(Close);
            var closeTxt = MakeRect("X", closeGo.transform);
            var closeTxtRt = closeTxt.GetComponent<RectTransform>();
            closeTxtRt.anchorMin = Vector2.zero;
            closeTxtRt.anchorMax = Vector2.one;
            var ct = closeTxt.AddComponent<Text>();
            ct.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ct.text = "✕"; ct.fontSize = 16; ct.fontStyle = FontStyle.Bold;
            ct.color = ColWhite; ct.alignment = TextAnchor.MiddleCenter;

            // ScrollRect para la lista
            var scrollGo = MakeRect("Scroll", _panel.transform);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin        = new Vector2(0, 0);
            scrollRt.anchorMax        = new Vector2(1, 1);
            scrollRt.offsetMin        = new Vector2(PadX, PadBot);
            scrollRt.offsetMax        = new Vector2(-PadX, -HeaderH);

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical   = true;
            scrollRect.scrollSensitivity = 30f;

            var mask = scrollGo.AddComponent<RectMask2D>();

            var contentGo = MakeRect("Content", scrollGo.transform);
            _listContent  = contentGo.transform;
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0.5f, 1);
            contentRt.offsetMin = new Vector2(0, 0);
            contentRt.offsetMax = new Vector2(0, 0);

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing            = RowGap;
            vlg.childAlignment     = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;
            vlg.padding            = new RectOffset(0, 0, 4, 4);

            contentGo.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content  = contentRt;
            scrollRect.viewport = scrollRt;

            // Pre-poblar filas (máximo 28 hechizos)
            _rowBgs   = new Image[LocalPlayer.SpellsSize];
            _rowNames = new Text[LocalPlayer.SpellsSize];
            _rowManas = new Text[LocalPlayer.SpellsSize];

            for (int i = 0; i < LocalPlayer.SpellsSize; i++)
            {
                int capturedI = i;
                var row   = MakeRect($"Row{i}", _listContent);
                var rowLe = row.AddComponent<LayoutElement>();
                rowLe.preferredHeight = RowH;
                rowLe.flexibleWidth   = 1;

                var rowImg = row.AddComponent<Image>();
                rowImg.color = BgRow;
                _rowBgs[i]   = rowImg;

                var rowBtn = row.AddComponent<Button>();
                rowBtn.targetGraphic = rowImg;
                rowBtn.onClick.AddListener(() => OnSpellClick(capturedI));

                // Nombre del hechizo
                var nameGo = MakeRect("Name", row.transform);
                var nameRt = nameGo.GetComponent<RectTransform>();
                nameRt.anchorMin        = new Vector2(0, 0);
                nameRt.anchorMax        = new Vector2(1, 1);
                nameRt.offsetMin        = new Vector2(8, 0);
                nameRt.offsetMax        = new Vector2(-60, 0);
                var nameTxt = nameGo.AddComponent<Text>();
                nameTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameTxt.fontSize  = 15;
                nameTxt.fontStyle = FontStyle.Normal;
                nameTxt.color     = ColWhite;
                nameTxt.alignment = TextAnchor.MiddleLeft;
                _rowNames[i] = nameTxt;

                // Coste de maná (derecha)
                var manaGo = MakeRect("Mana", row.transform);
                var manaRt = manaGo.GetComponent<RectTransform>();
                manaRt.anchorMin        = new Vector2(1, 0);
                manaRt.anchorMax        = new Vector2(1, 1);
                manaRt.pivot            = new Vector2(1, 0.5f);
                manaRt.anchoredPosition = new Vector2(-6, 0);
                manaRt.sizeDelta        = new Vector2(54, 0);
                var manaTxt = manaGo.AddComponent<Text>();
                manaTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                manaTxt.fontSize  = 13;
                manaTxt.color     = ColMana;
                manaTxt.alignment = TextAnchor.MiddleRight;
                _rowManas[i] = manaTxt;

                row.SetActive(false); // oculto hasta que haya un hechizo
            }

            _panel.SetActive(false);
        }

        // ── Lógica ────────────────────────────────────────────────────────────

        public void Toggle()
        {
            if (_isOpen) Close(); else Open();
        }

        public void Open()
        {
            _isOpen = true;
            Refresh();
            _panel.SetActive(true);
        }

        public void Close()
        {
            _isOpen = false;
            _panel.SetActive(false);
        }

        private void Refresh()
        {
            if (!_isOpen) return;
            var spells = GameState.Instance.LocalPlayer.Spells;
            for (int i = 0; i < LocalPlayer.SpellsSize; i++)
            {
                var slot = spells[i];
                if (slot.IsEmpty)
                {
                    _rowBgs[i].gameObject.SetActive(false);
                    continue;
                }

                var rowGo = _rowBgs[i].gameObject;
                rowGo.SetActive(true);
                _rowNames[i].text = slot.Name;
                _rowManas[i].text = $"{slot.ManaRequired}✦";
                UpdateRowColor(i);
            }
        }

        private void OnSpellClick(int slotIndex)
        {
            var spell = GameState.Instance.LocalPlayer.Spells[slotIndex];
            if (spell.IsEmpty) return;

            if (SelectedSpellSlot == slotIndex)
            {
                // Segundo tap → desseleccionar
                SelectedSpellSlot = -1;
            }
            else
            {
                SelectedSpellSlot = slotIndex;
            }

            // Actualizar colores de todas las filas
            for (int i = 0; i < LocalPlayer.SpellsSize; i++)
                UpdateRowColor(i);
        }

        private void UpdateRowColor(int i)
        {
            var spell = GameState.Instance.LocalPlayer.Spells[i];
            if (spell.IsEmpty)
            {
                _rowBgs[i].color = BgInactive;
                return;
            }
            _rowBgs[i].color = (i == SelectedSpellSlot) ? BgSelected : BgRow;
        }

        /// <summary>Llamado por PlayerController tras lanzar el hechizo.</summary>
        public void ClearSelection()
        {
            SelectedSpellSlot = -1;
            for (int i = 0; i < LocalPlayer.SpellsSize; i++)
                UpdateRowColor(i);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static GameObject MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void AddOutline(GameObject go, Color color)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor    = color;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }
}
