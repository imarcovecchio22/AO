using UnityEngine;
using ArgentumOnline.Network;
using ArgentumOnline.Game;

/// <summary>
/// Script de prueba — conecta al servidor con credenciales hardcodeadas.
/// Adjuntar al mismo GameObject que NetworkManager.
/// Borrar cuando se implemente el login real.
/// </summary>
public class ConnectionTest : MonoBehaviour
{
    [Header("Credenciales de prueba")]
    public string accountId  = "69c0a6c336a1fe21e4f99c0c";
    public string characterId= "69c0a79ab2406d190cf6d928";
    public string email      = "test@test.com";
    public byte   typeGame   = 1;
    public byte   idChar     = 0;

    void Start()
    {
        SessionData.Current = new SessionData
        {
            AccountId   = accountId,
            CharacterId = characterId,
            Email       = email,
            TypeGame    = typeGame,
            IdChar      = idChar
        };

        // Suscribirse al evento antes de conectar
        GameState.Instance.OnConnected += OnConnected;

        NetworkManager.Instance.Connect();
    }

    private void OnConnected()
    {
        var p = GameState.Instance.LocalPlayer;
        Debug.Log($"[Test] Conectado como {p.Name} lv{p.Level} en mapa {p.Map} @ {p.PosX},{p.PosY}");
        Debug.Log($"[Test] HP: {p.HP}/{p.MaxHP} | Mana: {p.Mana}/{p.MaxMana} | Gold: {p.Gold}");
    }
}
