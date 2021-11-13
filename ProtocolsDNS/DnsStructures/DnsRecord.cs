using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProtocolsDNS
{
    public class DnsRecord
    {
        public DnsQuestion DnsQuestion { get; }
        public uint TimeToLive { get; }
        public ushort DataLength { get; }
        public string Data { get; }
        private readonly byte[] byteRepresentation;

        public DnsRecord(DnsQuestion dnsQuestion, uint timeToLive, ushort dataLength, string data,
            byte[] byteRepresentation = null)
        {
            (DnsQuestion, TimeToLive, DataLength, Data) = (dnsQuestion, timeToLive, dataLength, data);
            this.byteRepresentation = byteRepresentation ?? DnsQuestion.ToBytes()
                .Concat(TimeToLive.ToBytes())
                .Concat(DataLength.ToBytes())
                .Concat(EncodeData())
                .ToArray();
        }

        public byte[] ToBytes() => byteRepresentation;

        private IEnumerable<byte> EncodeData() =>
            DnsQuestion.QuestionType switch
            {
                QuestionType.A => Data.IpToBytes(),
                QuestionType.AAAA => Data.IpToBytes(),
                QuestionType.NS => Data.UrlToBytes(),
                QuestionType.CNAME => Data.UrlToBytes(),
                _ => Encoding.UTF8.GetBytes(Data)
            };

        public static DnsRecord Parse(IEnumerable<byte> data, Func<int, string> referenceResolver,
            out int lastByteIndex)
        {
            var bytes = data as byte[] ?? data.ToArray();
            var dnsQuestion = DnsQuestion.Parse(bytes, referenceResolver, out lastByteIndex);
            bytes = bytes[lastByteIndex..];
            var timeToLive = bytes.ToUInt32();
            var dataLength = bytes[4..].ToUInt16();
            lastByteIndex += 6 + dataLength;

            var (name, reference) = ParseData(bytes[6..].Take(dataLength), dnsQuestion.QuestionType);
            name = reference == 0
                ? name
                : referenceResolver(reference) is var resolved && string.IsNullOrEmpty(name)
                    ? resolved
                    : $"{name}.{resolved}";
            return new DnsRecord(dnsQuestion, timeToLive, dataLength, name);
        }

        private static (string Parsed, int Reference) ParseData(IEnumerable<byte> data, QuestionType questionType) =>
            questionType switch
            {
                QuestionType.A => (data.ToIpString(), 0),
                QuestionType.AAAA => (data.ToIpString(), 0),
                QuestionType.NS => data.ToUrl(out _),
                QuestionType.CNAME => data.ToUrl(out _),
                _ => (Encoding.UTF8.GetString(data as byte[] ?? data.ToArray()), 0)
            };
    }
}