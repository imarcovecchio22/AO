using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Reproduce efectos visuales animados (animFX, packet 10) sobre la posición
    /// de una entidad. Usa fxs.json para mapear el ID de FX al GRH e offset correctos.
    /// </summary>
    public class FXManager : MonoBehaviour
    {
        public static FXManager Instance { get; private set; }

        private struct FxEntry { public int grh; public float offsetX, offsetY; }

        private readonly Dictionary<int, FxEntry> _fxTable = new();
        private bool _tableLoaded = false;

        void Awake()
        {
            Instance = this;
            StartCoroutine(LoadFxTable());
        }

        private IEnumerator LoadFxTable()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "init", "fxs.json");
            using var req = UnityWebRequest.Get(path);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[FXManager] No se pudo cargar fxs.json: {req.error}");
                _tableLoaded = true;
                yield break;
            }

            var root = JObject.Parse(req.downloadHandler.text);
            foreach (var kvp in root)
            {
                if (!int.TryParse(kvp.Key, out int fxId)) continue;
                var node = kvp.Value;
                _fxTable[fxId] = new FxEntry
                {
                    grh     = node["grh"].Value<int>(),
                    offsetX = node["offsetX"].Value<float>() / 32f,   // px → unidades Unity
                    offsetY = node["offsetY"].Value<float>() / 32f
                };
            }
            _tableLoaded = true;
            Debug.Log($"[FXManager] fxs.json cargado: {_fxTable.Count} efectos.");
        }

        /// <summary>
        /// Reproduce el efecto fxId (ID de la tabla FX) sobre la entidad indicada.
        /// </summary>
        public void PlayFX(long entityId, ushort fxId)
        {
            var t = EntityRenderer.Instance?.GetEntityTransform(entityId);
            Vector3 pos = t != null ? t.position : Vector3.zero;
            StartCoroutine(PlayFXRoutine(pos, fxId));
        }

        private IEnumerator PlayFXRoutine(Vector3 entityPos, int fxId)
        {
            // Esperar a que la tabla esté lista (debería ser inmediato en la mayoría de casos)
            while (!_tableLoaded)
                yield return null;

            if (!_fxTable.TryGetValue(fxId, out var entry))
            {
                Debug.LogWarning($"[FXManager] FX ID {fxId} no encontrado en fxs.json");
                yield break;
            }

            if (!GrhDatabase.Instance.TryGetGrh(entry.grh, out var grh))
            {
                Debug.LogWarning($"[FXManager] GRH {entry.grh} (fx={fxId}) no encontrado en graficos.json");
                yield break;
            }

            var go = new GameObject("FX");
            go.transform.SetParent(transform);
            // Centrar el FX sobre la entidad. Los offsets del fxs.json son para el sistema
            // de píxeles del AO original (top-left del tile) y no se trasladan directamente
            // a coordenadas Unity, así que los ignoramos y centramos el efecto.
            go.transform.position = entityPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 500;  // siempre encima de todo

            if (grh.numFrames > 1)
            {
                float frameSec = grh.speed > 0 ? grh.speed / 1000f : 0.085f;

                for (int f = 1; f <= grh.numFrames; f++)
                {
                    string key = f.ToString();
                    if (grh.frames != null &&
                        grh.frames.TryGetValue(key, out string frameStr) &&
                        int.TryParse(frameStr, out int frameGrh))
                    {
                        int captured = frameGrh;
                        SpriteRenderer capturedSr = sr;
                        GrhDatabase.Instance.GetSprite(captured, sprite =>
                        {
                            if (capturedSr != null) capturedSr.sprite = sprite;
                        });
                    }
                    yield return new WaitForSeconds(frameSec);
                }
            }
            else
            {
                GrhDatabase.Instance.GetSprite(entry.grh, sprite =>
                {
                    if (sr != null) sr.sprite = sprite;
                });
                yield return new WaitForSeconds(0.5f);
            }

            Destroy(go);
        }
    }
}
