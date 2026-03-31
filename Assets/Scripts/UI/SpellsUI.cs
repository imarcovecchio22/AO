using UnityEngine;
using UnityEngine.UI;
using ArgentumOnline.Game;

namespace ArgentumOnline.UI
{
    /// <summary>
    /// Panel lateral derecho con los hechizos del jugador.
    /// Tap en un hechizo lo selecciona; el siguiente click en el mundo lo lanza.
    /// Altura dinámica según cantidad de hechizos; scrollbar visible si hay muchos.
    /// </summary>
    public class SpellsUI : MonoBehaviour
    {
        public static SpellsUI Instance { get; private set; }

        /// <summary>-1 = ninguno seleccionado, 0-27 = slot del hechizo activo.</summary>
        public int SelectedSpellSlot { get; private set; } = -1;

        private RectTransform _panelRt;
        private GameObject    _panel;
        private Transform     _listContent;
        private bool          _isOpen;

        // Colores
        private static readonly Color BgPanel    = new Color(0.05f, 0.05f, 0.05f, 0.55f);
        private static readonly Color BgRow      = new Color(0.10f, 0.10f, 0.10f, 0.60f);
        private static readonly Color BgSelected = new Color(0.20f, 0.35f, 0.20f, 0.90f);
        private static readonly Color ColGold    = new Color(0.92f, 0.80f, 0.45f, 1f);
        private static readonly Color ColMana    = new Color(0.50f, 0.75f, 1.00f, 1f);
        private static readonly Color ColWhite   = new Color(0.95f, 0.95f, 0.95f, 1f);

        private const float PanelW    = 240f;
        private const float RowH      = 48f;
        private const float RowGap    = 3f;
        private const float PadX      = 8f;
        private const float HeaderH   = 44f;
        private const float PadBot    = 8f;
        private const float MaxPanelH = 700f;   // altura máxima antes de activar scroll
        private const float SbW       = 8f;     // ancho del scrollbar
        private const float TopOffset = 200f;   // distancia desde top-right (debajo del minimapa)

        // Referencias por fila
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
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);

            // Panel: anclado top-right, debajo del minimapa
            _panel   = MakeRect("SpellsPanel", canvasGo.transform);
            _panelRt = _panel.GetComponent<RectTransform>();
            _panelRt.anchorMin        = new Vector2(1f, 1f);
            _panelRt.anchorMax        = new Vector2(1f, 1f);
            _panelRt.pivot            = new Vector2(1f, 1f);
            _panelRt.anchoredPosition = new Vector2(-8f, -TopOffset);
            _panelRt.sizeDelta        = new Vector2(PanelW, MaxPanelH);

            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = BgPanel;
            panelImg.raycastTarget = true;
            _panel.AddComponent<Outline>().effectColor = new Color(0.30f, 0.30f, 0.30f, 0.5f);

