using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ArgentumOnline.Renderer
{
    /// <summary>
    /// Carga graficos.json y cachea Sprites recortados por grhIndex.
    /// Los PNGs se cargan desde StreamingAssets/graficos/ en runtime.
    /// </summary>
    public class GrhDatabase : MonoBehaviour
    {
        public static GrhDatabase Instance { get; private set; }

        // grhIndex → GrhData
        private Dictionary<int, GrhData> _grhs = new Dictionary<int, GrhData>();

        // numFile → Texture2D cargada
        private Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();

        // grhIndex → Sprite recortado (cache)
        private Dictionary<int, Sprite> _sprites = new Dictionary<int, Sprite>();

        public bool IsReady { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        public IEnumerator Initialize()
        {
            yield return StartCoroutine(LoadGrhJson());
            Debug.Log($"[GrhDB] Cargados {_grhs.Count} GRHs");
            IsReady = true;
        }

        // ── Carga graficos.json ───────────────────────────────────────────────

        private IEnumerator LoadGrhJson()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "init", "graficos.json");
            string url  = "file://" + path;

            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GrhDB] Error cargando graficos.json: {req.error}");
                yield break;
            }

            // graficos.json es un objeto plano: { "1": {...}, "2": {...}, ... }
            // Unity JsonUtility no soporta diccionarios raíz — usamos un wrapper manual
            ParseGrhJson(req.downloadHandler.text);
        }

        private void ParseGrhJson(string json)
        {
            // Parseo manual simple: buscamos cada entrada "index": { ... }
            // Usamos MiniJSON o SimpleJSON si está disponible, sino parseamos con splits básicos
            // Aquí usamos Newtonsoft si está disponible (viene con Unity 2021+)
            try
            {
                var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, GrhData>>(json);
                foreach (var kvp in dict)
                {
                    if (int.TryParse(kvp.Key, out int idx))
                        _grhs[idx] = kvp.Value;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GrhDB] Error parseando graficos.json: {e.Message}");
            }
        }

        // ── API pública ───────────────────────────────────────────────────────

        /// <summary>
        /// Obtiene el Sprite para un grhIndex. Si el PNG no está cargado, lo carga async.
        /// Callback se llama cuando el sprite esté listo.
        /// </summary>
        public void GetSprite(int grhIndex, Action<Sprite> callback)
        {
            if (_sprites.TryGetValue(grhIndex, out var cached))
            {
                callback(cached);
                return;
            }

            if (!_grhs.TryGetValue(grhIndex, out var grh))
            {
                callback(null);
                return;
            }

            // Para GRHs animados, usar el primer frame
            int targetGrh = grhIndex;
            if (grh.numFrames > 1 && grh.frames != null && grh.frames.TryGetValue("1", out string frameStr))
            {
                if (int.TryParse(frameStr, out int frameGrh))
                    targetGrh = frameGrh;
                if (!_grhs.TryGetValue(targetGrh, out grh))
                {
                    callback(null);
                    return;
                }
            }

            StartCoroutine(LoadSpriteCoroutine(grhIndex, grh, callback));
        }

        /// <summary>
        /// Versión síncrona — solo funciona si el PNG ya fue cargado antes.
        /// </summary>
        public Sprite GetSpriteCached(int grhIndex)
        {
            _sprites.TryGetValue(grhIndex, out var s);
            return s;
        }

        public bool TryGetGrh(int grhIndex, out GrhData grh) => _grhs.TryGetValue(grhIndex, out grh);

        // ── Carga de textura y recorte ────────────────────────────────────────

        private IEnumerator LoadSpriteCoroutine(int grhIndex, GrhData grh, Action<Sprite> callback)
        {
            Texture2D tex = null;

            if (_textures.TryGetValue(grh.numFile, out tex))
            {
                var sprite = CropSprite(tex, grh);
                _sprites[grhIndex] = sprite;
                callback(sprite);
                yield break;
            }

            string path = Path.Combine(Application.streamingAssetsPath, "graficos", grh.numFile + ".png");
            string url  = "file://" + path;

            using var req = UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                // PNG no existe — silencioso (hay muchos GRHs sin archivo)
                callback(null);
                yield break;
            }

            tex = DownloadHandlerTexture.GetContent(req);
            tex.filterMode = FilterMode.Point;  // pixel art — sin blur
            _textures[grh.numFile] = tex;

            var sp = CropSprite(tex, grh);
            _sprites[grhIndex] = sp;
            callback(sp);
        }

        private Sprite CropSprite(Texture2D tex, GrhData grh)
        {
            // Unity y el protocolo usan Y invertido — corregir
            int texH  = tex.height;
            int rectY = texH - grh.sY - grh.height;
            if (rectY < 0) rectY = 0;

            var rect = new Rect(grh.sX, rectY, grh.width, grh.height);
            // pivot en centro para facilitar posicionamiento
            return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
