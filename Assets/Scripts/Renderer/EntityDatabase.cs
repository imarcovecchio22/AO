using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Carga bodies.json, heads.json, armas.json, cascos.json, escudos.json
    /// y provee los grhIndex por dirección para cada entidad.
    /// </summary>
    public class EntityDatabase : MonoBehaviour
    {
        public static EntityDatabase Instance { get; private set; }

        // bodyId → { heading → grhIndex }
        public Dictionary<int, DirectionalGrh> Bodies  { get; private set; } = new();
        public Dictionary<int, DirectionalGrh> Heads   { get; private set; } = new();
        public Dictionary<int, DirectionalGrh> Weapons { get; private set; } = new();
        public Dictionary<int, DirectionalGrh> Helmets { get; private set; } = new();
        public Dictionary<int, DirectionalGrh> Shields { get; private set; } = new();

        public bool IsReady { get; private set; }

        void Awake() => Instance = this;

        public IEnumerator Initialize()
        {
            yield return Load("bodies.json",  Bodies,  hasBodiesOffset: true);
            yield return Load("heads.json",   Heads);
            yield return Load("armas.json",   Weapons);
            yield return Load("cascos.json",  Helmets);
            yield return Load("escudos.json", Shields);
            IsReady = true;
            Debug.Log($"[EntityDB] Bodies:{Bodies.Count} Heads:{Heads.Count} Weapons:{Weapons.Count}");
        }

        private IEnumerator Load(string fileName, Dictionary<int, DirectionalGrh> target,
                                  bool hasBodiesOffset = false)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "init", fileName);
            using var req = UnityWebRequest.Get("file://" + path);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[EntityDB] No se pudo cargar {fileName}");
                yield break;
            }

            try
            {
                var raw = Newtonsoft.Json.JsonConvert.DeserializeObject<
                    Dictionary<string, Newtonsoft.Json.Linq.JObject>>(req.downloadHandler.text);

                foreach (var kvp in raw)
                {
                    if (!int.TryParse(kvp.Key, out int id)) continue;
                    var obj = kvp.Value;

                    var dg = new DirectionalGrh
                    {
                        Up    = obj["1"]?.ToObject<int>() ?? 0,
                        Down  = obj["2"]?.ToObject<int>() ?? 0,
                        Right = obj["3"]?.ToObject<int>() ?? 0,
                        Left  = obj["4"]?.ToObject<int>() ?? 0,
                    };

                    if (hasBodiesOffset)
                    {
                        dg.HeadOffsetX = obj["headOffsetX"]?.ToObject<int>() ?? 0;
                        dg.HeadOffsetY = obj["headOffsetY"]?.ToObject<int>() ?? 0;
                    }

                    target[id] = dg;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[EntityDB] Error parseando {fileName}: {e.Message}");
            }
        }

        /// <summary>Obtiene el grhIndex para un body/head/arma según el heading actual.</summary>
        public static int GetGrhForHeading(DirectionalGrh dg, byte heading)
        {
            // Heading: 1=Up 2=Down 3=Right 4=Left
            return heading switch
            {
                1 => dg.Up,
                2 => dg.Down,
                3 => dg.Right,
                4 => dg.Left,
                _ => dg.Down
            };
        }
    }

    public class DirectionalGrh
    {
        public int Up, Down, Right, Left;
        public int HeadOffsetX, HeadOffsetY;  // solo bodies
    }
}