            // Header "HECHIZOS"
            var header   = MakeRect("Header", _panel.transform);
            var headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin        = new Vector2(0, 1);
            headerRt.anchorMax        = new Vector2(1, 1);
            headerRt.pivot            = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta        = new Vector2(0, HeaderH);
            header.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.10f, 0.80f);

            var title   = MakeRect("Title", header.transform);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMax = new Vector2(-44f, 0f);
            var titleTxt = title.AddComponent<Text>();
            titleTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.text      = "HECHIZOS";
            titleTxt.fontSize  = 17;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color     = ColGold;
            titleTxt.alignment = TextAnchor.MiddleCenter;

            // Botón cerrar
            var closeGo  = MakeRect("Close", header.transform);
            var closeRt  = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin        = new Vector2(1, 0.5f);
            closeRt.anchorMax        = new Vector2(1, 0.5f);
            closeRt.pivot            = new Vector2(1, 0.5f);
            closeRt.anchoredPosition = new Vector2(-6f, 0f);
            closeRt.sizeDelta        = new Vector2(36f, 36f);
            closeGo.AddComponent<Image>().color = new Color(0.85f, 0.15f, 0.15f, 0.95f);
            closeGo.AddComponent<Button>().onClick.AddListener(Close);
            var ct = MakeRect("X", closeGo.transform);
            ct.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            ct.GetComponent<RectTransform>().anchorMax = Vector2.one;
            var ctTxt = ct.AddComponent<Text>();
            ctTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ctTxt.text = "✕"; ctTxt.fontSize = 16; ctTxt.fontStyle = FontStyle.Bold;
            ctTxt.color = ColWhite; ctTxt.alignment = TextAnchor.MiddleCenter;

            // Scrollbar (derecha del área de scroll)
            var sbGo = MakeRect("Scrollbar", _panel.transform);
            var sbRt = sbGo.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1, 0);
            sbRt.anchorMax = new Vector2(1, 1);
            sbRt.pivot     = new Vector2(1, 0.5f);
            sbRt.offsetMin = new Vector2(-SbW, PadBot);
            sbRt.offsetMax = new Vector2(0f, -HeaderH);
            sbGo.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.85f);
            var sb = sbGo.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;

            var slidingArea = MakeRect("SlidingArea", sbGo.transform);
            var saRt = slidingArea.GetComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero;
            saRt.anchorMax = Vector2.one;
            saRt.offsetMin = new Vector2(1f, 2f);
            saRt.offsetMax = new Vector2(-1f, -2f);

            var handle    = MakeRect("Handle", slidingArea.transform);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = new Color(0.55f, 0.55f, 0.55f, 0.90f);
            sb.handleRect     = handle.GetComponent<RectTransform>();
            sb.targetGraphic  = handleImg;

            // ScrollRect (viewport)
            var scrollGo = MakeRect("Scroll", _panel.transform);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(PadX, PadBot);
            scrollRt.offsetMax = new Vector2(-(SbW + 2f), -HeaderH);

            scrollGo.AddComponent<RectMask2D>();
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal            = false;
            scrollRect.vertical              = true;
            scrollRect.scrollSensitivity     = 40f;
            scrollRect.verticalScrollbar     = sb;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            // Content con VerticalLayoutGroup + ContentSizeFitter
            var contentGo = MakeRect("Content", scrollGo.transform);
            _listContent  = contentGo.transform;
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0.5f, 1);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing            = RowGap;
            vlg.childAlignment     = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight     = true;
            vlg.padding = new RectOffset(0, 0, 4, 4);

            contentGo.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content  = contentRt;
            scrollRect.viewport = scrollRt;

            // ── Botones ▲/▼ a la derecha del panel ───────────────────────────
            const float BtnW = 28f;
            const float BtnH = 40f;

            var orderBar = MakeRect("OrderBar", _panel.transform);
            var orderRt  = orderBar.GetComponent<RectTransform>();
            orderRt.anchorMin        = new Vector2(1f, 0.5f);
            orderRt.anchorMax        = new Vector2(1f, 0.5f);
            orderRt.pivot            = new Vector2(0f, 0.5f);
            orderRt.anchoredPosition = new Vector2(4f, 0f);
            orderRt.sizeDelta        = new Vector2(BtnW, BtnH * 2 + 4f);
            orderBar.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.80f);

            BuildSideBtn("BtnUp", orderBar.transform, "▲", new Vector2(0,  BtnH / 2f + 2f), () => MoveSpell(-1));
            BuildSideBtn("BtnDn", orderBar.transform, "▼", new Vector2(0, -(BtnH / 2f + 2f)), () => MoveSpell(+1));

            // Pre-poblar filas (28 hechizos máximo)
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

                // Número de slot (izquierda)
                var numGo = MakeRect("Num", row.transform);
                var numRt = numGo.GetComponent<RectTransform>();
                numRt.anchorMin        = new Vector2(0, 0);
                numRt.anchorMax        = new Vector2(0, 1);
                numRt.pivot            = new Vector2(0, 0.5f);
                numRt.anchoredPosition = new Vector2(6f, 0f);
                numRt.sizeDelta        = new Vector2(20f, 0f);
                var numTxt = numGo.AddComponent<Text>();
                numTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                numTxt.fontSize  = 11;
                numTxt.color     = new Color(0.55f, 0.55f, 0.55f, 1f);
                numTxt.alignment = TextAnchor.MiddleCenter;
                numTxt.text      = $"{i + 1}";

                // Nombre del hechizo
                var nameGo = MakeRect("Name", row.transform);
                var nameRt = nameGo.GetComponent<RectTransform>();
                nameRt.anchorMin = new Vector2(0, 0);
                nameRt.anchorMax = new Vector2(1, 1);
                nameRt.offsetMin = new Vector2(30f, 0f);
                nameRt.offsetMax = new Vector2(-58f, 0f);
                var nameTxt = nameGo.AddComponent<Text>();
                nameTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameTxt.fontSize  = 14;
                nameTxt.color     = ColWhite;
                nameTxt.alignment = TextAnchor.MiddleLeft;
                _rowNames[i] = nameTxt;

                // Coste de maná
                var manaGo = MakeRect("Mana", row.transform);
                var manaRt = manaGo.GetComponent<RectTransform>();
                manaRt.anchorMin        = new Vector2(1, 0);
                manaRt.anchorMax        = new Vector2(1, 1);
                manaRt.pivot            = new Vector2(1, 0.5f);
                manaRt.anchoredPosition = new Vector2(-6f, 0f);
                manaRt.sizeDelta        = new Vector2(54f, 0f);
                var manaTxt = manaGo.AddComponent<Text>();
                manaTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                manaTxt.fontSize  = 12;
                manaTxt.color     = ColMana;
                manaTxt.alignment = TextAnchor.MiddleRight;
                _rowManas[i] = manaTxt;

                row.SetActive(false);
            }

            _panel.SetActive(false);
        }

        // ── Lógica ────────────────────────────────────────────────────────────

        public void Toggle() { if (_isOpen) Close(); else Open(); }

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
            var spells      = GameState.Instance.LocalPlayer.Spells;
            int activeCount = 0;

            for (int i = 0; i < LocalPlayer.SpellsSize; i++)
            {
                var slot = spells[i];
                if (slot.IsEmpty)
                {
                    _rowBgs[i].gameObject.SetActive(false);
                    continue;
                }
                _rowBgs[i].gameObject.SetActive(true);
                _rowNames[i].text = slot.Name;
                _rowManas[i].text = $"{slot.ManaRequired}✦";
                UpdateRowColor(i);
                activeCount++;
            }

            // Ajustar altura del panel al contenido (capped a MaxPanelH)
            float contentH = activeCount * RowH + Mathf.Max(0, activeCount - 1) * RowGap + 8f;
            float panelH   = Mathf.Clamp(HeaderH + contentH + PadBot, HeaderH + RowH + PadBot, MaxPanelH);
            _panelRt.sizeDelta = new Vector2(PanelW, panelH);
        }

        private void OnSpellClick(int slotIndex)
        {
            var spell = GameState.Instance.LocalPlayer.Spells[slotIndex];
            if (spell.IsEmpty) return;

            SelectedSpellSlot = (SelectedSpellSlot == slotIndex) ? -1 : slotIndex;

            for (int i = 0; i < LocalPlayer.SpellsSize; i++)
                UpdateRowColor(i);
        }

        private void UpdateRowColor(int i)
        {
            var spell = GameState.Instance.LocalPlayer.Spells[i];
            if (spell.IsEmpty) { _rowBgs[i].color = BgRow; return; }
            _rowBgs[i].color = (i == SelectedSpellSlot) ? BgSelected : BgRow;
        }

        /// <summary>Llamado por PlayerController tras lanzar el hechizo.</summary>
        public void ClearSelection()
        {
            SelectedSpellSlot = -1;
            for (int i = 0; i < LocalPlayer.SpellsSize; i++)
                UpdateRowColor(i);
        }

        // ── Reordenamiento ────────────────────────────────────────────────────

        private void BuildSideBtn(string name, Transform parent, string label,
                                   Vector2 pos, System.Action onClick)
        {
            var go = MakeRect(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(26f, 38f);
            go.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.90f);
            var btn    = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.40f, 0.35f, 0.10f, 1f);
            colors.pressedColor     = new Color(0.60f, 0.50f, 0.10f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());

            var lblGo = MakeRect("L", go.transform);
            lblGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            lblGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            var txt = lblGo.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text      = label;
            txt.fontSize  = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = ColGold;
            txt.alignment = TextAnchor.MiddleCenter;
        }

        private void MoveSpell(int direction)
        {
            if (SelectedSpellSlot < 0) return;

            var spells = GameState.Instance.LocalPlayer.Spells;
            int target = SelectedSpellSlot + direction;

            while (target >= 0 && target < LocalPlayer.SpellsSize && spells[target].IsEmpty)
                target += direction;

            if (target < 0 || target >= LocalPlayer.SpellsSize) return;

            int from = SelectedSpellSlot;
            SelectedSpellSlot = target;  // primero, para que Refresh() pinte la fila correcta
            GameState.Instance.LocalPlayer.SwapSpells(from, target);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static GameObject MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }
    }
}
