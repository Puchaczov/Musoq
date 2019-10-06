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
            if (content == null)
                return null;

            return Encoding.UTF8.GetBytes(content);
        }

        [BindableMethod]
        public byte[] GetBytes(string content, int length, int offset)
        {
            if (content == null)
                return null;

            return Encoding.UTF8.GetBytes(content.Substring(offset, length));
        }

        [BindableMethod]
        public byte[] GetBytes(char? character)
        {
            if (character == null)
                return null;

            return BitConverter.GetBytes(character.Value);
        }

        [BindableMethod]
        public byte[] GetBytes(bool? bit)
        {
            if (bit == null)
                return null;

            return BitConverter.GetBytes(bit.Value);
        }

        [BindableMethod]
        public byte[] GetBytes(long? value)
        {
            if (value == null)
                return null;

            return BitConverter.GetBytes(value.Value);
        }

        [BindableMethod]
        public byte[] GetBytes(int? value)
        {
            if (value == null)
                return null;

            return BitConverter.GetBytes(value.Value);
        }

        [BindableMethod]
        public byte[] GetBytes(short? value)
        {
            if (value == null)
                return null;

            return BitConverter.GetBytes(value.Value);
        }

        [BindableMethod]
        public byte[] GetBytes(ulong? value)
        {
            if (value == null)
                return null;

            return BitConverter.GetBytes(value.Value);
        }

        [BindableMethod]
        public byte[] GetBytes(ushort? value)
        {
            if (value == null)
                return null;

            return BitConverter.GetBytes(value.Value);
        }

        [BindableMethod]
        public byte[] GetBytes(uint? value)
        {
            if (value == null)
                return null;

            return BitConverter.GetBytes(value.Value);
        }

        [BindableMethod]
        public byte[] GetBytes(decimal? value)
        {
            if (value == null)
                return null;

            var bytes = new List<byte>();

            foreach (var integerValue in decimal.GetBits(value.Value))
                bytes.AddRange(BitConverter.GetBytes(integerValue));

            return bytes.ToArray();
        }

        [BindableMethod]
        public byte[] GetBytes(double? value)
        {
            if (value == null)
                return null;

            return GetBytes(BitConverter.DoubleToInt64Bits(value.Value));
        }

        [BindableMethod]
        public byte[] GetBytes(float? value)
        {
            if (value == null)
                return null;

            return BitConverter.GetBytes(value.Value);
        }
    }
}
