using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Crea en runtime un Volume global con Bloom y ColorAdjustments para darle
    /// más vida visual al juego (estilo cliente web competidor).
    /// Se autoiicializa en Awake; no requiere configuración manual en escena.
    /// </summary>
    public class PostProcessSetup : MonoBehaviour
    {
        public static PostProcessSetup Instance { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            SetupVolume();
            EnablePostProcessOnCamera();
        }

        private void SetupVolume()
        {
            var go = new GameObject("GlobalPostProcess");
            go.transform.SetParent(transform);

            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;

            // ── Bloom ─────────────────────────────────────────────────────────
            var bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.intensity.Override(0.55f);
            bloom.threshold.Override(0.80f);
            bloom.scatter.Override(0.65f);

            // ── Color Adjustments ─────────────────────────────────────────────
            var color = profile.Add<ColorAdjustments>(true);
            color.active = true;
            color.postExposure.Override(0.10f);
            color.contrast.Override(18f);
            color.saturation.Override(22f);
            color.colorFilter.Override(new Color(1f, 0.97f, 0.92f));

            // ── Vignette ──────────────────────────────────────────────────────
            var vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.55f);
        }

        /// <summary>
        /// Asegura que la cámara principal tenga Post Processing habilitado
        /// (en URP esto es una propiedad de UniversalAdditionalCameraData).
        /// </summary>
        private void EnablePostProcessOnCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var camData = cam.GetUniversalAdditionalCameraData();
            if (camData != null)
                camData.renderPostProcessing = true;
        }
    }
}
