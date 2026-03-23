namespace ArgentumOnline.Network
{
    /// <summary>
    /// IDs de paquetes que el servidor envía al cliente.
    /// </summary>
    public enum ServerPacketID : byte
    {
        // Entidades
        GetMyCharacter   = 1,   // propio personaje al conectar
        GetCharacter     = 2,   // otro jugador entra al área visual
        ChangeRopa       = 3,   // otro jugador cambió armadura
        ActPosition      = 4,   // otro jugador/NPC se movió
        ChangeHeading    = 5,   // otro jugador/NPC cambió dirección
        DeleteCharacter  = 6,   // entidad sale del área visual
        Dialog           = 7,   // burbuja de diálogo sobre entidad
        Console          = 8,   // mensaje en consola/chat
        Pong             = 9,   // respuesta a ping
        AnimFX           = 10,  // efecto visual sobre entidad
        Inmo             = 11,  // inmovilización/parálisis
        UpdateHP         = 12,  // HP actual
        UpdateMaxHP      = 13,  // HP y maxHP
        UpdateMana       = 14,  // mana actual
        TeleportMe       = 15,  // teleport del propio jugador
        // 16 = sin uso documentado
        // 17 = sin uso documentado
        // 18 = sin uso documentado
        ActOnline        = 19,  // usuarios online
        ConsoleOnline    = 20,  // sin uso documentado
        ActPositionServer= 21,  // corrección de posición del propio jugador
        ActExp           = 22,  // experiencia actual
        ActMyLevel       = 23,  // subida de nivel
        ActGold          = 24,  // oro del jugador
        ActColorName     = 25,  // cambiar color del nombre de entidad
        ChangeHelmet     = 26,  // otro jugador cambió casco
        ChangeWeapon     = 27,  // otro jugador cambió arma
        Error            = 28,  // mensaje de error (cierra conexión)
        ChangeName       = 29,  // sin uso documentado
        GetNpc           = 30,  // NPC entra al área visual
        ChangeShield     = 31,  // otro jugador cambió escudo
        PutBodyAndHeadDead= 32, // jugador murió
        RevivirUsuario   = 33,  // jugador revivió
        QuitarUserInvItem= 34,  // quitar item del inventario
        RenderItem       = 35,  // item aparece en el suelo
        DeleteItem       = 36,  // item desaparece del suelo
        AgregarUserInvItem= 37, // agregar item al inventario
        ChangeArrow      = 38,  // cambiar flecha equipada
        BlockMap         = 39,  // cambiar bloqueo de casilla (puertas)
        ChangeObjIndex   = 40,  // cambiar sprite de objeto en suelo
        OpenTrade        = 41,  // abrir tienda de NPC
        AprenderSpell    = 42,  // aprender nuevo hechizo
        CloseForce       = 43,  // servidor fuerza desconexión
        NameMap          = 44,  // nombre del mapa actual
        ChangeBody       = 45,  // cambio completo de apariencia
        Navegando        = 46,  // estado de navegación en barco
        UpdateAgilidad   = 47,  // atributo agilidad
        UpdateFuerza     = 48,  // atributo fuerza
        PlaySound        = 49,  // reproducir sonido
    }
}
