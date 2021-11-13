using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtocolsDNS
{
    public class DnsQuestion : IEquatable<DnsQuestion>
    {
        public string Name { get; }
        public QuestionType QuestionType { get; }
        public QuestionClass QuestionClass { get; }
        private readonly byte[] byteRepresentation;

        public DnsQuestion(string name, QuestionType questionType, QuestionClass questionClass,
            byte[] byteRepresentation = null)
        {
            (Name, QuestionType, QuestionClass) = (name, questionType, questionClass);
            this.byteRepresentation = byteRepresentation ?? Name.UrlToBytes()
                .Concat(((ushort) QuestionType).ToBytes())
                .Concat(((ushort) QuestionClass).ToBytes())
                .ToArray();
        }

        public byte[] ToBytes() => byteRepresentation;

        public static DnsQuestion Parse(IEnumerable<byte> bytes, Func<int, string> referenceResolver,
            out int lastByteIndex)
        {
            var byteArray = bytes as byte[] ?? bytes.ToArray();
            var (name, reference) = byteArray.ToUrl(out lastByteIndex);
            name = reference == 0
                ? name
                : referenceResolver(reference) is var resolved && string.IsNullOrEmpty(name)
                    ? resolved
                    : $"{name}.{resolved}";
            var counter = 0;
            var numbersArray = new byte[4];

            foreach (var b in byteArray.Skip(lastByteIndex).Take(4))
            {
                lastByteIndex++;
                numbersArray[counter++] = b;
            }

            return new DnsQuestion(name, (QuestionType) numbersArray.ToUInt16(),
                (QuestionClass) numbersArray[2..].ToUInt16());
        }

        public bool Equals(DnsQuestion other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && QuestionType == other.QuestionType && QuestionClass == other.QuestionClass;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is DnsQuestion question && Equals(question);
        }

        public override int GetHashCode() => HashCode.Combine(Name, (int) QuestionType, (int) QuestionClass);
    }
}