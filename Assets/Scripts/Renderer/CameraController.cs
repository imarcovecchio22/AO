using UnityEngine;
using ArgentumOnline.Game;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Sigue al jugador local. La cámara siempre está centrada en el MapRenderer.
    /// El MapRenderer renderiza el viewport relativo a su posición (dx, dy desde centro),
    /// así que la cámara solo necesita apuntar al centro del MapRenderer (0,0 local).
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [SerializeField] private Camera _cam;

        void Awake()
        {
            Instance = this;
            if (_cam == null) _cam = Camera.main;
        }

        void Start()
        {
            _cam.transform.position = new Vector3(0, 0, -10);

            _cam.orthographicSize = 10f;
        }
    }
}
