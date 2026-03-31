using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using ArgentumOnline.Game;
using ArgentumOnline.Network;

namespace ArgentumOnline.UI
{
    /// <summary>
    /// Consola de chat — lado izquierdo de la pantalla, centrada verticalmente.
    /// Pool fijo de Text objects posicionados manualmente de abajo hacia arriba.
    /// Enter abre/envía, Escape cierra.
    /// </summary>
    public class ConsoleUI : MonoBehaviour
    {
        private const int   MaxLines   = 3;
        private const float LineH      = 16f;
        private const float PanelW     = 624f;  // entre HUD (x=256+8) y minimap (x=896-8)
        private const float InputH     = 22f;
        private const float MsgAreaH   = MaxLines * LineH;
        private const float PanelH     = MsgAreaH + InputH + 8f;
        private const float PadX       = 8f;

        // Líneas de texto circulares (índice 0 = más antigua visible)
        private Text[]     _lines;
        private int        _head;       // índice de la línea más antigua

        private GameObject _inputRow;
        private InputField _inputField;
        private RectTransform _msgArea;

        void Start()
        {
            BuildConsole();
            GameState.Instance.OnConsoleMessage += OnMessage;
            GameState.Instance.OnConnected      += OnConnected;
        }

        private void OnConnected()
        {
            OnMessage("Conectado al servidor. Escribí /online para ver jugadores.", "#88ccff");
        }

        void OnDestroy()
        {
            GameState.Instance.OnConsoleMessage -= OnMessage;
            GameState.Instance.OnConnected      -= OnConnected;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                if (_inputField.isFocused)
                    TrySend();
                else
                    OpenInput();
            }
            if (kb.escapeKey.wasPressedThisFrame && _inputField.isFocused)
                CloseInput();
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void BuildConsole()
        {
            var canvasGo = new GameObject("ConsoleCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);

            // Panel principal — entre HUD (right=264) y minimap (left=888), al tope de pantalla
            // HUD: anchoredPos(16,-16) + sizeDelta(240,114) → right edge x=256, gap 8 → start x=264
            // Minimap: anchoredPos(-16,-16) desde right, ancho=168 → left edge x=896, gap 8 → end x=888
            var panel     = Go("Panel", canvasGo.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin        = new Vector2(0, 1);
            panelRect.anchorMax        = new Vector2(0, 1);
            panelRect.pivot            = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(264, -16f);
            panelRect.sizeDelta        = new Vector2(PanelW, PanelH);

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.08f, 0.70f);
            var sh = panel.AddComponent<Shadow>();
            sh.effectColor    = new Color(0, 0, 0, 0.5f);
            sh.effectDistance = new Vector2(3, -3);

            // Área de mensajes (top, deja espacio para input abajo)
            var msgGo   = Go("MsgArea", panel.transform);
            _msgArea    = msgGo.GetComponent<RectTransform>();
            _msgArea.anchorMin        = new Vector2(0, 0);
            _msgArea.anchorMax        = new Vector2(1, 1);
            _msgArea.offsetMin        = new Vector2(0, InputH + 6f);
            _msgArea.offsetMax        = Vector2.zero;

            // Máscara de recorte
            var maskImg = msgGo.AddComponent<Image>();
            maskImg.color = Color.clear;
            msgGo.AddComponent<Mask>().showMaskGraphic = false;

            // Crear líneas de texto posicionadas de abajo a arriba
            _lines = new Text[MaxLines];
            for (int i = 0; i < MaxLines; i++)
            {
                var lineGo   = Go($"Line{i}", msgGo.transform);
                var lineRect = lineGo.GetComponent<RectTransform>();
                // Ancla al borde inferior izquierdo del msgArea
                lineRect.anchorMin        = new Vector2(0, 0);
                lineRect.anchorMax        = new Vector2(1, 0);
                lineRect.pivot            = new Vector2(0, 0);
                lineRect.anchoredPosition = new Vector2(PadX, i * LineH + 2f);
                lineRect.sizeDelta        = new Vector2(-PadX * 2, LineH);

                var txt = lineGo.AddComponent<Text>();
                txt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize           = 11;
                txt.color              = Color.clear;
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.verticalOverflow   = VerticalWrapMode.Truncate;
                txt.supportRichText    = false;

                var shadow = lineGo.AddComponent<Shadow>();
                shadow.effectColor    = new Color(0, 0, 0, 0.8f);
                shadow.effectDistance = new Vector2(1, -1);

                _lines[i] = txt;
            }
            _head = 0;

            // Fila de input
            _inputRow = Go("InputRow", panel.transform);
            var inputRowRect = _inputRow.GetComponent<RectTransform>();
            inputRowRect.anchorMin        = new Vector2(0, 0);
            inputRowRect.anchorMax        = new Vector2(1, 0);
            inputRowRect.pivot            = new Vector2(0, 0);
            inputRowRect.anchoredPosition = new Vector2(4, 3);
            inputRowRect.sizeDelta        = new Vector2(-8, InputH);

            _inputRow.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.16f, 0.95f);

            var inputGo   = Go("Input", _inputRow.transform);
            var inputRect = inputGo.GetComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.one;
            inputRect.offsetMin = new Vector2(6, 1);
            inputRect.offsetMax = new Vector2(-6, -1);

            _inputField = inputGo.AddComponent<InputField>();
            _inputField.onEndEdit.AddListener(OnEndEdit);

            var inputTextGo   = Go("Text", inputGo.transform);
            var inputTextRect = inputTextGo.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = new Vector2(4, 0);
            inputTextRect.offsetMax = Vector2.zero;
            var inputTxt = inputTextGo.AddComponent<Text>();
            inputTxt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputTxt.fontSize           = 12;
            inputTxt.color              = Color.white;
            inputTxt.supportRichText    = false;
            inputTxt.alignment          = TextAnchor.MiddleLeft;
            inputTxt.verticalOverflow   = VerticalWrapMode.Truncate;
            _inputField.textComponent   = inputTxt;

            var phGo   = Go("Placeholder", inputGo.transform);
            var phRect = phGo.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(4, 0);
            phRect.offsetMax = Vector2.zero;
            var ph = phGo.AddComponent<Text>();
            ph.font                = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ph.fontSize            = 12;
            ph.fontStyle           = FontStyle.Italic;
            ph.color               = new Color(0.6f, 0.6f, 0.6f, 0.9f);
            ph.text                = "Enter para chatear...";
            ph.alignment           = TextAnchor.MiddleLeft;
            ph.verticalOverflow    = VerticalWrapMode.Truncate;
            ph.horizontalOverflow  = HorizontalWrapMode.Overflow;
            _inputField.placeholder = ph;

            // La barra de input siempre visible como hint; se activa al presionar Enter
        }

