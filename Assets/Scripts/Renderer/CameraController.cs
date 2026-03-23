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
            _cam.clearFlags         = CameraClearFlags.SolidColor;
            _cam.backgroundColor    = Color.black;

            FitCameraToViewport();
        }

        /// <summary>
        /// Ajusta orthographicSize para que el viewport de tiles llene toda la pantalla.
        /// El lado más largo de la pantalla determina cuántos tiles se necesitan.
        /// </summary>
        private void FitCameraToViewport()
        {
            int   radius = MapRenderer.Instance != null ? MapRenderer.Instance.ViewportRadius : 9;
            float aspect = (float)Screen.width / Screen.height;

            // Necesitamos que 'radius' tiles lleguen hasta el borde en la dirección más ancha.
            // orthographicSize = half-height en world units.
            // half-width = orthographicSize * aspect
            // Queremos max(half-height, half-width) <= radius + 0.5  →  tiles llenen pantalla.
            // En portrait (aspect < 1) la altura manda: orthographicSize = radius + 0.5
            // En landscape (aspect > 1) el ancho manda: orthographicSize = (radius + 0.5) / aspect
            _cam.orthographicSize = (radius + 0.5f) / Mathf.Max(1f, aspect);
        }
    }
}
