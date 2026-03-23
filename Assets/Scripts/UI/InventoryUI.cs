using UnityEngine;
using UnityEngine.UI;
using ArgentumOnline.Game;
using ArgentumOnline.Renderer;

namespace ArgentumOnline.UI
{
    /// <summary>
    /// Panel de inventario: grid 5×5 (21 slots). Se abre/cierra con el botón INV.
    /// Los sprites de items se cargan async desde GrhDatabase por grhIndex.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        private GameObject _panel;
        private Image[]    _slotBgs;
        private Image[]    _slotIcons;
        private Text[]     _slotCounts;
        private bool       _isOpen;

        private const int   Cols     = 5;
        private const float SlotSize = 86f;
        private const float SlotGap  = 6f;
        private const float PadX     = 14f;
        private const float HeaderH  = 52f;
        private const float PadBot   = 14f;

        // Colores
        private static readonly Color BgPanel    = new Color(0.09f, 0.07f, 0.04f, 0.97f);
        private static readonly Color BgSlot     = new Color(0.20f, 0.16f, 0.10f, 1f);
        private static readonly Color BgEquipped = new Color(0.42f, 0.32f, 0.05f, 1f);
        private static readonly Color Gold       = new Color(0.92f, 0.80f, 0.45f, 1f);

        void Awake() => Instance = this;

        void Start()
        {
            BuildUI();
            GameState.Instance.OnConnected += OnConnected;
        }

        void OnDestroy()
        {
            GameState.Instance.OnConnected -= OnConnected;
        }

        private void OnConnected()
        {
            GameState.Instance.LocalPlayer.OnInventoryChanged += Refresh;
        }

        // ── Construcción ──────────────────────────────────────────────────────

        private void BuildUI()
        {
            var canvasGo = new GameObject("InventoryCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);

            BuildPanel(canvasGo.transform);
            _panel.SetActive(false);
        }


        // Panel central con los 21 slots
        private void BuildPanel(Transform parent)
        {
            int rows     = Mathf.CeilToInt((float)LocalPlayer.InventorySize / Cols);
            float panelW = PadX * 2 + Cols * SlotSize + (Cols - 1) * SlotGap;
            float panelH = HeaderH + rows * SlotSize + (rows - 1) * SlotGap + PadBot;

            _panel = Rect("InvPanel", parent,
                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(0.5f, 0.5f), Vector2.zero,
                          new Vector2(panelW, panelH));

            var bg = _panel.AddComponent<Image>();
            bg.sprite = MakeRounded((int)panelW, (int)panelH, 16,
                                    new Color(0.09f, 0.07f, 0.04f, 0f));
            bg.color  = BgPanel;
            bg.type   = Image.Type.Simple;
            _panel.AddComponent<Shadow>().effectColor    = new Color(0, 0, 0, 0.8f);

            // Título
            var titleGo = Rect("Title", _panel.transform,
                               new Vector2(0, 1), new Vector2(1, 1),
                               new Vector2(0.5f, 1), Vector2.zero,
                               new Vector2(0, HeaderH));
            var title = titleGo.AddComponent<Text>();
            title.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.text      = "INVENTARIO";
            title.fontSize  = 24;
            title.fontStyle = FontStyle.Bold;
            title.color     = Gold;
            title.alignment = TextAnchor.MiddleCenter;
            titleGo.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);

            // Botón cerrar ×
            var closeGo = Rect("Close", _panel.transform,
                               new Vector2(1, 1), new Vector2(1, 1),
                               new Vector2(1, 1), new Vector2(-8, -8),
                               new Vector2(40, 40));
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.55f, 0.12f, 0.08f, 0.92f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.onClick.AddListener(TogglePanel);
            var xGo = Rect("X", closeGo.transform, Vector2.zero, Vector2.one,
                           new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var xTxt = xGo.AddComponent<Text>();
            xTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            xTxt.text      = "✕";
            xTxt.fontSize  = 20;
            xTxt.fontStyle = FontStyle.Bold;
            xTxt.color     = Color.white;
            xTxt.alignment = TextAnchor.MiddleCenter;

            // Divisor bajo el título
            var divGo = Rect("Divider", _panel.transform,
                             new Vector2(0, 1), new Vector2(1, 1),
                             new Vector2(0.5f, 1), new Vector2(0, -HeaderH + 4),
                             new Vector2(-20, 1));
            var divImg = divGo.AddComponent<Image>();
            divImg.color = new Color(0.6f, 0.5f, 0.2f, 0.4f);

            // Slots
            _slotBgs    = new Image[LocalPlayer.InventorySize];
            _slotIcons  = new Image[LocalPlayer.InventorySize];
            _slotCounts = new Text[LocalPlayer.InventorySize];

            for (int i = 0; i < LocalPlayer.InventorySize; i++)
            {
                int   col = i % Cols;
                int   row = i / Cols;
                float x   = -panelW / 2 + PadX + col * (SlotSize + SlotGap) + SlotSize / 2;
                float y   =  panelH / 2 - HeaderH - row * (SlotSize + SlotGap) - SlotSize / 2;
                BuildSlot(i, _panel.transform, x, y);
            }
        }

        private void BuildSlot(int idx, Transform parent, float x, float y)
        {
            var go = Rect($"Slot{idx}", parent,
                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(0.5f, 0.5f), new Vector2(x, y),
                          new Vector2(SlotSize, SlotSize));

            var bg = go.AddComponent<Image>();
            bg.sprite = MakeRounded((int)SlotSize, (int)SlotSize, 8, Color.white);
            bg.color  = BgSlot;
            bg.type   = Image.Type.Simple;
            _slotBgs[idx] = bg;

            // Borde sutil
            var borderGo = Rect("Border", go.transform, Vector2.zero, Vector2.one,
                                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-2, -2));
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0.35f, 0.28f, 0.15f, 0.5f);

