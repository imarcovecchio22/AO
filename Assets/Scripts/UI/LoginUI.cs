using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using ArgentumOnline.Network;

namespace ArgentumOnline.UI
{
    /// <summary>
    /// Pantalla de login y selección de personaje.
    /// Se crea en runtime y llama a ApiClient para autenticar.
    /// Dispara OnCharacterSelected cuando el usuario elige personaje.
    /// </summary>
    public class LoginUI : MonoBehaviour
    {
        public static LoginUI Instance { get; private set; }

        public event System.Action<string, string, string> OnCharacterSelected;
        // accountId, characterId, email

        // ── Colores ───────────────────────────────────────────────────────────
        private static readonly Color BgColor     = new(0.06f, 0.06f, 0.10f, 0.97f);
        private static readonly Color PanelColor  = new(0.09f, 0.09f, 0.14f, 1.00f);
        private static readonly Color InputColor  = new(0.13f, 0.13f, 0.18f, 1.00f);
        private static readonly Color BtnGreen    = new(0.18f, 0.65f, 0.30f, 1.00f);
        private static readonly Color BtnBlue     = new(0.18f, 0.40f, 0.80f, 1.00f);
        private static readonly Color ErrorColor  = new(1.00f, 0.35f, 0.35f, 1.00f);
        private static readonly Color TextLight   = new(0.90f, 0.90f, 0.90f, 1.00f);
        private static readonly Color TextMuted   = new(0.55f, 0.55f, 0.60f, 1.00f);

        // ── Refs ──────────────────────────────────────────────────────────────
        private Canvas     _canvas;
        private GameObject _loginPanel;
        private GameObject _charPanel;
        private InputField _userField;
        private InputField _passField;
        private Text       _errorText;
        private Button     _loginBtn;
        private Transform  _charList;
        private Text       _charErrorText;

        private string _accountId;
        private string _email;

        void Awake()
        {
            Instance = this;
            Build();
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void Build()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            var go     = new GameObject("LoginCanvas");
            _canvas    = go.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            go.AddComponent<GraphicRaycaster>();

            // Fondo completo oscuro
            var bg     = Rect("BG", go.transform);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.04f, 1f);

            _loginPanel = BuildLoginPanel(go.transform);
            _charPanel  = BuildCharPanel(go.transform);
            _charPanel.SetActive(false);
        }

        // ── Panel de Login ────────────────────────────────────────────────────

        private GameObject BuildLoginPanel(Transform parent)
        {
            var panel     = Rect("LoginPanel", parent);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRect.pivot            = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta        = new Vector2(360, 360);

            panel.AddComponent<Image>().color = PanelColor;
            var sh = panel.AddComponent<Shadow>();
            sh.effectColor    = new Color(0, 0, 0, 0.6f);
            sh.effectDistance = new Vector2(4, -4);

            float y = 140f;

            // Título
            var title = MakeText("Title", panel.transform, "Argentum Online", 28, FontStyle.Bold, TextLight);
            Place(title, new Vector2(0, y), new Vector2(320, 40));
            title.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            y -= 50f;
            MakeText("SubLabel", panel.transform, "Iniciá sesión para jugar", 14, FontStyle.Normal, TextMuted)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, y);
            y -= 50f;

            // Campo usuario
            MakeLabel(panel.transform, "Usuario", new Vector2(-50f, y));
            y -= 28f;
            _userField = MakeInput(panel.transform, "usuario", false, new Vector2(0, y), new Vector2(320, 36));
            y -= 50f;

            // Campo contraseña
            MakeLabel(panel.transform, "Contraseña", new Vector2(-80f, y));
            y -= 28f;
            _passField = MakeInput(panel.transform, "contraseña", true, new Vector2(0, y), new Vector2(320, 36));
            y -= 50f;

            // Error
            var errGo = MakeText("Error", panel.transform, "", 12, FontStyle.Normal, ErrorColor);
            Place(errGo, new Vector2(0, y), new Vector2(320, 28));
            errGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            _errorText = errGo.GetComponent<Text>();
            y -= 44f;

            // Botón login
            _loginBtn = MakeButton("LoginBtn", panel.transform, "Ingresar", BtnGreen,
                new Vector2(0, y), new Vector2(320, 44));
            _loginBtn.onClick.AddListener(OnLoginClicked);

