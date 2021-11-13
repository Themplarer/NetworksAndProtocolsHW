using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ProtocolsDNS
{
    internal class DnsResolver
    {
        private readonly UdpClient udpClient;
        private readonly IPAddress rootServerAIp = IPAddress.Parse("198.41.0.4");
        private readonly Dictionary<DnsQuestion, (DateTime LifeTimeEnd, byte[] LastMessage)> valueTuples;

        public DnsResolver()
        {
            udpClient = new UdpClient();
            valueTuples = new Dictionary<DnsQuestion, (DateTime, byte[])>();
        }

        public async Task<(DnsMessage Message, byte[] Bytes)> Resolve(DnsMessage request)
        {
            if (valueTuples.TryGetValue(request.Questions.First(), out var a) && a is var (lifeTimeEnd, messageBytes) &&
                lifeTimeEnd > DateTime.Now)
            {
                var bytesRes = request.Id.ToBytes().Concat(messageBytes[2..]).ToArray();
                return (DnsMessage.Parse(bytesRes), bytesRes);
            }

            var ip = rootServerAIp;
            byte[] bytes;
            var query = DnsMessage.CreateQuery(request.Id, request.Questions.First());
            DnsMessage message;

            do
            {
                var ipEndPoint = new IPEndPoint(ip, 53);
                var queryBytes = query.ToBytes();
                await udpClient.SendAsync(queryBytes, queryBytes.Length, ipEndPoint);
                bytes = (await udpClient.ReceiveAsync()).Buffer;
                message = DnsMessage.Parse(bytes);

                if (message.IsTruncated) message = await GetMessageByTcpAsync(ipEndPoint, queryBytes);

                if (!message.Answers.Any())
                {
                    var additionalData = message.AdditionalRecords
                        .FirstOrDefault(r => r.DnsQuestion.QuestionType == QuestionType.A)?
                        .Data;
                    if (additionalData is { }) ip = IPAddress.Parse(additionalData);
                    else
                    {
                        additionalData = message.Authorities
                            .FirstOrDefault(r => r.DnsQuestion.QuestionType == QuestionType.NS)?
                            .Data;
                        if (additionalData is { }) ip = IPAddress.Parse(await GetSubqueriedIpAsync(additionalData));
                    }
                }
            } while (!message.Answers.Any());

            valueTuples[request.Questions.First()] =
                (DateTime.Now + TimeSpan.FromSeconds(message.Answers.Min(a => a.TimeToLive)), bytes);
            return (message, bytes);
        }

        private static async Task<DnsMessage> GetMessageByTcpAsync(IPEndPoint ipEndPoint, byte[] queryBytes)
        {
            var networkStream = new TcpClient(ipEndPoint).GetStream();
            await networkStream.WriteAsync(queryBytes);
            var memory = new Memory<byte>(new byte[1024]);
            await networkStream.ReadAsync(memory);
            return DnsMessage.Parse(memory.ToArray());
        }

        private async Task<string> GetSubqueriedIpAsync(string resource)
        {
            const ushort subqueryId = 1337;
            var subquery = DnsMessage.CreateQuery(subqueryId,
                new DnsQuestion(resource, QuestionType.A, QuestionClass.Internet));
            var (subqueriedMessage, _) = await Resolve(subquery);
            return subqueriedMessage.Answers.First().Data;
        }
    }
}