            // Icon
            var iconGo = Rect("Icon", go.transform,
                              new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f),
                              new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var icon = iconGo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.enabled        = false;
            _slotIcons[idx]     = icon;

            // Count
            var cntGo = Rect("Count", go.transform, Vector2.zero, new Vector2(1, 0.4f),
                             new Vector2(0.5f, 0), new Vector2(-3, 3), Vector2.zero);
            var cnt = cntGo.AddComponent<Text>();
            cnt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cnt.fontSize  = 13;
            cnt.fontStyle = FontStyle.Bold;
            cnt.color     = new Color(1f, 0.95f, 0.80f, 1f);
            cnt.alignment = TextAnchor.LowerRight;
            cnt.text      = "";
            var cntSh = cntGo.AddComponent<Shadow>();
            cntSh.effectColor    = new Color(0, 0, 0, 0.95f);
            cntSh.effectDistance = new Vector2(1, -1);
            _slotCounts[idx] = cnt;

            // Toque
            int captured = idx;
            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => OnSlotTapped(captured));
        }

        // ── Lógica ───────────────────────────────────────────────────────────

        public void ToggleFromTopBar() => TogglePanel();

        private void TogglePanel()
        {
            _isOpen = !_isOpen;
            _panel.SetActive(_isOpen);
            if (_isOpen) Refresh();
        }

        private void Refresh()
        {
            if (!_isOpen || _panel == null) return;

            var p = GameState.Instance.LocalPlayer;
            for (int i = 0; i < LocalPlayer.InventorySize; i++)
            {
                var slot = p.Inventory[i];

                if (slot.IdItem == 0)
                {
                    _slotBgs[i].color     = BgSlot;
                    _slotIcons[i].enabled = false;
                    _slotCounts[i].text   = "";
                    continue;
                }

                _slotBgs[i].color   = slot.Equipped ? BgEquipped : BgSlot;
                _slotCounts[i].text = slot.Cantidad > 1 ? slot.Cantidad.ToString() : "";

                int    capturedIdx = i;
                ushort grh         = slot.GrhIndex;

                if (grh > 0)
                {
                    _slotIcons[i].enabled = false; // ocultar hasta que cargue
                    GrhDatabase.Instance.GetSprite(grh, sprite =>
                    {
                        if (_slotIcons[capturedIdx] == null) return;
                        _slotIcons[capturedIdx].sprite  = sprite;
                        _slotIcons[capturedIdx].enabled = sprite != null;
                    });
                }
                else
                {
                    _slotIcons[i].enabled = false;
                }
            }
        }

        private void OnSlotTapped(int idx)
        {
            var slot = GameState.Instance.LocalPlayer.Inventory[idx];
            if (slot.IdItem == 0) return;
            ShowItemPopup(idx, slot);
        }

        // ── Popup de ítem ─────────────────────────────────────────────────────

        private GameObject _popup;

        private void ShowItemPopup(int idx, InventorySlot slot)
        {
            ClosePopup();

            // Fondo oscuro que cierra al tocar fuera
            _popup = Rect("ItemPopup", _panel.transform,
                          Vector2.zero, Vector2.one,
                          new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var blocker = _popup.AddComponent<Image>();
            blocker.color = new Color(0, 0, 0, 0.45f);
            var blkBtn = _popup.AddComponent<Button>();
            blkBtn.transition = Selectable.Transition.None;
            blkBtn.onClick.AddListener(ClosePopup);

            // Panel del popup
            float pw = 320f, ph = 220f;
            var card = Rect("Card", _popup.transform,
                            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                            new Vector2(0.5f, 0.5f), Vector2.zero,
                            new Vector2(pw, ph));
            var cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.08f, 0.06f, 0.03f, 0.98f);
            card.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.85f);

            // Evitar que clicks en la card cierren el popup
            var cardBtn = card.AddComponent<Button>();
            cardBtn.transition = Selectable.Transition.None;
            cardBtn.onClick.AddListener(() => { });

            // Nombre
            var nameGo = Rect("Name", card.transform,
                              new Vector2(0, 1), new Vector2(1, 1),
                              new Vector2(0.5f, 1), new Vector2(0, -10),
                              new Vector2(-20, 30));
            var nameTxt = nameGo.AddComponent<Text>();
            nameTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.text      = slot.Name;
            nameTxt.fontSize  = 20;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.color     = Gold;
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameGo.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);

            // Stats (DataObj)
            float statY = -48f;
            if (!string.IsNullOrEmpty(slot.DataObj))
            {
                var statGo = Rect("Stats", card.transform,
                                  new Vector2(0, 1), new Vector2(1, 1),
                                  new Vector2(0.5f, 1), new Vector2(0, statY),
                                  new Vector2(-20, 26));
                var statTxt = statGo.AddComponent<Text>();
                statTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                statTxt.text      = slot.DataObj;
                statTxt.fontSize  = 15;
                statTxt.color     = new Color(0.85f, 0.85f, 0.75f, 1f);
                statTxt.alignment = TextAnchor.MiddleCenter;
                statY -= 30f;
            }

            // Cantidad y valor
            string infoStr = slot.Cantidad > 1 ? $"Cantidad: {slot.Cantidad}   " : "";
            infoStr += $"Valor: {slot.Valor} oro";
            var infoGo = Rect("Info", card.transform,
                              new Vector2(0, 1), new Vector2(1, 1),
                              new Vector2(0.5f, 1), new Vector2(0, statY),
                              new Vector2(-20, 22));
            var infoTxt = infoGo.AddComponent<Text>();
            infoTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoTxt.text      = infoStr;
            infoTxt.fontSize  = 13;
            infoTxt.color     = new Color(0.70f, 0.65f, 0.50f, 1f);
            infoTxt.alignment = TextAnchor.MiddleCenter;

            // Botones de acción
            float btnY = -ph / 2 + 50f;
            bool isEquippable = slot.ObjType >= 1 && slot.ObjType <= 5;

            if (isEquippable)
            {
                string label = slot.Equipped ? "Desequipar" : "Equipar";
                AddPopupButton(card.transform, label, -84f, btnY, () =>
                {
                    Network.NetworkManager.Instance.Send(
                        Network.PacketSerializer.Instance.EquipItem(slot.IdPos));
                    ClosePopup();
                });
            }
            else
            {
                AddPopupButton(card.transform, "Usar", -84f, btnY, () =>
                {
                    Network.NetworkManager.Instance.Send(
                        Network.PacketSerializer.Instance.UseItem(slot.IdPos));
                    ClosePopup();
                });
            }

            AddPopupButton(card.transform, "Tirar", 84f, btnY, () =>
            {
                ShowDropConfirm(idx, slot);
            });

            // × cerrar
            var xGo = Rect("X", card.transform,
                           new Vector2(1, 1), new Vector2(1, 1),
                           new Vector2(1, 1), new Vector2(-6, -6),
                           new Vector2(32, 32));
            var xImg = xGo.AddComponent<Image>();
            xImg.color = new Color(0.5f, 0.1f, 0.08f, 0.9f);
            var xBtn = xGo.AddComponent<Button>();
            xBtn.transition = Selectable.Transition.None;
            xBtn.onClick.AddListener(ClosePopup);
            var xTxt = Rect("T", xGo.transform, Vector2.zero, Vector2.one,
                            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .AddComponent<Text>();
            xTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            xTxt.text      = "✕";
            xTxt.fontSize  = 18;
            xTxt.color     = Color.white;
            xTxt.alignment = TextAnchor.MiddleCenter;
        }

        private void ShowDropConfirm(int idx, InventorySlot slot)
        {
            ClosePopup();
            if (slot.Cantidad <= 1)
            {
                Network.NetworkManager.Instance.Send(
                    Network.PacketSerializer.Instance.TirarItem(slot.IdPos, 1));
                return;
            }

            // Para stacks: confirmar con cantidad máxima por simplicidad
            // TODO: agregar input de cantidad
            Network.NetworkManager.Instance.Send(
                Network.PacketSerializer.Instance.TirarItem(slot.IdPos, slot.Cantidad));
        }

        private void AddPopupButton(Transform parent, string label, float x, float y, UnityEngine.Events.UnityAction action)
        {
            var go = Rect(label, parent,
                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(0.5f, 0.5f), new Vector2(x, y),
                          new Vector2(140f, 44f));
            var img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.16f, 0.06f, 1f);
            go.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.7f);
            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.35f, 0.26f, 0.10f, 1f);
            colors.pressedColor     = new Color(0.45f, 0.34f, 0.12f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(action);
            var txt = Rect("T", go.transform, Vector2.zero, Vector2.one,
                           new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                .AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text      = label;
            txt.fontSize  = 17;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = Gold;
            txt.alignment = TextAnchor.MiddleCenter;
        }

        private void ClosePopup()
        {
            if (_popup != null) { Destroy(_popup); _popup = null; }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static GameObject Rect(string name, Transform parent,
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

        private static Sprite MakeRounded(int w, int h, float radius, Color color)
        {
            var tex    = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float cx = px < radius ? radius : (px > w - radius ? w - radius : px);
                float cy = py < radius ? radius : (py > h - radius ? h - radius : py);
                float d  = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                float a  = Mathf.Clamp01(radius - d + 0.5f);
                pixels[y * w + x] = new Color(color.r, color.g, color.b, color.a * a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }
    }
}
