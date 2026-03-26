using UnityEngine;
using UnityEngine.UI;
using ArgentumOnline.Game;
using ArgentumOnline.Renderer;

namespace ArgentumOnline.UI
{
    /// <summary>
    /// Ventana de comercio con NPC.
    /// Columna izquierda: items del NPC (comprar).
    /// Columna derecha: items del jugador vendibles (vender).
    /// Se abre al recibir OpenTrade y se cierra al tocar X o fuera del panel.
    /// </summary>
    public class TradeUI : MonoBehaviour
    {
        public static TradeUI Instance { get; private set; }

        private Canvas     _canvas;
        private GameObject _blocker;
        private GameObject _panel;
        private Text       _goldText;
        private Transform  _buyList;
        private Transform  _sellList;

        private const float PanelW   = 700f;
        private const float PanelH   = 860f;
        private const float ColW     = 320f;
        private const float RowH     = 72f;
        private const float ListH    = 640f;

        private static readonly Color BgPanel     = new Color(0.07f, 0.05f, 0.03f, 0.97f);
        private static readonly Color BgRow       = new Color(0.16f, 0.12f, 0.07f, 1f);
        private static readonly Color BgRowHover  = new Color(0.26f, 0.20f, 0.10f, 1f);
        private static readonly Color BtnBuy      = new Color(0.18f, 0.55f, 0.22f, 1f);
        private static readonly Color BtnSell     = new Color(0.55f, 0.35f, 0.10f, 1f);
        private static readonly Color BtnDisabled = new Color(0.30f, 0.25f, 0.20f, 1f);
        private static readonly Color Gold        = new Color(0.92f, 0.80f, 0.45f, 1f);
        private static readonly Color TextLight   = new Color(0.92f, 0.90f, 0.85f, 1f);
        private static readonly Color TextMuted   = new Color(0.60f, 0.55f, 0.45f, 1f);
        private static readonly Color TextInvalid = new Color(0.75f, 0.35f, 0.30f, 1f);

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            BuildCanvas();
            GameState.Instance.OnTradeOpen    += OnTradeOpen;
            GameState.Instance.LocalPlayer.OnGoldChanged += RefreshGold;
        }

        void OnDestroy()
        {
            GameState.Instance.OnTradeOpen    -= OnTradeOpen;
            GameState.Instance.LocalPlayer.OnGoldChanged -= RefreshGold;
        }

        // ── Evento ───────────────────────────────────────────────────────────

        private void OnTradeOpen(TradeData data)
        {
            PopulateLists(data);
            _blocker.SetActive(true);
            _panel.SetActive(true);
            RefreshGold();
        }

        // ── Build canvas (una sola vez) ───────────────────────────────────────

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("TradeCanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 25;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);

            // Fondo oscuro bloqueador — oculto por defecto junto al panel
            _blocker = MakeRect("Blocker", canvasGo.transform,
                                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                                Vector2.zero, Vector2.zero);
            var blockerImg = _blocker.AddComponent<Image>();
            blockerImg.color = new Color(0, 0, 0, 0.5f);
            var blockerBtn = _blocker.AddComponent<Button>();
            blockerBtn.transition = Selectable.Transition.None;
            blockerBtn.onClick.AddListener(Close);
            _blocker.SetActive(false);

            // Panel principal
            _panel = MakeRect("TradePanel", canvasGo.transform,
                               new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                               new Vector2(0.5f, 0.5f), Vector2.zero,
                               new Vector2(PanelW, PanelH));
            _panel.AddComponent<Image>().color = BgPanel;
            _panel.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.85f);

            // Detener click en panel para no cerrar el blocker
            var panelBtn = _panel.AddComponent<Button>();
            panelBtn.transition = Selectable.Transition.None;
            panelBtn.onClick.AddListener(() => { });

            BuildHeader();
            BuildColumns();