            return panel;
        }

        // ── Panel de Personajes ───────────────────────────────────────────────

        private GameObject BuildCharPanel(Transform parent)
        {
            var panel     = Rect("CharPanel", parent);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRect.pivot            = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta        = new Vector2(380, 480);

            panel.AddComponent<Image>().color = PanelColor;
            var sh = panel.AddComponent<Shadow>();
            sh.effectColor    = new Color(0, 0, 0, 0.6f);
            sh.effectDistance = new Vector2(4, -4);

            var title = MakeText("Title", panel.transform, "Seleccionar personaje", 22, FontStyle.Bold, TextLight);
            Place(title, new Vector2(0, 200f), new Vector2(340, 36));
            title.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            // Error de carga
            var errGo = MakeText("CharError", panel.transform, "", 12, FontStyle.Normal, ErrorColor);
            Place(errGo, new Vector2(0, 165f), new Vector2(340, 24));
            errGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            _charErrorText = errGo.GetComponent<Text>();

            // Contenedor de la lista
            var listGo   = Rect("CharList", panel.transform);
            var listRect = listGo.GetComponent<RectTransform>();
            listRect.anchorMin        = new Vector2(0.5f, 0.5f);
            listRect.anchorMax        = new Vector2(0.5f, 0.5f);
            listRect.pivot            = new Vector2(0.5f, 0.5f);
            listRect.anchoredPosition = new Vector2(0, -10f);
            listRect.sizeDelta        = new Vector2(340, 300);
            var vlg = listGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing               = 10f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight= false;
            vlg.childAlignment        = TextAnchor.UpperCenter;
            listGo.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            _charList = listGo.transform;

            // Botón volver
            var backBtn = MakeButton("BackBtn", panel.transform, "← Volver", BtnBlue,
                new Vector2(0, -210f), new Vector2(160, 38));
            backBtn.onClick.AddListener(() =>
            {
                _charPanel.SetActive(false);
                _loginPanel.SetActive(true);
            });

            return panel;
        }

        // ── Lógica ────────────────────────────────────────────────────────────

        private void OnLoginClicked()
        {
            string user = _userField.text.Trim();
            string pass = _passField.text;
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                _errorText.text = "Completá usuario y contraseña.";
                return;
            }
            _errorText.text = "";
            _loginBtn.interactable = false;
            SetButtonLabel(_loginBtn, "Ingresando...");
            StartCoroutine(DoLogin(user, pass));
        }

        private IEnumerator DoLogin(string user, string pass)
        {
            yield return StartCoroutine(ApiClient.Instance.Login(user, pass, result =>
            {
                _loginBtn.interactable = true;
                SetButtonLabel(_loginBtn, "Ingresar");

                if (!result.Success)
                {
                    _errorText.text = result.Error ?? "Error al conectar";
                    return;
                }
                _accountId = result.AccountId;
                _email     = result.Email;
            }));

            if (string.IsNullOrEmpty(_accountId)) yield break;

            // Pedir personajes
            yield return StartCoroutine(ApiClient.Instance.GetCharacters(chars =>
            {
                ShowCharacters(chars);
            }));
        }

        private void ShowCharacters(ApiClient.CharacterInfo[] chars)
        {
            // Limpiar lista
            foreach (Transform t in _charList)
                Destroy(t.gameObject);

            _loginPanel.SetActive(false);
            _charPanel.SetActive(true);

            if (chars == null || chars.Length == 0)
            {
                _charErrorText.text = "No tenés personajes creados.";
                return;
            }

            _charErrorText.text = "";

            foreach (var ch in chars)
            {
                var charId   = ch.Id;
                var charName = ch.Name;

                var row     = Rect($"Char_{charName}", _charList);
                var rowLE   = row.AddComponent<LayoutElement>();
                rowLE.preferredHeight = 60f;
                row.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.18f, 1f);

                // Nombre
                var nameGo = MakeText("Name", row.transform, charName, 16, FontStyle.Bold, TextLight);
                Place(nameGo, new Vector2(-50f, 0), new Vector2(200, 60));
                nameGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

                // Botón jugar
                var btn = MakeButton("PlayBtn", row.transform, "Jugar", BtnGreen,
                    new Vector2(120f, 0), new Vector2(100, 40));
                btn.onClick.AddListener(() =>
                {
                    _canvas.gameObject.SetActive(false);
                    OnCharacterSelected?.Invoke(_accountId, charId, _email);
                });
            }
        }

        // ── Helpers de UI ─────────────────────────────────────────────────────

        private static GameObject Rect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void Place(GameObject go, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
        }

        private static GameObject MakeText(string name, Transform parent, string text,
            int size, FontStyle style, Color color)
        {
            var go = Rect(name, parent);
            var t  = go.AddComponent<Text>();
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text      = text;
            t.fontSize  = size;
            t.fontStyle = style;
            t.color     = color;
            t.alignment = TextAnchor.MiddleLeft;
            return go;
        }

        private static void MakeLabel(Transform parent, string text, Vector2 pos)
        {
            var go = MakeText("Label_" + text, parent, text, 12, FontStyle.Normal, TextMuted);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(200, 20);
        }

        private static InputField MakeInput(Transform parent, string placeholder,
            bool password, Vector2 pos, Vector2 size)
        {
            var go = Rect("InputGo", parent);
            Place(go, pos, size);
            go.AddComponent<Image>().color = InputColor;
            var field = go.AddComponent<InputField>();
            if (password) field.contentType = InputField.ContentType.Password;

            var textGo = Rect("Text", go.transform);
            textGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            textGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            textGo.GetComponent<RectTransform>().offsetMin = new Vector2(8, 0);
            textGo.GetComponent<RectTransform>().offsetMax = new Vector2(-8, 0);
            var txt = textGo.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 14;
            txt.color     = TextLight;
            txt.supportRichText = false;
            field.textComponent = txt;

            var phGo = Rect("PH", go.transform);
            phGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            phGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            phGo.GetComponent<RectTransform>().offsetMin = new Vector2(8, 0);
            phGo.GetComponent<RectTransform>().offsetMax = new Vector2(-8, 0);
            var ph = phGo.AddComponent<Text>();
            ph.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ph.fontSize  = 14;
            ph.fontStyle = FontStyle.Italic;
            ph.color     = TextMuted;
            ph.text      = placeholder;
            field.placeholder = ph;

            return field;
        }

        private static Button MakeButton(string name, Transform parent, string label,
            Color color, Vector2 pos, Vector2 size)
        {
            var go = Rect(name, parent);
            Place(go, pos, size);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();

            var colors = btn.colors;
            colors.highlightedColor = new Color(
                Mathf.Min(color.r + 0.15f, 1),
                Mathf.Min(color.g + 0.15f, 1),
                Mathf.Min(color.b + 0.15f, 1));
            colors.pressedColor = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f);
            btn.colors = colors;

            var txtGo = Rect("Label", go.transform);
            txtGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            txtGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            var txt = txtGo.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text      = label;
            txt.fontSize  = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            return btn;
        }

        private static void SetButtonLabel(Button btn, string label)
        {
            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null) txt.text = label;
        }
    }
}
