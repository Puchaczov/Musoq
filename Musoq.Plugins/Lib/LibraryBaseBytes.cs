using Musoq.Plugins.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [BindableMethod]
        public byte[] GetBytes(string content)
        {
            return Encoding.UTF8.GetBytes(content);
        }

        [BindableMethod]
        public byte[] GetBytes(string content, int length, int offset)
        {
            return Encoding.UTF8.GetBytes(content.Substring(offset, length));
        }

        [BindableMethod]
        public byte[] GetBytes(char character)
        {
            return BitConverter.GetBytes(character);
        }

        [BindableMethod]
        public byte[] GetBytes(bool bit)
        {
            return BitConverter.GetBytes(bit);
        }

        [BindableMethod]
        public byte[] GetBytes(long value)
        {
            return BitConverter.GetBytes(value);
        }

        [BindableMethod]
        public byte[] GetBytes(int value)
        {
            return BitConverter.GetBytes(value);
        }

        [BindableMethod]
        public byte[] GetBytes(short value)
        {
            return BitConverter.GetBytes(value);
        }

        [BindableMethod]
        public byte[] GetBytes(ulong value)
        {
            return BitConverter.GetBytes(value);
        }

        [BindableMethod]
        public byte[] GetBytes(ushort value)
        {
            return BitConverter.GetBytes(value);
        }

        [BindableMethod]
        public byte[] GetBytes(uint value)
        {
            return BitConverter.GetBytes(value);
        }

        [BindableMethod]
        public byte[] GetBytes(decimal value)
        {
            var bytes = new List<byte>();

            foreach (var integerValue in decimal.GetBits(value))
                bytes.AddRange(BitConverter.GetBytes(integerValue));

            return bytes.ToArray();
        }
    }
}
