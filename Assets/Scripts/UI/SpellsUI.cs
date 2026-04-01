using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArgentumOnline.Game;

namespace ArgentumOnline.UI
{
    /// <summary>
    /// Panel lateral derecho con los hechizos del jugador.
    /// Tap en un hechizo lo selecciona; el siguiente click en el mundo lo lanza.
    /// Soporta minimizar/maximizar y arrastre libre.
    /// </summary>
    public class SpellsUI : MonoBehaviour
    {
        public static SpellsUI Instance { get; private set; }

        /// <summary>-1 = ninguno seleccionado, 0-27 = slot del hechizo activo.</summary>
        public int SelectedSpellSlot { get; private set; } = -1;

        private RectTransform _panelRt;
        private GameObject    _panel;
        private Transform     _listContent;
        private GameObject    _scrollGo;
        private GameObject    _scrollbarGo;
        private GameObject    _orderBarGo;
        private ScrollRect    _scrollRect;
        private RectTransform _scrollHandleRt;
        private bool          _isOpen;
        private bool          _minimized;

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
        private const float PadBot    = 30f;  // espacio para la barra ▲/▼ en el pie
        private const float MaxPanelH = 500f;
        private const float SbW       = 10f;
        private const float TopOffset = 200f;

        // Referencias por fila
        private Image[] _rowBgs;
        private Text[]  _rowNames;
        private Text[]  _rowManas;
        private Text    _minBtnTxt;

        void Awake() => Instance = this;

        void Update()
        {
            if (!_isOpen || _minimized || _scrollRect == null || _scrollHandleRt == null) return;
            float content  = _scrollRect.content.rect.height;
            float viewport = ((RectTransform)_scrollRect.transform).rect.height;
            if (content <= viewport || content <= 0f)
            {
                _scrollHandleRt.gameObject.SetActive(false);
                return;
            }
            _scrollHandleRt.gameObject.SetActive(true);
            float ratio  = Mathf.Clamp01(viewport / content);
            float nPos   = _scrollRect.normalizedPosition.y;          // 1=top, 0=bottom
            float bot    = nPos * (1f - ratio);
            _scrollHandleRt.anchorMin = new Vector2(0f, bot);
            _scrollHandleRt.anchorMax = new Vector2(1f, bot + ratio);
            _scrollHandleRt.offsetMin = Vector2.zero;
            _scrollHandleRt.offsetMax = Vector2.zero;
        }

        void Start()
        {
            BuildUI();
            _minimized = false;  // siempre expandido al inicio
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
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);

            // Panel: anclado top-right, debajo del minimapa
            _panel   = MakeRect("SpellsPanel", canvasGo.transform);
            _panelRt = _panel.GetComponent<RectTransform>();
            _panelRt.anchorMin        = new Vector2(1f, 1f);
            _panelRt.anchorMax        = new Vector2(1f, 1f);
            _panelRt.pivot            = new Vector2(1f, 1f);
            _panelRt.anchoredPosition = new Vector2(-8f, -TopOffset);
            _panelRt.sizeDelta        = new Vector2(PanelW, HeaderH);

            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = BgPanel;
            panelImg.raycastTarget = true;
            _panel.AddComponent<Outline>().effectColor = new Color(0.30f, 0.30f, 0.30f, 0.5f);

            // Header
            var header   = MakeRect("Header", _panel.transform);
            var headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin        = new Vector2(0, 1);
            headerRt.anchorMax        = new Vector2(1, 1);
            headerRt.pivot            = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta        = new Vector2(0, HeaderH);
            header.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.10f, 0.80f);

            // Ícono de drag
            var dragIconGo = MakeRect("DragIcon", header.transform);
            var dragIconRt = dragIconGo.GetComponent<RectTransform>();
            dragIconRt.anchorMin        = new Vector2(0, 0.5f);
            dragIconRt.anchorMax        = new Vector2(0, 0.5f);
            dragIconRt.pivot            = new Vector2(0, 0.5f);
            dragIconRt.anchoredPosition = new Vector2(6f, 0f);
            dragIconRt.sizeDelta        = new Vector2(20f, HeaderH);
            var dragTxt = dragIconGo.AddComponent<Text>();
            dragTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dragTxt.text      = "⠿";
            dragTxt.fontSize  = 18;
            dragTxt.color     = new Color(0.55f, 0.55f, 0.55f, 0.9f);
            dragTxt.alignment = TextAnchor.MiddleCenter;

            var drag = header.AddComponent<PanelDragHandler>();
            drag.PanelRt = _panelRt;
            drag.SaveKey = "SpellsUI_Panel";

            // Título
            var title   = MakeRect("Title", header.transform);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = new Vector2(28f, 0f);
            titleRt.offsetMax = new Vector2(-84f, 0f);
            var titleTxt = title.AddComponent<Text>();
            titleTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.text      = "HECHIZOS";
            titleTxt.fontSize  = 17;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color     = ColGold;
            titleTxt.alignment = TextAnchor.MiddleCenter;

