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

        // Tiles visibles (half-height en world units = tiles desde centro hasta borde).
        // 6 → ~12 tiles verticales en portrait, ~21 horizontales en landscape 16:9.
        // 4 → ~8 tiles en portrait,  ~14 en landscape. Bajar para acercar la cámara.
        // Valor recomendado para móvil portrait: 4–5. Para desktop landscape: 5–6.
        private const float DisplayHalfTiles = 5f;

        /// <summary>
        /// Ajusta orthographicSize para mostrar exactamente DisplayHalfTiles tiles
        /// en la dirección vertical (portrait) o equivalente en landscape.
        /// El mapa sigue renderizando viewportRadius tiles para no mostrar bordes vacíos.
        /// </summary>
        private void FitCameraToViewport()
        {
            float aspect = (float)Screen.width / Screen.height;

            // En landscape (aspect > 1) escalar por el aspecto para que los tiles sigan siendo cuadrados
            // y se mantenga la misma densidad visual que en portrait.
            _cam.orthographicSize = DisplayHalfTiles / Mathf.Max(1f, aspect);
        }
    }
}
