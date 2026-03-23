using System;
using System.Text;

namespace ArgentumOnline.Network
{
    /// <summary>
    /// Lector de buffers binarios little-endian para el protocolo de Argentum Online.
    /// </summary>
    public class ByteBuffer
    {
        private readonly byte[] _data;
        private int _readPos;

        public ByteBuffer(byte[] data)
        {
            _data = data;
            _readPos = 0;
        }

        public int BytesRemaining => _data.Length - _readPos;

        public byte ReadByte() => _data[_readPos++];

        public ushort ReadShort()
        {
            ushort v = BitConverter.ToUInt16(_data, _readPos);
            _readPos += 2;
            return v;
        }

        public uint ReadInt()
        {
            uint v = BitConverter.ToUInt32(_data, _readPos);
            _readPos += 4;
            return v;
        }

        public float ReadFloat()
        {
            float v = BitConverter.ToSingle(_data, _readPos);
            _readPos += 4;
            return v;
        }

        public double ReadDouble()
        {
            double v = BitConverter.ToDouble(_data, _readPos);
            _readPos += 8;
            return v;
        }

        /// <summary>
        /// IDs de personaje/NPC: se transmiten como double (timestamp Date.now()).
        /// Convertimos a long para usarlos como clave en diccionarios.
        /// </summary>
        public long ReadId() => (long)ReadDouble();

        /// <summary>
        /// Strings: uint16 con cantidad de chars UTF-8, luego los bytes.
        /// Para ASCII es 1:1. Para caracteres especiales (tildes, ñ) puede diferir.
        /// </summary>
        public string ReadString()
        {
            ushort charCount = ReadShort();
            if (charCount == 0) return string.Empty;
            int available = _data.Length - _readPos;
            if (charCount > available) return string.Empty;
            byte[] raw = new byte[charCount];
            Buffer.BlockCopy(_data, _readPos, raw, 0, charCount);
            _readPos += charCount;
            return Encoding.UTF8.GetString(raw);
        }
    }
}