            // Botón minimizar
            var minGo  = MakeRect("Minimize", header.transform);
            var minRt  = minGo.GetComponent<RectTransform>();
            minRt.anchorMin        = new Vector2(1, 0.5f);
            minRt.anchorMax        = new Vector2(1, 0.5f);
            minRt.pivot            = new Vector2(1, 0.5f);
            minRt.anchoredPosition = new Vector2(-46f, 0f);
            minRt.sizeDelta        = new Vector2(32f, 32f);
            minGo.AddComponent<Image>().color = new Color(0.20f, 0.20f, 0.20f, 0.95f);
            minGo.AddComponent<Button>().onClick.AddListener(ToggleMinimize);
            var minLabel = MakeRect("L", minGo.transform);
            minLabel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            minLabel.GetComponent<RectTransform>().anchorMax = Vector2.one;
            _minBtnTxt = minLabel.AddComponent<Text>();
            _minBtnTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _minBtnTxt.text      = "−";
            _minBtnTxt.fontSize  = 18;
            _minBtnTxt.fontStyle = FontStyle.Bold;
            _minBtnTxt.color     = ColWhite;
            _minBtnTxt.alignment = TextAnchor.MiddleCenter;

            // Botón cerrar
            var closeGo  = MakeRect("Close", header.transform);
            var closeRt  = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin        = new Vector2(1, 0.5f);
            closeRt.anchorMax        = new Vector2(1, 0.5f);
            closeRt.pivot            = new Vector2(1, 0.5f);
            closeRt.anchoredPosition = new Vector2(-8f, 0f);
            closeRt.sizeDelta        = new Vector2(32f, 32f);
            closeGo.AddComponent<Image>().color = new Color(0.85f, 0.15f, 0.15f, 0.95f);
            closeGo.AddComponent<Button>().onClick.AddListener(Close);
            var ct = MakeRect("X", closeGo.transform);
            ct.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            ct.GetComponent<RectTransform>().anchorMax = Vector2.one;
            var ctTxt = ct.AddComponent<Text>();
            ctTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ctTxt.text = "✕"; ctTxt.fontSize = 15; ctTxt.fontStyle = FontStyle.Bold;
            ctTxt.color = ColWhite; ctTxt.alignment = TextAnchor.MiddleCenter;

