using UnityEngine;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Controla el zoom ortográfico de la cámara principal.
    /// orthographicSize = 4  →  8 tiles visible verticalmente (portrait)
    ///                          ~14 tiles en landscape 16:9
    /// Aumentar para ver más mapa; bajar para acercar la cámara.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [SerializeField] private Camera _cam;

        // Ajustá este valor para cambiar el zoom:
        // 3 = muy cerca   (bueno para móvil portrait)
        // 4 = recomendado (similar al cliente web de referencia)
        // 6 = más alejado
        [SerializeField] private float orthographicSize = 4f;

        private bool _applied = false;

        void Awake()
        {
            Instance = this;
            if (_cam == null) _cam = Camera.main;
            Apply();
        }

        void Start()  => Apply();
        void Update() { if (!_applied) Apply(); }

        private void Apply()
        {
            if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }

            _cam.orthographicSize = orthographicSize;
            _cam.transform.position = new Vector3(0, 0, -10);
            _cam.clearFlags      = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Color.black;
            _applied = true;
        }
    }
}
