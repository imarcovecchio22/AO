using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentumOnline.Network
{
    /// <summary>
    /// Construye paquetes binarios little-endian para enviar al servidor.
    /// Uso: var bytes = PacketSerializer.Instance.Position(Heading.Up);
    /// </summary>
    public class PacketSerializer
    {
        public static readonly PacketSerializer Instance = new PacketSerializer();

        private readonly List<byte> _buf = new List<byte>(64);

        private PacketSerializer Start(ClientPacketID id)
        {
            _buf.Clear();
            _buf.Add((byte)id);
            return this;
        }

        private PacketSerializer WriteByte(byte v)
        {
            _buf.Add(v);
            return this;
        }

        private PacketSerializer WriteShort(ushort v)
        {
            _buf.AddRange(BitConverter.GetBytes(v));
            return this;
        }

        private PacketSerializer WriteInt(uint v)
        {
            _buf.AddRange(BitConverter.GetBytes(v));
            return this;
        }

        private PacketSerializer WriteString(string s)
        {
            // Protocolo: uint16 con cantidad de chars, luego bytes UTF-8
            _buf.AddRange(BitConverter.GetBytes((ushort)s.Length));
            _buf.AddRange(Encoding.UTF8.GetBytes(s));
            return this;
        }

        private byte[] Build() => _buf.ToArray();

        // ── Paquetes concretos ────────────────────────────────────────────────

        public byte[] ConnectCharacter(string accountId, string charId, string email,
                                       byte typeGame, byte idChar)
            => Start(ClientPacketID.ConnectCharacter)
                .WriteString(accountId)
                .WriteString(charId)
                .WriteString(email)
                .WriteByte(typeGame)
                .WriteByte(idChar)
                .Build();

        public byte[] ChangeHeading(Heading heading)
            => Start(ClientPacketID.ChangeHeading)
                .WriteByte((byte)heading)
                .Build();

        /// <summary>
        /// Movimiento: heading 1-4 mueve, heading 5 (None) solo cambia orientación.
        /// </summary>
        public byte[] Position(Heading heading)
            => Start(ClientPacketID.Position)
                .WriteByte((byte)heading)
                .Build();

        public byte[] Click(byte x, byte y)
            => Start(ClientPacketID.Click)
                .WriteByte(x)
                .WriteByte(y)
                .Build();

        public byte[] UseItem(uint slotPos)
            => Start(ClientPacketID.UseItem)
                .WriteInt(slotPos)
                .Build();

        public byte[] EquipItem(uint slotPos)
            => Start(ClientPacketID.EquipItem)
                .WriteInt(slotPos)
                .Build();

        public byte[] Dialog(string msg)
            => Start(ClientPacketID.Dialog)
                .WriteString(msg)
                .Build();

        public byte[] Ping()
            => Start(ClientPacketID.Ping)
                .Build();

        public byte[] AttackMelee()
            => Start(ClientPacketID.AttackMelee)
                .Build();

        public byte[] AttackRange(byte x, byte y)
            => Start(ClientPacketID.AttackRange)
                .WriteByte(x)
                .WriteByte(y)
                .Build();

        /// <summary>
        /// Atacar con hechizo.
        /// </summary>
        /// <param name="spellSlot">Slot del hechizo (0-27)</param>
        /// <param name="x">Posición X del objetivo en el mapa</param>
        /// <param name="y">Posición Y del objetivo en el mapa</param>
        public byte[] AttackSpell(byte spellSlot, byte x, byte y)
            => Start(ClientPacketID.AttackSpell)
                .WriteByte(spellSlot)
                .WriteByte(x)
                .WriteByte(y)
                .Build();

        public byte[] TirarItem(uint slotPos, ushort cantidad)
            => Start(ClientPacketID.TirarItem)
                .WriteInt(slotPos)
                .WriteShort(cantidad)
                .Build();

        public byte[] AgarrarItem()
            => Start(ClientPacketID.AgarrarItem)
                .Build();

        public byte[] BuyItem(byte slotPos, ushort cantidad)
            => Start(ClientPacketID.BuyItem)
                .WriteByte(slotPos)
                .WriteShort(cantidad)
                .Build();

        public byte[] SellItem(byte slotPos, ushort cantidad)
            => Start(ClientPacketID.SellItem)
                .WriteByte(slotPos)
                .WriteShort(cantidad)
                .Build();

        public byte[] ChangeSeguro()
            => Start(ClientPacketID.ChangeSeguro)
                .Build();
    }
}
