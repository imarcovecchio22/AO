namespace ArgentumOnline.Game
{
    public class InventorySlot
    {
        public byte   IdPos;
        public uint   IdItem;
        public string Name;
        public bool   Equipped;
        public ushort GrhIndex;
        public ushort Cantidad;
        public uint   Valor;
        public byte   ObjType;
        public bool   ValidParaClase;
        public string DataObj;        // "Daño: min/max" o "Defensa: min/max"

        public bool IsEmpty => IdItem == 0;

        /// <summary>Precio de venta al NPC = Valor / 3</summary>
        public uint PrecioVenta => Valor / 3;
    }

    public class SpellSlot
    {
        public byte   IdPos;
        public ushort IdSpell;
        public string Name;
        public ushort ManaRequired;

        public bool IsEmpty => IdSpell == 0;
    }
}
