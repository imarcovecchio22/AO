namespace ArgentumOnline.Network
{
    /// <summary>
    /// Datos de sesión que persisten entre la pantalla de login y la escena de juego.
    /// Setear antes de cargar la escena de juego.
    /// </summary>
    public class SessionData
    {
        public static SessionData Current { get; set; }

        public string AccountId;    // ID MongoDB de la cuenta
        public string CharacterId;  // ID MongoDB del personaje (vacío para PvP)
        public string Email;
        public byte   TypeGame;     // 1=normal, 2=arena (solo mapa 272)
        public byte   IdChar;       // índice numérico del personaje seleccionado
    }
}
