using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ProtocolsDNS
{
    public static class StringExtensions
    {
        public static IEnumerable<byte> UrlToBytes(this string url)
        {
            var encoding = Encoding.UTF8;

            foreach (var s in url.Split('.'))
            {
                var bytes = encoding.GetBytes(s);
                yield return (byte) bytes.Length;

                foreach (var b in bytes)
                    yield return b;
            }

            yield return 0;
        }
        
        public static IEnumerable<byte> IpToBytes(this string ip) => IPAddress.Parse(ip).GetAddressBytes();
    }
}