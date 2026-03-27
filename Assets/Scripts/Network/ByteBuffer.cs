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
        /// Strings: uint16 con cantidad de codepoints UTF-8, luego los bytes UTF-8.
        /// El servidor escribe el número de codepoints (no bytes), por lo que para
        /// caracteres multibyte (tildes, ñ) hay que escanear byte a byte.
        /// </summary>
        public string ReadString()
        {
            ushort charCount = ReadShort();
            if (charCount == 0) return string.Empty;

            // Calcular cuántos bytes corresponden a charCount codepoints UTF-8
            int byteCount = 0;
            int pos = _readPos;
            for (int i = 0; i < charCount && pos < _data.Length; i++)
            {
                byte b = _data[pos];
                int seqLen = b < 0x80 ? 1 : b < 0xE0 ? 2 : b < 0xF0 ? 3 : 4;
                byteCount += seqLen;
                pos += seqLen;
            }

            if (byteCount > _data.Length - _readPos) return string.Empty;

            byte[] raw = new byte[byteCount];
            Buffer.BlockCopy(_data, _readPos, raw, 0, byteCount);
            _readPos += byteCount;
            return Encoding.UTF8.GetString(raw);
        }
    }
}