            _panel.SetActive(false);
        }

        private void BuildHeader()
        {
            // Título
            var titleGo = MakeRect("Title", _panel.transform,
                                   new Vector2(0, 1), new Vector2(1, 1),
                                   new Vector2(0.5f, 1), new Vector2(0, -10),
                                   new Vector2(0, 50));
            var title = titleGo.AddComponent<Text>();
            title.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.text      = "COMERCIO";
            title.fontSize  = 26;
            title.fontStyle = FontStyle.Bold;
            title.color     = Gold;
            title.alignment = TextAnchor.MiddleCenter;
            titleGo.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);

            // Oro del jugador
            var goldGo = MakeRect("Gold", _panel.transform,
                                  new Vector2(0, 1), new Vector2(1, 1),
                                  new Vector2(0.5f, 1), new Vector2(0, -58),
                                  new Vector2(0, 28));
            _goldText = goldGo.AddComponent<Text>();
            _goldText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _goldText.fontSize  = 17;
            _goldText.color     = Gold;
            _goldText.alignment = TextAnchor.MiddleCenter;

            // Divisor
            var div = MakeRect("Div", _panel.transform,
                                new Vector2(0, 1), new Vector2(1, 1),
                                new Vector2(0.5f, 1), new Vector2(0, -90),
                                new Vector2(-20, 1));
            div.AddComponent<Image>().color = new Color(0.5f, 0.4f, 0.15f, 0.4f);

            // Cabeceras de columna
            MakeColumnHeader("Comprar (NPC)", -ColW * 0.5f - 10, -100);
            MakeColumnHeader("Vender (mis items)", ColW * 0.5f + 10, -100);

            // Botón cerrar
            var closeGo = MakeRect("Close", _panel.transform,
                                   new Vector2(1, 1), new Vector2(1, 1),
                                   new Vector2(1, 1), new Vector2(-10, -10),
                                   new Vector2(44, 44));
            closeGo.AddComponent<Image>().color = new Color(0.55f, 0.12f, 0.08f, 0.92f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(Close);
            var xTxt = MakeRect("X", closeGo.transform,
                                  Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                                  Vector2.zero, Vector2.zero).AddComponent<Text>();
            xTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            xTxt.text      = "✕";
            xTxt.fontSize  = 20;
            xTxt.fontStyle = FontStyle.Bold;
            xTxt.color     = Color.white;
            xTxt.alignment = TextAnchor.MiddleCenter;
        }

        private void MakeColumnHeader(string label, float x, float y)
        {
            var go = MakeRect(label, _panel.transform,
                              new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                              new Vector2(0.5f, 1), new Vector2(x, y),
                              new Vector2(ColW, 28));
            var txt = go.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text      = label;
            txt.fontSize  = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = new Color(0.80f, 0.70f, 0.40f, 1f);
            txt.alignment = TextAnchor.MiddleCenter;
        }

        private void BuildColumns()
        {
            _buyList  = BuildScrollList("BuyList",  -ColW * 0.5f - 10, -132);
            _sellList = BuildScrollList("SellList",  ColW * 0.5f + 10, -132);
        }

        private Transform BuildScrollList(string name, float x, float y)
        {
            // Viewport + máscara
            var viewport = MakeRect(name, _panel.transform,
                                    new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                                    new Vector2(0.5f, 1), new Vector2(x, y),
                                    new Vector2(ColW, ListH));
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            viewport.AddComponent<Image>().color = new Color(0.10f, 0.08f, 0.04f, 0.6f);

            // Contenedor de filas (crece hacia abajo)
            var content = MakeRect("Content", viewport.transform,
                                   new Vector2(0, 1), new Vector2(1, 1),
                                   new Vector2(0, 1), Vector2.zero, Vector2.zero);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing               = 4f;
            vlg.padding               = new RectOffset(4, 4, 4, 4);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight= false;
            vlg.childControlWidth     = true;
            vlg.childControlHeight    = false;
            content.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            // ScrollRect
            var sr = viewport.AddComponent<ScrollRect>();
            sr.content          = content.GetComponent<RectTransform>();
            sr.viewport         = viewport.GetComponent<RectTransform>();
            sr.horizontal       = false;
            sr.vertical         = true;
            sr.scrollSensitivity= 30f;
            sr.movementType     = ScrollRect.MovementType.Clamped;

            return content.transform;
        }

        // ── Populate ─────────────────────────────────────────────────────────

        private void PopulateLists(TradeData data)
        {
            // Limpiar listas anteriores
            foreach (Transform t in _buyList)  Destroy(t.gameObject);
            foreach (Transform t in _sellList) Destroy(t.gameObject);

            foreach (var item in data.NpcItems)
                AddBuyRow(item);

            foreach (var item in data.UserItems)
                AddSellRow(item);
        }

        private void AddBuyRow(NpcTradeItem item)
        {
            bool canAfford  = GameState.Instance.LocalPlayer.Gold >= item.Valor;
            bool validClass = item.ValidParaClase;

            var row = MakeRow(_buyList);

            // Ícono
            var iconGo = MakeRect("Icon", row.transform,
                                  new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                                  new Vector2(0, 0.5f), new Vector2(6, 0),
                                  new Vector2(52, 52));
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.enabled = false;
            if (item.GrhIndex > 0)
            {
                var capturedImg = iconImg;
                GrhDatabase.Instance.GetSprite(item.GrhIndex, sprite =>
                {
                    if (capturedImg == null) return;
                    capturedImg.sprite  = sprite;
                    capturedImg.enabled = sprite != null;
                    capturedImg.preserveAspect = true;
                });
            }

            // Info (nombre + stats + precio)
            var infoGo = MakeRect("Info", row.transform,
                                  new Vector2(0, 0), new Vector2(1, 1),
                                  new Vector2(0, 0.5f), new Vector2(64, 0),
                                  new Vector2(-130, 0));
            AddInfoTexts(infoGo.transform, item.Name, item.DataObj,
                         $"{item.Valor} oro", validClass);

            // Botón Comprar
            float btnW = 58f;
            var btnGo = MakeRect("BuyBtn", row.transform,
                                 new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                                 new Vector2(1, 0.5f), new Vector2(-6, 0),
                                 new Vector2(btnW, 44));
            bool enabled = canAfford && validClass;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = enabled ? BtnBuy : BtnDisabled;
            var btn = btnGo.AddComponent<Button>();
            btn.interactable = enabled;
            var idPos = item.IdPos;
            btn.onClick.AddListener(() =>
            {
                Network.NetworkManager.Instance.Send(
                    Network.PacketSerializer.Instance.BuyItem(idPos, 1));
                Close();
            });
            var btnTxt = MakeRect("T", btnGo.transform, Vector2.zero, Vector2.one,
                                  new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .AddComponent<Text>();
            btnTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.text      = "Comprar";
            btnTxt.fontSize  = 12;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.color     = Color.white;
            btnTxt.alignment = TextAnchor.MiddleCenter;
        }

        private void AddSellRow(UserTradeItem item)
        {
            var row = MakeRow(_sellList);

            // Ícono
            var iconGo = MakeRect("Icon", row.transform,
                                  new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                                  new Vector2(0, 0.5f), new Vector2(6, 0),
                                  new Vector2(52, 52));
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.enabled = false;
            if (item.GrhIndex > 0)
            {
                var capturedImg = iconImg;
                GrhDatabase.Instance.GetSprite(item.GrhIndex, sprite =>
                {
                    if (capturedImg == null) return;
                    capturedImg.sprite  = sprite;
                    capturedImg.enabled = sprite != null;
                    capturedImg.preserveAspect = true;
                });
            }

            // Info
            var infoGo = MakeRect("Info", row.transform,
                                  new Vector2(0, 0), new Vector2(1, 1),
                                  new Vector2(0, 0.5f), new Vector2(64, 0),
                                  new Vector2(-130, 0));
            string cantStr = item.Cantidad > 1 ? $"x{item.Cantidad}  " : "";
            AddInfoTexts(infoGo.transform, item.Name, item.DataObj,
                         $"{cantStr}{item.ValorVenta} oro", true);

            // Botón Vender
            var btnGo = MakeRect("SellBtn", row.transform,
                                 new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                                 new Vector2(1, 0.5f), new Vector2(-6, 0),
                                 new Vector2(58, 44));
            btnGo.AddComponent<Image>().color = BtnSell;
            var btn = btnGo.AddComponent<Button>();
            var idPos = item.IdPos;
            btn.onClick.AddListener(() =>
            {
                Network.NetworkManager.Instance.Send(
                    Network.PacketSerializer.Instance.SellItem(idPos, 1));
                Close();
            });
            var btnTxt = MakeRect("T", btnGo.transform, Vector2.zero, Vector2.one,
                                  new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .AddComponent<Text>();
            btnTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.text      = "Vender";
            btnTxt.fontSize  = 12;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.color     = Color.white;
            btnTxt.alignment = TextAnchor.MiddleCenter;
        }

        // ── Helpers de fila ───────────────────────────────────────────────────

        private static GameObject MakeRow(Transform parent)
        {
            var row = MakeRect("Row", parent,
                               new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                               new Vector2(0.5f, 0.5f), Vector2.zero,
                               new Vector2(0, RowH));
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = RowH;
            row.AddComponent<Image>().color = BgRow;
            return row;
        }

        private static void AddInfoTexts(Transform parent,
                                         string name, string dataObj,
                                         string priceStr, bool validClass)
        {
            // Nombre
            var nameGo = MakeRect("Name", parent,
                                  new Vector2(0, 1), new Vector2(1, 1),
                                  new Vector2(0, 1), new Vector2(0, -4),
                                  new Vector2(0, 22));
            var nameTxt = nameGo.AddComponent<Text>();
            nameTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.text      = name;
            nameTxt.fontSize  = 15;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.color     = validClass ? TextLight : TextInvalid;
            nameTxt.alignment = TextAnchor.MiddleLeft;
            nameTxt.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Stats (DataObj) — solo si hay datos
            float priceY = -28f;
            if (!string.IsNullOrEmpty(dataObj))
            {
                var statsGo = MakeRect("Stats", parent,
                                       new Vector2(0, 1), new Vector2(1, 1),
                                       new Vector2(0, 1), new Vector2(0, -28),
                                       new Vector2(0, 18));
                var statsTxt = statsGo.AddComponent<Text>();
                statsTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                statsTxt.text      = dataObj;
                statsTxt.fontSize  = 12;
                statsTxt.color     = new Color(0.75f, 0.75f, 0.65f, 1f);
                statsTxt.alignment = TextAnchor.MiddleLeft;
                priceY = -46f;
            }

            // Precio
            var priceGo = MakeRect("Price", parent,
                                   new Vector2(0, 1), new Vector2(1, 1),
                                   new Vector2(0, 1), new Vector2(0, priceY),
                                   new Vector2(0, 18));
            var priceTxt = priceGo.AddComponent<Text>();
            priceTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            priceTxt.text      = priceStr;
            priceTxt.fontSize  = 13;
            priceTxt.color     = Gold;
            priceTxt.alignment = TextAnchor.MiddleLeft;

            // Indicador "no válido para tu clase"
            if (!validClass)
            {
                var warnGo = MakeRect("Warn", parent,
                                      new Vector2(0, 0), new Vector2(1, 0),
                                      new Vector2(0, 0), new Vector2(0, 4),
                                      new Vector2(0, 16));
                var warnTxt = warnGo.AddComponent<Text>();
                warnTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                warnTxt.text      = "No válido para tu clase";
                warnTxt.fontSize  = 11;
                warnTxt.color     = TextInvalid;
                warnTxt.alignment = TextAnchor.MiddleLeft;
            }
        }

        // ── Gold ──────────────────────────────────────────────────────────────

        private void RefreshGold()
        {
            if (_goldText != null)
                _goldText.text = $"Oro: {GameState.Instance.LocalPlayer.Gold}";
        }

        // ── Close ─────────────────────────────────────────────────────────────

        public void Close()
        {
            _blocker.SetActive(false);
            _panel.SetActive(false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static GameObject MakeRect(string name, Transform parent,
                                           Vector2 anchorMin, Vector2 anchorMax,
                                           Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            return go;
        }
    }
}