        // ── Mensajes ──────────────────────────────────────────────────────────

        private void OnMessage(string msg, string hex)
        {
            Debug.Log($"[Console] OnMessage: {msg}");

            // Desplazar todas las líneas hacia arriba (línea 0 = fondo/más nueva)
            for (int i = MaxLines - 1; i > 0; i--)
            {
                _lines[i].text  = _lines[i - 1].text;
                _lines[i].color = _lines[i - 1].color;
            }

            // Nuevo mensaje en la línea del fondo (índice 0)
            Color color = new Color(0.88f, 0.88f, 0.88f, 1f);
            if (!string.IsNullOrEmpty(hex))
                ColorUtility.TryParseHtmlString(hex, out color);

            _lines[0].text  = msg;
            _lines[0].color = color;
        }

        // ── Input ─────────────────────────────────────────────────────────────

        private void OpenInput()
        {
            _inputField.ActivateInputField();
        }

        private void CloseInput()
        {
            _inputField.text = "";
            _inputField.DeactivateInputField();
        }

        private void OnEndEdit(string text)
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                TrySend();
        }

        private void TrySend()
        {
            string text = _inputField.text.Trim();
            CloseInput();
            if (string.IsNullOrEmpty(text)) return;

            if (TryHandleCommand(text)) return;

            if (!NetworkManager.Instance.IsConnected) return;
            NetworkManager.Instance.Send(PacketSerializer.Instance.Dialog(text));
        }

        private bool TryHandleCommand(string text)
        {
            if (!text.StartsWith("/")) return false;

            string cmd = text.ToLowerInvariant();

            if (cmd == "/meditar")
            {
                var lp = GameState.Instance.LocalPlayer;
                if (lp == null) return true;
                bool next = !lp.Meditando;
                lp.SetMeditando(next);
                if (NetworkManager.Instance.IsConnected)
                    NetworkManager.Instance.Send(PacketSerializer.Instance.Meditar());
                OnMessage(next ? "Comenzaste a meditar." : "Dejaste de meditar.", "#88aaff");
                return true;
            }

            if (cmd == "/online")
            {
                if (NetworkManager.Instance.IsConnected)
                    NetworkManager.Instance.Send(PacketSerializer.Instance.Dialog(text));
                return true;
            }

            OnMessage($"Comando desconocido: {text}", "#ff6666");
            return true;
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static GameObject Go(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }
    }
}
