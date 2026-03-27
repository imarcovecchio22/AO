using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArgentumOnline.Network;

namespace ArgentumOnline.UI
{
    /// <summary>
    /// Botón circular de ataque melé (abajo-derecha).
    /// Envía attackMelee al servidor con cooldown de 1 segundo.
    /// </summary>
    public class MeleeButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public static MeleeButton Instance { get; private set; }

        private Image  _icon;
        private float  _lastAttack = -999f;
        private const float Cooldown = 1.0f;
        private const float BtnSize  = 90f;

        private static readonly Color ColNormal  = new Color(0.10f, 0.10f, 0.10f, 0.72f);
        private static readonly Color ColPressed = new Color(0.55f, 0.20f, 0.10f, 0.90f);
        private static readonly Color ColReady   = new Color(0.18f, 0.08f, 0.05f, 0.72f);

        void Awake() => Instance = this;

        void Start() => BuildButton();

        private void BuildButton()
        {
            var canvasGo = new GameObject("MeleeCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGo);

            // Botón: abajo-derecha, encima del suelo de pantalla
            var btnGo = new GameObject("MeleeBtn");
            btnGo.transform.SetParent(canvasGo.transform, false);
            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-24f, 80f);
            rt.sizeDelta        = new Vector2(BtnSize, BtnSize);

            _icon = btnGo.AddComponent<Image>();
            _icon.sprite = MakeCircle(128, ColNormal);
            _icon.type   = Image.Type.Simple;

            var shadow = btnGo.AddComponent<Shadow>();
            shadow.effectColor    = new Color(0, 0, 0, 0.7f);
            shadow.effectDistance = new Vector2(3, -3);

            // Registrar eventos de toque/click directamente en este MonoBehaviour
            btnGo.AddComponent<MeleeButtonProxy>().Owner = this;

            // Ícono ⚔
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(btnGo.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            var txt = iconGo.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text      = "⚔";
            txt.fontSize  = 36;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = new Color(0.90f, 0.75f, 0.55f, 1f);
            txt.alignment = TextAnchor.MiddleCenter;
            var sh = iconGo.AddComponent<Shadow>();
            sh.effectColor    = new Color(0, 0, 0, 0.9f);
            sh.effectDistance = new Vector2(1, -1);
        }

        public void OnPointerDown(PointerEventData _)
        {
            TryAttack();
            _icon.sprite = MakeCircle(128, ColPressed);
        }

        public void OnPointerUp(PointerEventData _)
        {
            _icon.sprite = MakeCircle(128, ColNormal);
        }

        private void TryAttack()
        {
            if (!NetworkManager.Instance.IsConnected) return;
            if (Time.time - _lastAttack < Cooldown) return;

            _lastAttack = Time.time;
            NetworkManager.Instance.Send(PacketSerializer.Instance.AttackMelee());
        }

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
    }

    /// <summary>
    /// Proxy para recibir IPointerDownHandler/IPointerUpHandler desde el GameObject del botón.
    /// </summary>
    internal class MeleeButtonProxy : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public MeleeButton Owner;
        public void OnPointerDown(PointerEventData e) => Owner?.OnPointerDown(e);
        public void OnPointerUp(PointerEventData e)   => Owner?.OnPointerUp(e);
    }
}
