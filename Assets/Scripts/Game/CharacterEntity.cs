using UnityEngine;

namespace ArgentumOnline.Game
{
    public enum EntityType { Player, NPC }

    /// <summary>
    /// Representa cualquier entidad visible en el mapa (otro jugador o NPC).
    /// El jugador local usa LocalPlayer en su lugar.
    /// </summary>
    public class CharacterEntity
    {
        public long       Id;
        public string     Name;
        public EntityType Type;
        public byte       IdClase;
        public ushort     Map;
        public byte       PosX;
        public byte       PosY;
        public byte       Heading;    // 1=up 2=down 3=right 4=left
        public ushort     IdHead;
        public ushort     IdHelmet;
        public ushort     IdWeapon;
        public ushort     IdShield;
        public ushort     IdBody;
        public byte       Privileges;
        public string     Color;
        public string     Clan;
        public bool       IsDead;

        // Interpolación de movimiento (igual que el cliente web: offset de 32px a 0)
        public Vector2 RenderOffset;   // en pixels, se decrementa cada frame
        public Vector2 TargetPos;      // posición lógica destino en tiles
    }
}
