using UnityEngine;
using UnityEngine.EventSystems;

namespace ArgentumOnline.Input
{
    /// <summary>
    /// Joystick virtual para mobile. Adjuntar al panel del joystick en el Canvas.
    /// Emite la dirección dominante (Up/Down/Left/Right) o None cuando está en reposo.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static VirtualJoystick Instance { get; private set; }

        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private float _deadZone = 0.3f;   // fracción del radio

        public enum JoyDir { None, Up, Down, Left, Right }

        public JoyDir CurrentDir { get; private set; } = JoyDir.None;

        // Evento que se dispara cuando la dirección cambia
        public event System.Action<JoyDir> OnDirectionChanged;

        private Vector2 _origin;
        private float   _radius;
        private int     _pointerId = -1;

        void Awake()
        {
            Instance = this;
            if (_background != null)
                _radius = _background.sizeDelta.x * 0.5f;
        }

        /// <summary>Llamar desde JoystickUI después de asignar background y handle en runtime.</summary>
        public void Init(RectTransform background, RectTransform handle)
        {
            _background = background;
            _handle     = handle;
            _radius     = _background.sizeDelta.x * 0.5f;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_pointerId != -1) return;
            _pointerId = e.pointerId;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, e.position, e.pressEventCamera, out _origin);
            UpdateHandle(e.position, e.pressEventCamera);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            UpdateHandle(e.position, e.pressEventCamera);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            _pointerId = -1;
            _handle.localPosition = Vector2.zero;
            SetDir(JoyDir.None);
        }

        private void UpdateHandle(Vector2 screenPos, Camera cam)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, screenPos, cam, out var local);

            Vector2 delta   = local - _origin;
            Vector2 clamped = delta / _radius;
            if (clamped.magnitude > 1f) clamped = clamped.normalized;

            _handle.localPosition = clamped * _radius;

            if (clamped.magnitude < _deadZone)
            {
                SetDir(JoyDir.None);
                return;
            }

            // Dirección dominante
            if (Mathf.Abs(clamped.x) >= Mathf.Abs(clamped.y))
                SetDir(clamped.x > 0 ? JoyDir.Right : JoyDir.Left);
            else
                SetDir(clamped.y > 0 ? JoyDir.Up : JoyDir.Down);
        }

        private void SetDir(JoyDir dir)
        {
            if (dir == CurrentDir) return;
            CurrentDir = dir;
            OnDirectionChanged?.Invoke(dir);
        }
    }
}
