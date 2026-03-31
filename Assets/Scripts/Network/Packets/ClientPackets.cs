namespace ArgentumOnline.Network
{
    /// <summary>
    /// IDs de paquetes que el cliente envía al servidor.
    /// </summary>
    public enum ClientPacketID : byte
    {
        ChangeHeading    = 1,   // byte heading (1=up,2=down,3=right,4=left)
        Click            = 2,   // byte x, byte y
        UseItem          = 3,   // uint idPos
        EquipItem        = 4,   // uint idPos
        ConnectCharacter = 5,   // string idAccount, string idCharacter, string email, byte typeGame, byte idChar
        Position         = 6,   // byte heading (1-5, 5=sin movimiento)
        Dialog           = 7,   // string msg
        Ping             = 8,   // (sin campos)
        AttackMelee      = 9,   // (sin campos)
        AttackRange      = 10,  // byte x, byte y
        AttackSpell      = 11,  // byte idPos, byte x, byte y
        TirarItem        = 12,  // uint idPos, ushort cant
        AgarrarItem      = 13,  // (sin campos)
        BuyItem          = 14,  // byte idPos, ushort cant
        SellItem         = 15,  // byte idPos, ushort cant
        MoveTo           = 16,  // byte x, byte y (teleport al tile clickeado)
        ChangeSeguro     = 17,  // (sin campos, toggle seguro PvP)
        Meditar          = 18,  // (sin campos, toggle meditación)
    }

    /// <summary>
    /// Direcciones de movimiento/orientación.
    /// </summary>
    public enum Heading : byte
    {
        Up    = 1,
        Down  = 2,
        Right = 3,
        Left  = 4,
        None  = 5,  // usado en paquete Position para indicar sin movimiento
    }

    /// <summary>
    /// Tipo de partida al conectar.
    /// </summary>
    public enum TypeGame : byte
    {
        Normal = 1,
        Arena  = 2, // modo PvP, solo mapa 272
    }
}
