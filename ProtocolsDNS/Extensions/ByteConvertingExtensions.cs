using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtocolsDNS
{
    public static class ByteConvertingExtensions
    {
        public static IEnumerable<byte> ToBytes(this ushort u)
        {
            var bytes = BitConverter.GetBytes(u);
            return BitConverter.IsLittleEndian ? bytes.Reverse() : bytes;
        }

        public static IEnumerable<byte> ToBytes(this uint u)
        {
            var bytes = BitConverter.GetBytes(u);
            return BitConverter.IsLittleEndian ? bytes.Reverse() : bytes;
        }

        public static ushort ToUInt16(this IEnumerable<byte> bytes)
        {
            var array = bytes.Take(2) is var number && BitConverter.IsLittleEndian
                ? number.Reverse().ToArray()
                : number as byte[] ?? number.ToArray();
            return BitConverter.ToUInt16(array, 0);
        }

        public static uint ToUInt32(this IEnumerable<byte> bytes)
        {
            var array = bytes.Take(4) is var number && BitConverter.IsLittleEndian
                ? number.Reverse().ToArray()
                : number as byte[] ?? number.ToArray();
            return BitConverter.ToUInt32(array, 0);
        }
    }
}