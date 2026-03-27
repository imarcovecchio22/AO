namespace ArgentumOnline.Game
{
    public class TradeData
    {
        public NpcTradeItem[]  NpcItems;
        public UserTradeItem[] UserItems;
    }

    public class NpcTradeItem
    {
        public byte   IdPos;          // índice en la lista del NPC (para BuyItem)
        public ushort GrhIndex;
        public string Name;
        public ushort Cantidad;
        public uint   Valor;
        public bool   ValidParaClase;
        public string DataObj;
    }

    public class UserTradeItem
    {
        public byte   IdPos;          // slot en el inventario del jugador (para SellItem)
        public ushort GrhIndex;
        public string Name;
        public ushort Cantidad;
        public uint   ValorVenta;
        public bool   Equipped;
        public bool   ValidParaClase;
        public string DataObj;
    }
}
