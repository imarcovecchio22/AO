using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ArgentumOnline.Renderer
{
    [System.Serializable]
    internal class ObjEntry { public int grhIndex; }

    /// <summary>
    /// Carga objs.json y expone el grhIndex de cada item por su ID.
    /// </summary>
    public class ObjDatabase : MonoBehaviour
    {
        public static ObjDatabase Instance { get; private set; }

        // itemId → grhIndex
        private readonly Dictionary<int, int> _grhByItem = new();

        void Awake() => Instance = this;

        public IEnumerator Initialize()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "init", "objs.json");
            string url  = "file://" + path;

            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ObjDB] No se pudo cargar objs.json: {req.error}");
                yield break;
            }

            try
            {
                var root = Newtonsoft.Json.JsonConvert.DeserializeObject<
                    Dictionary<string, ObjEntry>>(req.downloadHandler.text);

                foreach (var kvp in root)
                {
                    if (!int.TryParse(kvp.Key, out int itemId)) continue;
                    if (kvp.Value.grhIndex > 0) _grhByItem[itemId] = kvp.Value.grhIndex;
                }

                Debug.Log($"[ObjDB] {_grhByItem.Count} objetos cargados");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ObjDB] Error parseando objs.json: {e.Message}");
            }
        }

        /// <summary>Devuelve el grhIndex del sprite del objeto, o 0 si no existe.</summary>
        public int GetGrhIndex(int itemId)
        {
            return _grhByItem.TryGetValue(itemId, out int grh) ? grh : 0;
        }
    }
}
