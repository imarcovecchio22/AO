using System.Collections;
using UnityEngine;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Reproduce efectos visuales animados (animFX, packet 10) sobre la posición
    /// de una entidad. Cada FX es un SpriteRenderer temporal que se destruye solo.
    /// </summary>
    public class FXManager : MonoBehaviour
    {
        public static FXManager Instance { get; private set; }

        void Awake() => Instance = this;

        /// <summary>
        /// Reproduce el efecto fxGrh sobre la entidad con el ID dado.
        /// Si la entidad no se encuentra, lo reproduce en el centro del viewport.
        /// </summary>
        public void PlayFX(long entityId, ushort fxGrh)
        {
            // fxGrh es un ID del sistema FX del AO original (no un índice de GrhDatabase).
            // Sin la tabla de mapeo FX→GRH no podemos renderizar el efecto correcto.
            // TODO: cargar fx_mapping.json cuando esté disponible.
        }

        private IEnumerator PlayFXRoutine(Vector3 worldPos, ushort fxGrh)
        {
            var go = new GameObject("FX");
            go.transform.SetParent(transform);
            go.transform.position = worldPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 500;  // siempre encima de todo

            if (GrhDatabase.Instance.TryGetGrh(fxGrh, out var grh) && grh.numFrames > 1)
            {
                // Animado: reproducir cada frame exactamente una vez
                float frameSec = grh.speed > 0 ? grh.speed / 1000f : 0.085f;

                for (int f = 1; f <= grh.numFrames; f++)
                {
                    string key = f.ToString();
                    if (grh.frames != null &&
                        grh.frames.TryGetValue(key, out string frameStr) &&
                        int.TryParse(frameStr, out int frameGrh))
                    {
                        int capturedGrh = frameGrh;
                        SpriteRenderer capturedSr = sr;
                        GrhDatabase.Instance.GetSprite(capturedGrh, sprite =>
                        {
                            if (capturedSr != null) capturedSr.sprite = sprite;
                        });
                    }
                    yield return new WaitForSeconds(frameSec);
                }
            }
            else
            {
                // Estático: mostrar durante 0.5 s
                GrhDatabase.Instance.GetSprite(fxGrh, sprite =>
                {
                    if (sr != null) sr.sprite = sprite;
                });
                yield return new WaitForSeconds(0.5f);
            }

            Destroy(go);
        }
    }
}
