using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ProtocolsDNS
{
    public static class ByteCollectionExtensions
    {
        public static (string Parsed, int Reference) ToUrl(this IEnumerable<byte> data, out int lastByteIndex)
        {
            var isRef = false;
            var refBytes = new byte[2];

            var encoding = Encoding.UTF8;
            var dotEncoded = encoding.GetBytes(".").First();
            var counter = 0;
            var bytesList = new List<byte>();
            lastByteIndex = 0;

            foreach (var b in data)
            {
                lastByteIndex++;
                if (counter == 0)
                {
                    if (isRef)
                    {
                        refBytes[1] = b;
                        break;
                    }

                    var a = b >> 6;
                    if (b >> 6 == 0b11)
                    {
                        refBytes[0] = (byte) (b & 0b11_1111);
                        isRef = true;
                    }
                    else
                    {
                        counter = b;
                        if (counter > 0) bytesList.Add(dotEncoded);
                        else break;
                    }
                }
                else
                {
                    bytesList.Add(b);
                    counter--;
                }
            }

            return (encoding.GetString(bytesList.Skip(1).ToArray()), refBytes.ToUInt16());
        }

        public static string ToIpString(this IEnumerable<byte> data) => new IPAddress(data.ToArray()).ToString();
    }
}