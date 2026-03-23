using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Carga mapa_XXX.json desde StreamingAssets y construye un MapGrid.
    /// </summary>
    public class MapLoader : MonoBehaviour
    {
        public static MapLoader Instance { get; private set; }

        private Dictionary<int, MapGrid> _maps = new Dictionary<int, MapGrid>();

        void Awake() => Instance = this;

        public IEnumerator LoadMap(int mapNumber, Action<MapGrid> onDone)
        {
            if (_maps.TryGetValue(mapNumber, out var cached))
            {
                onDone(cached);
                yield break;
            }

            string path = Path.Combine(Application.streamingAssetsPath, $"mapa_{mapNumber}.json");
            string url  = "file://" + path;

            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[MapLoader] No se pudo cargar mapa_{mapNumber}.json: {req.error}");
                onDone(null);
                yield break;
            }

            var grid = ParseMap(mapNumber, req.downloadHandler.text);
            _maps[mapNumber] = grid;
            Debug.Log($"[MapLoader] Mapa {mapNumber} cargado");
            onDone(grid);
        }

        private MapGrid ParseMap(int mapNumber, string json)
        {
            var grid = new MapGrid { MapNumber = mapNumber };

            try
            {
                // El JSON tiene estructura: { "272": { "1": { "1": { blocked, graphics }, ... }, ... } }
                var root = Newtonsoft.Json.JsonConvert.DeserializeObject<
                    Dictionary<string, Dictionary<string, Dictionary<string, RawCell>>>>(json);

                if (!root.TryGetValue(mapNumber.ToString(), out var rows)) return grid;

                foreach (var rowKvp in rows)
                {
                    if (!int.TryParse(rowKvp.Key, out int y)) continue;
                    if (y < 1 || y > 100) continue;

                    foreach (var colKvp in rowKvp.Value)
                    {
                        if (!int.TryParse(colKvp.Key, out int x)) continue;
                        if (x < 1 || x > 100) continue;

                        var raw  = colKvp.Value;
                        var cell = grid.Cells[y][x];

                        cell.blocked = raw.blocked == 1;

                        if (raw.graphics != null)
                        {
                            foreach (var g in raw.graphics)
                            {
                                if (int.TryParse(g.Value.ToString(), out int grhIdx))
                                    cell.graphics[g.Key] = grhIdx;
                            }
                        }

                        if (raw.trigger != null) cell.trigger = raw.trigger.Value;
                        if (raw.obj     != null) cell.objIndex = raw.obj.Value;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapLoader] Error parseando mapa {mapNumber}: {e.Message}");
            }

            return grid;
        }

        // Clase auxiliar para deserialización del JSON crudo
        [Serializable]
        private class RawCell
        {
            public int blocked;
            public Dictionary<string, object> graphics;
            public int? trigger;
            public int? obj;
        }
    }
}
