using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ArgentumOnline.Input
{
    /// <summary>
    /// Crea en runtime el canvas con el virtual joystick.
    /// Agregar este script al mismo GameObject que PlayerController.
    /// </summary>
    public class JoystickUI : MonoBehaviour
    {
        [SerializeField] private float bgRadius    = 70f;
        [SerializeField] private float handleRadius = 35f;

        void Start()
        {
            // EventSystem (necesario para UI)
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
            }

            // Canvas
            var canvasGo = new GameObject("JoystickCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Panel invisible para capturar input en la mitad izquierda
            var panelGo = new GameObject("JoystickPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelImg  = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.01f);  // casi transparente pero recibe raycast
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0.5f, 1);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Background del joystick (círculo gris)
            var bgGo   = new GameObject("JoyBackground");
            bgGo.transform.SetParent(panelGo.transform, false);
            var bgImg  = bgGo.AddComponent<Image>();
            bgImg.color = new Color(1, 1, 1, 0.15f);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot     = new Vector2(0.5f, 0.5f);
            bgRect.anchoredPosition = new Vector2(0, -120f);  // parte inferior izquierda
            bgRect.sizeDelta = new Vector2(bgRadius * 2, bgRadius * 2);

            // Handle (círculo blanco)
            var handleGo   = new GameObject("JoyHandle");
            handleGo.transform.SetParent(bgGo.transform, false);
            var handleImg  = handleGo.AddComponent<Image>();
            handleImg.color = new Color(1, 1, 1, 0.5f);
            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot     = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(handleRadius * 2, handleRadius * 2);

            // VirtualJoystick en el panel (captura eventos de toda el área)
            var joy = panelGo.AddComponent<VirtualJoystick>();
            joy.Init(bgRect, handleRect);

            DontDestroyOnLoad(canvasGo);
        }
    }
}
