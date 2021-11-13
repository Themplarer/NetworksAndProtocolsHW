using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProtocolsDNS
{
    public class DnsMessage
    {
        public static DnsMessage CreateQuery(ushort id, DnsQuestion question, OpCode opCode = OpCode.Query,
            bool isRecursionDesired = false) =>
            new(id, false, opCode, false, false, isRecursionDesired, false, ResponseCode.NoError, new[] {question},
                Enumerable.Empty<DnsRecord>(), Enumerable.Empty<DnsRecord>(), Enumerable.Empty<DnsRecord>());

        public static DnsMessage CreateReply(ushort id, IEnumerable<DnsQuestion> questions,
            IEnumerable<DnsRecord> answers, IEnumerable<DnsRecord> authorities,
            IEnumerable<DnsRecord> additionalRecords, OpCode opCode = OpCode.Query, bool isAuthoritativeAnswer = false,
            bool isRecursionDesired = false, ResponseCode code = ResponseCode.NoError) =>
            new(id, true, opCode, isAuthoritativeAnswer, false, isRecursionDesired, true, code, questions, answers,
                authorities, additionalRecords);

        private readonly IReadOnlyCollection<DnsQuestion> questions;
        private readonly IReadOnlyCollection<DnsRecord> answers;
        private readonly IReadOnlyCollection<DnsRecord> authorities;
        private readonly IReadOnlyCollection<DnsRecord> additionalRecords;

        // ReSharper disable MemberCanBePrivate.Global
        public ushort Id { get; }
        public bool IsReply { get; }
        public OpCode OpCode { get; }
        public bool IsAuthoritativeAnswer { get; }
        public bool IsTruncated { get; }
        public bool IsRecursionDesired { get; }
        public bool IsRecursionAvailable { get; }
        public static byte Reserved => 0;
        public ResponseCode ResponseCode { get; }
        public IEnumerable<DnsQuestion> Questions => questions;
        public IEnumerable<DnsRecord> Answers => answers;
        public IEnumerable<DnsRecord> Authorities => authorities;
        public IEnumerable<DnsRecord> AdditionalRecords => additionalRecords;

        private DnsMessage(ushort id, bool isReply, OpCode opCode, bool isAuthoritativeAnswer, bool isTruncated,
            bool isRecursionDesired, bool isRecursionAvailable, ResponseCode responseCode,
            IEnumerable<DnsQuestion> questions, IEnumerable<DnsRecord> answers, IEnumerable<DnsRecord> authorities,
            IEnumerable<DnsRecord> additionalRecords)
        {
            (Id, IsReply, OpCode, IsAuthoritativeAnswer, IsTruncated, IsRecursionDesired, IsRecursionAvailable,
                    ResponseCode) =
                (id, isReply, opCode, isAuthoritativeAnswer, isTruncated, isRecursionDesired, isRecursionAvailable,
                    responseCode);

            this.questions = questions as DnsQuestion[] ?? questions.ToArray();
            this.answers = answers as DnsRecord[] ?? answers.ToArray();
            this.authorities = authorities as DnsRecord[] ?? authorities.ToArray();
            this.additionalRecords = additionalRecords as DnsRecord[] ?? additionalRecords.ToArray();
        }

        public byte[] ToBytes()
        {
            var firstHeaderByte = (byte) (IsReply.ToByte() << 7
                                          | (byte) OpCode
                                          | IsAuthoritativeAnswer.ToByte() << 2
                                          | IsTruncated.ToByte() << 1
                                          | IsRecursionDesired.ToByte());
            var secondHeaderByte = (byte) (IsRecursionAvailable.ToByte() << 7
                                           | Reserved << 4
                                           | (byte) ResponseCode);

            return Id.ToBytes()
                .Concat(new[] {firstHeaderByte, secondHeaderByte})
                .Concat(CollectionLengthTo2Bytes(questions))
                .Concat(CollectionLengthTo2Bytes(answers))
                .Concat(CollectionLengthTo2Bytes(authorities))
                .Concat(CollectionLengthTo2Bytes(additionalRecords))
                .Concat(questions.SelectMany(q => q.ToBytes()))
                .Concat(answers.SelectMany(a => a.ToBytes()))
                .Concat(authorities.SelectMany(a => a.ToBytes()))
                .Concat(additionalRecords.SelectMany(a => a.ToBytes()))
                .ToArray();
        }

        private static IEnumerable<byte> CollectionLengthTo2Bytes<T>(IEnumerable<T> enumerable) =>
            ((ushort) enumerable.Count()).ToBytes();

        public static DnsMessage Parse(IEnumerable<byte> bytes)
        {
            var array = bytes as byte[] ?? bytes.ToArray();

            var id = array.ToUInt16();

            var isReply = array[2] >> 7 > 0;
            var opCode = (OpCode) (array[2] & 0b_0111_1000);
            var isAuthoritativeAnswer = (array[2] >> 2 & 1) > 1;
            var isTruncated = (array[2] >> 1 & 1) > 1;
            var isRecursionDesired = (array[2] & 1) > 1;

            var isRecursionAvailable = array[3] >> 7 > 0;
            var responseCode = (ResponseCode) (array[3] & 0b1111);

            var questionCount = array[4..].ToUInt16();
            var answerCount = array[6..].ToUInt16();
            var authoritiesCount = array[8..].ToUInt16();
            var additionalRecordsCount = array[10..].ToUInt16();

            var (questions, offset1) = ParseCollection(array, 12, questionCount, ParseQuestionWrapper);
            var (answers, offset2) = ParseCollection(array, offset1, answerCount, ParseRecordWrapper);
            var (authorities, offset3) = ParseCollection(array, offset2, authoritiesCount, ParseRecordWrapper);
            var (additionalRecords, _) = ParseCollection(array, offset3, additionalRecordsCount, ParseRecordWrapper);

            return new DnsMessage(id, isReply, opCode, isAuthoritativeAnswer, isTruncated, isRecursionDesired,
                isRecursionAvailable, responseCode, questions, answers, authorities, additionalRecords);
        }

        private static (DnsQuestion Value, int Offset) ParseQuestionWrapper(IEnumerable<byte> bytes,
            Func<int, string> referenceResolver) =>
            (DnsQuestion.Parse(bytes, referenceResolver, out var lastByteIndex), lastByteIndex);

        private static (DnsRecord Value, int Offset) ParseRecordWrapper(IEnumerable<byte> bytes,
            Func<int, string> referenceResolver) =>
            (DnsRecord.Parse(bytes, referenceResolver, out var lastByteIndex), lastByteIndex);

        private static (IEnumerable<T> Result, int Offset) ParseCollection<T>(byte[] bytes, int initialOffset,
            ushort amount, Func<IEnumerable<byte>, Func<int, string>, (T Value, int Offset)> func)
        {
            var referenceResolver = new Func<int, string>(i =>
            {
                var strings = new List<string>();
                var builder = new StringBuilder();
                var reference = i;
                while (reference != 0)
                {
                    string ans;
                    (ans, reference) = bytes[reference..].ToUrl(out _);
                    strings.Add(ans);
                    builder.Append(ans);
                }

                return string.Join('.', strings);
            });

            var totalOffset = initialOffset;
            var res = Enumerable.Range(0, amount)
                .Select(_ =>
                {
                    var (value, curOffset) = func(bytes[totalOffset..], referenceResolver);
                    totalOffset += curOffset;
                    return value;
                })
                .ToArray();
            return (res, totalOffset);
        }
    }
}