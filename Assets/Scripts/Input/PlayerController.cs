using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using ArgentumOnline.Network;
using ArgentumOnline.Game;

namespace ArgentumOnline.Input
{
    /// <summary>
    /// Escucha el joystick virtual y envía packets de Walk al servidor.
    /// El movimiento real se aplica cuando el servidor responde con EntityMove.
    /// También soporta teclado (WASD / flechas) para testear en editor.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Tooltip("Tiempo mínimo entre moves (ms) para no spamear")]
        [SerializeField] private float _moveIntervalSec = 0.15f;

        private float _lastMoveSent = -999f;
        private bool  _moving       = false;
        private VirtualJoystick.JoyDir _heldDir = VirtualJoystick.JoyDir.None;

        private bool _joystickSubscribed = false;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (VirtualJoystick.Instance != null)
                VirtualJoystick.Instance.OnDirectionChanged -= OnJoyDir;
        }

        // ── Joystick ──────────────────────────────────────────────────────────

        private void OnJoyDir(VirtualJoystick.JoyDir dir)
        {
            _heldDir = dir;

            if (dir == VirtualJoystick.JoyDir.None)
            {
                _moving = false;
            }
            else
            {
                if (!_moving)
                {
                    _moving = true;
                    StartCoroutine(MoveLoop());
                }
            }
        }

        private IEnumerator MoveLoop()
        {
            while (_moving && _heldDir != VirtualJoystick.JoyDir.None)
            {
                SendMove(_heldDir);
                yield return new WaitForSeconds(_moveIntervalSec);
            }
            _moving = false;
        }

        // ── Teclado (editor / debug) ──────────────────────────────────────────

        void Update()
        {
            // Suscribirse al joystick cuando esté disponible (se crea en runtime)
            if (!_joystickSubscribed && VirtualJoystick.Instance != null)
            {
                VirtualJoystick.Instance.OnDirectionChanged += OnJoyDir;
                _joystickSubscribed = true;
            }

            // Click/tap: siempre activo (independiente del joystick)
            HandleClick();

            // Solo en editor o si no hay joystick
            if (VirtualJoystick.Instance != null &&
                VirtualJoystick.Instance.CurrentDir != VirtualJoystick.JoyDir.None)
                return;

            if (Time.time - _lastMoveSent < _moveIntervalSec) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                SendMove(VirtualJoystick.JoyDir.Up);
            else if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                SendMove(VirtualJoystick.JoyDir.Down);
            else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                SendMove(VirtualJoystick.JoyDir.Left);
            else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                SendMove(VirtualJoystick.JoyDir.Right);
        }

        // ── Click / tap para interactuar ──────────────────────────────────────

        private void HandleClick()
        {
            // Click izquierdo → interacción (NPC, puertas)
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                var screenPos = Mouse.current.position.ReadValue();
                if (!IsBlockedByUI(screenPos)) SendClickAt(screenPos);
            }

            // Click derecho → teletransporte al tile
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                var screenPos = Mouse.current.position.ReadValue();
                if (!IsBlockedByUI(screenPos)) SendMoveToAt(screenPos);
            }

            // Touch (móvil) → interacción
            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                var screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                if (!IsBlockedByUI(screenPos)) SendClickAt(screenPos);
            }
        }

        /// <summary>
        /// Devuelve true solo si el punto cae sobre un elemento de UI interactivo
        /// (botón, input, scroll) o sobre un canvas de UI "real" (sortingOrder > 10).
        /// Ignora el JoystickPanel (sortingOrder=10, solo Image de fondo).
        /// </summary>
        private static bool IsBlockedByUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            var results  = new System.Collections.Generic.List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var r in results)
            {
                var go = r.gameObject;
                if (go.GetComponent<Button>()     != null) return true;
                if (go.GetComponent<InputField>() != null) return true;
                if (go.GetComponent<ScrollRect>() != null) return true;
                var canvas = go.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.sortingOrder > 10) return true;
            }
            return false;
        }

        private void SendMoveToAt(Vector2 screenPos)
        {
            if (!NetworkManager.Instance.IsConnected) return;

            var world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            var p  = GameState.Instance.LocalPlayer;
            int dx = Mathf.RoundToInt(world.x);
            int dy = Mathf.RoundToInt(-world.y);
            int absX = Mathf.Clamp(p.PosX + dx, 1, 100);
            int absY = Mathf.Clamp(p.PosY + dy, 1, 100);

            NetworkManager.Instance.Send(PacketSerializer.Instance.MoveTo((byte)absX, (byte)absY));
        }

        private void SendClickAt(Vector2 screenPos)
        {
            if (!NetworkManager.Instance.IsConnected) return;

            // Convertir posición de pantalla → tile absoluto en el mapa
            var world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            var p  = GameState.Instance.LocalPlayer;
            int dx = Mathf.RoundToInt(world.x);
            int dy = Mathf.RoundToInt(-world.y);
            int absX = Mathf.Clamp(p.PosX + dx, 1, 100);
            int absY = Mathf.Clamp(p.PosY + dy, 1, 100);

            // Si hay hechizo seleccionado, lanzarlo en lugar del click normal
            var spellsUI = ArgentumOnline.UI.SpellsUI.Instance;
            if (spellsUI != null && spellsUI.SelectedSpellSlot >= 0)
            {
                NetworkManager.Instance.Send(
                    PacketSerializer.Instance.AttackSpell(
                        (byte)spellsUI.SelectedSpellSlot,
                        (byte)absX, (byte)absY));
                spellsUI.ClearSelection();
                return;
            }

            NetworkManager.Instance.Send(PacketSerializer.Instance.Click((byte)absX, (byte)absY));
        }

        // ── Envío al servidor ─────────────────────────────────────────────────

        private void SendMove(VirtualJoystick.JoyDir dir)
        {
            if (!NetworkManager.Instance.IsConnected) return;

            _lastMoveSent = Time.time;

            var heading = dir switch
            {
                VirtualJoystick.JoyDir.Up    => Heading.Up,
                VirtualJoystick.JoyDir.Down  => Heading.Down,
                VirtualJoystick.JoyDir.Right => Heading.Right,
                VirtualJoystick.JoyDir.Left  => Heading.Left,
                _                            => Heading.Down
            };

            // Predicción client-side: mover inmediatamente sin esperar al servidor
            var p = GameState.Instance.LocalPlayer;
            p.Heading = (byte)heading;
            switch (heading)
            {
                case Heading.Up:    p.PosY--; break;
                case Heading.Down:  p.PosY++; break;
                case Heading.Right: p.PosX++; break;
                case Heading.Left:  p.PosX--; break;
            }
            p.NotifyPositionChanged();

            NetworkManager.Instance.Send(PacketSerializer.Instance.Position(heading));
        }
    }
}
