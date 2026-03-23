using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArgentumOnline.Network
{
    /// <summary>
    /// Cliente HTTP para la API REST (puerto 3000).
    /// Maneja sesión via cookie manualmente.
    /// </summary>
    public class ApiClient : MonoBehaviour
    {
        public static ApiClient Instance { get; private set; }

        [SerializeField] private string apiBase = "http://localhost:3000/api";

        private string _sessionCookie = "";

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Modelos ───────────────────────────────────────────────────────────

        public class LoginResult
        {
            public bool   Success;
            public string AccountId;
            public string Name;
            public string Email;
            public string Error;
        }

        public class CharacterInfo
        {
            public string Id;
            public string Name;
            public int    IdHead, IdBody, IdHelmet, IdWeapon, IdShield;
        }

        // ── Login ─────────────────────────────────────────────────────────────

        public IEnumerator Login(string username, string password, Action<LoginResult> callback)
        {
            var body = JsonConvert.SerializeObject(new { name = username, password });
            using var req = new UnityWebRequest($"{apiBase}/login", "POST");
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(_sessionCookie))
                req.SetRequestHeader("Cookie", _sessionCookie);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                callback(new LoginResult { Error = req.error });
                yield break;
            }

            // Guardar cookie de sesión
            StoreCookie(req.GetResponseHeader("Set-Cookie"));

            try
            {
                var json = JObject.Parse(req.downloadHandler.text);
                if (json["error"]?.ToObject<bool>() == true)
                {
                    callback(new LoginResult { Error = json["message"]?.ToString() ?? "Error desconocido" });
                }
                else
                {
                    callback(new LoginResult
                    {
                        Success   = true,
                        AccountId = json["accountId"]?.ToString() ?? json["_id"]?.ToString(),
                        Name      = json["name"]?.ToString(),
                        Email     = json["email"]?.ToString()
                    });
                }
            }
            catch (Exception e)
            {
                callback(new LoginResult { Error = $"Parse error: {e.Message}" });
            }
        }

        // ── Personajes ────────────────────────────────────────────────────────

        public IEnumerator GetCharacters(Action<CharacterInfo[]> callback)
        {
            using var req = UnityWebRequest.Get($"{apiBase}/characters");
            if (!string.IsNullOrEmpty(_sessionCookie))
                req.SetRequestHeader("Cookie", _sessionCookie);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[API] GetCharacters error: {req.error}");
                callback(Array.Empty<CharacterInfo>());
                yield break;
            }

            StoreCookie(req.GetResponseHeader("Set-Cookie"));

            try
            {
                var arr = JArray.Parse(req.downloadHandler.text);
                var list = new CharacterInfo[arr.Count];
                for (int i = 0; i < arr.Count; i++)
                {
                    var c = arr[i];
                    list[i] = new CharacterInfo
                    {
                        Id        = c["_id"]?.ToString(),
                        Name      = c["name"]?.ToString(),
                        IdHead    = c["idHead"]?.ToObject<int>() ?? 0,
                        IdBody    = c["idBody"]?.ToObject<int>() ?? 0,
                        IdHelmet  = c["idHelmet"]?.ToObject<int>() ?? 0,
                        IdWeapon  = c["idWeapon"]?.ToObject<int>() ?? 0,
                        IdShield  = c["idShield"]?.ToObject<int>() ?? 0,
                    };
                }
                callback(list);
            }
            catch (Exception e)
            {
                Debug.LogError($"[API] Parse characters: {e.Message}");
                callback(Array.Empty<CharacterInfo>());
            }
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private void StoreCookie(string setCookieHeader)
        {
            if (string.IsNullOrEmpty(setCookieHeader)) return;
            // Extraer solo "name=value" del primer cookie (ignorar atributos Path, HttpOnly, etc.)
            int semi = setCookieHeader.IndexOf(';');
            _sessionCookie = semi >= 0 ? setCookieHeader[..semi] : setCookieHeader;
        }
    }
}
