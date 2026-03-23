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
        private const int   MaxLines   = 8;
        private const float LineH      = 18f;
        private const float PanelW     = 380f;
        private const float InputH     = 22f;
        private const float MsgAreaH   = MaxLines * LineH;
        private const float PanelH     = MsgAreaH + InputH + 10f;
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
                if (_inputRow.activeSelf && _inputField.isFocused)
                    TrySend();
                else
                    OpenInput();
            }
            if (kb.escapeKey.wasPressedThisFrame && _inputRow.activeSelf)
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

            // Panel principal — esquina superior derecha, debajo del HUD de vida
            // HUD ocupa 70px + 18px de margen = 88px desde arriba
            var panel     = Go("Panel", canvasGo.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin        = new Vector2(1, 1);
            panelRect.anchorMax        = new Vector2(1, 1);
            panelRect.pivot            = new Vector2(1, 1);
            panelRect.anchoredPosition = new Vector2(-18, -96f);
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
                txt.fontSize           = 12;
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
            inputTextGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            inputTextGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            var inputTxt = inputTextGo.AddComponent<Text>();
            inputTxt.font            = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputTxt.fontSize        = 12;
            inputTxt.color           = Color.white;
            inputTxt.supportRichText = false;
            _inputField.textComponent = inputTxt;

            var phGo   = Go("Placeholder", inputGo.transform);
            phGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            phGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            var ph = phGo.AddComponent<Text>();
            ph.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ph.fontSize  = 12;
            ph.fontStyle = FontStyle.Italic;
            ph.color     = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            ph.text      = "Enter para chatear...";
            _inputField.placeholder = ph;

            _inputRow.SetActive(false);
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
            _inputRow.SetActive(true);
            _inputField.ActivateInputField();
        }

        private void CloseInput()
        {
            _inputField.text = "";
            _inputRow.SetActive(false);
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
            if (string.IsNullOrEmpty(text) || !NetworkManager.Instance.IsConnected) return;
            NetworkManager.Instance.Send(PacketSerializer.Instance.Dialog(text));
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