            // Indicador de scroll custom (sin componente Scrollbar — ancho garantizado = SbW)
            _scrollbarGo = MakeRect("ScrollIndicator", _panel.transform);
            var sbRt = _scrollbarGo.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1, 0);
            sbRt.anchorMax = new Vector2(1, 1);
            sbRt.pivot     = new Vector2(1, 0.5f);
            sbRt.offsetMin = new Vector2(-SbW, PadBot);
            sbRt.offsetMax = new Vector2(0f, -HeaderH);
            var trackImg = _scrollbarGo.AddComponent<Image>();
            trackImg.color = new Color(0.08f, 0.08f, 0.08f, 0.40f);
            trackImg.raycastTarget = false;

            var handleGo  = MakeRect("Handle", _scrollbarGo.transform);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = new Color(0.65f, 0.65f, 0.65f, 0.90f);
            handleImg.raycastTarget = false;
            _scrollHandleRt = handleGo.GetComponent<RectTransform>();
            _scrollHandleRt.anchorMin = new Vector2(0, 0.8f);
            _scrollHandleRt.anchorMax = new Vector2(1, 1f);
            _scrollHandleRt.offsetMin = Vector2.zero;
            _scrollHandleRt.offsetMax = Vector2.zero;

            // ScrollRect
            _scrollGo = MakeRect("Scroll", _panel.transform);
            var scrollRt = _scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(PadX, PadBot);
            scrollRt.offsetMax = new Vector2(-(SbW + 2f), -HeaderH);

            _scrollGo.AddComponent<RectMask2D>();
            var scrollRect = _scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal        = false;
            scrollRect.vertical          = true;
            scrollRect.scrollSensitivity = 40f;
            _scrollRect = scrollRect;

            // Content
            var contentGo = MakeRect("Content", _scrollGo.transform);
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

            // Botones ▲/▼ en el pie del panel (dentro, no afuera)
            const float FootH = 26f;

            _orderBarGo = MakeRect("OrderBar", _panel.transform);
            var orderRt  = _orderBarGo.GetComponent<RectTransform>();
            orderRt.anchorMin = new Vector2(0, 0);
            orderRt.anchorMax = new Vector2(1, 0);
            orderRt.pivot     = new Vector2(0.5f, 0);
            orderRt.offsetMin = new Vector2(0, 0);
            orderRt.offsetMax = new Vector2(0, FootH);
            _orderBarGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.80f);

            BuildFootBtn("BtnUp", _orderBarGo.transform, "▲", new Vector2(-22f, 0), () => MoveSpell(-1));
            BuildFootBtn("BtnDn", _orderBarGo.transform, "▼", new Vector2( 22f, 0), () => MoveSpell(+1));

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

                // Número de slot
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
                nameRt.offsetMax = new Vector2(-44f, 0f);
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
                manaRt.anchoredPosition = new Vector2(-4f, 0f);
                manaRt.sizeDelta        = new Vector2(38f, 0f);
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
            _panel.SetActive(true);
            ApplyMinimizeState();
            if (!_minimized) Refresh();
        }

        public void Close()
        {
            _isOpen = false;
            _panel.SetActive(false);
        }

        private void ToggleMinimize()
        {
            _minimized = !_minimized;
            PlayerPrefs.SetInt("SpellsUI_Minimized", _minimized ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMinimizeState();
            if (!_minimized && _isOpen) Refresh();
        }

        private void ApplyMinimizeState()
        {
            _scrollGo.SetActive(!_minimized);
            _scrollbarGo.SetActive(!_minimized);
            _orderBarGo.SetActive(!_minimized);
            _minBtnTxt.text = _minimized ? "□" : "−";

            if (_minimized)
            {
                _panelRt.sizeDelta = new Vector2(PanelW, HeaderH);
            }
            else
            {
                RefreshPanelHeight();
            }
        }

        private void RefreshPanelHeight()
        {
            if (GameState.Instance?.LocalPlayer == null) return;
            var spells = GameState.Instance.LocalPlayer.Spells;
            int activeCount = 0;
            for (int i = 0; i < LocalPlayer.SpellsSize; i++)
                if (!spells[i].IsEmpty) activeCount++;

            float contentH = activeCount * RowH + Mathf.Max(0, activeCount - 1) * RowGap + 8f;
            float panelH   = Mathf.Clamp(HeaderH + contentH + PadBot, HeaderH + RowH + PadBot, MaxPanelH);
            _panelRt.sizeDelta = new Vector2(PanelW, panelH);
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

            if (!_minimized)
            {
                float contentH = activeCount * RowH + Mathf.Max(0, activeCount - 1) * RowGap + 8f;
                float panelH   = Mathf.Clamp(HeaderH + contentH + PadBot, HeaderH + RowH + PadBot, MaxPanelH);
                _panelRt.sizeDelta = new Vector2(PanelW, panelH);
            }
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

        public void ClearSelection()
        {
            SelectedSpellSlot = -1;
            for (int i = 0; i < LocalPlayer.SpellsSize; i++)
                UpdateRowColor(i);
        }

        // ── Reordenamiento ────────────────────────────────────────────────────

        private void BuildFootBtn(string name, Transform parent, string label,
                                   Vector2 offset, System.Action onClick)
        {
            var go = MakeRect(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta        = new Vector2(36f, 22f);
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
            txt.fontSize  = 13;
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
            SelectedSpellSlot = target;
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

    /// <summary>
    /// Permite arrastrar el panel. Guarda/restaura posición en PlayerPrefs.
    /// </summary>
    public class PanelDragHandler : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform PanelRt;
        public string        SaveKey = "Panel";

        private Vector2       _startAnchoredPos;
        private Vector2       _startPointerLocal;
        private RectTransform _canvasRt;
        private Canvas        _canvas;

        void Start()
        {
            _canvas   = PanelRt.GetComponentInParent<Canvas>();
            _canvasRt = _canvas.GetComponent<RectTransform>();

            if (PlayerPrefs.HasKey(SaveKey + "_x"))
            {
                float x = PlayerPrefs.GetFloat(SaveKey + "_x");
                float y = PlayerPrefs.GetFloat(SaveKey + "_y");
                PanelRt.anchoredPosition = new Vector2(x, y);
                ClampToScreen();
            }
        }

        public void OnBeginDrag(PointerEventData e)
        {
            _startAnchoredPos = PanelRt.anchoredPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRt, e.position, null, out _startPointerLocal);
        }

        public void OnDrag(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRt, e.position, null, out var currentLocal);
            PanelRt.anchoredPosition = _startAnchoredPos + (currentLocal - _startPointerLocal);
            ClampToScreen();
        }

        public void OnEndDrag(PointerEventData e)
        {
            PlayerPrefs.SetFloat(SaveKey + "_x", PanelRt.anchoredPosition.x);
            PlayerPrefs.SetFloat(SaveKey + "_y", PanelRt.anchoredPosition.y);
            PlayerPrefs.Save();
        }

        private void ClampToScreen()
        {
            var corners = new Vector3[4];
            PanelRt.GetWorldCorners(corners);
            float sf = _canvas.scaleFactor;

            Vector2 adj = Vector2.zero;
            if (corners[0].x < 0)             adj.x =  -corners[0].x;
            if (corners[2].x > Screen.width)  adj.x = Screen.width  - corners[2].x;
            if (corners[0].y < 0)             adj.y =  -corners[0].y;
            if (corners[1].y > Screen.height) adj.y = Screen.height - corners[1].y;

            if (adj != Vector2.zero)
                PanelRt.anchoredPosition += adj / sf;
        }
    }
}
