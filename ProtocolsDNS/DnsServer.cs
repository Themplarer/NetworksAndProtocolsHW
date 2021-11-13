using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProtocolsDNS
{
    internal class DnsServer
    {
        private readonly UdpClient udpClient;
        private readonly Regex regex;
        private readonly DnsResolver dnsResolver;

        private DnsServer()
        {
            var ipEndPoint = new IPEndPoint(IPAddress.Loopback, 53);
            udpClient = new UdpClient(ipEndPoint);
            regex = new Regex(@"(.*)\.multiply\..*");
            dnsResolver = new DnsResolver();
        }

        // ReSharper disable once FunctionNeverReturns
        private async Task EventLoop()
        {
            while (true)
            {
                try
                {
                    var udpReceiveResult = await udpClient.ReceiveAsync();
                    var dnsMessage = DnsMessage.Parse(udpReceiveResult.Buffer);

                    byte[] bytes;
                    if (dnsMessage.Questions
                        .Select(q => (Question: q, Match: regex.Match(q.Name)))
                        .Where(m => m.Match.Success)
                        .ToArray() is { } a && a.Any() && a.First() is var (question, match))
                    {
                        bytes = GetMultiplyDnsMessage(dnsMessage.Id, dnsMessage.Questions, question,
                            match.Groups[1].Value);
                        await udpClient.SendAsync(bytes, bytes.Length, udpReceiveResult.RemoteEndPoint);
                    }
                    else
                        Task.Run<Action>(async () =>
                        {
                            (_, bytes) = await dnsResolver.Resolve(dnsMessage);
                            await udpClient.SendAsync(bytes, bytes.Length, udpReceiveResult.RemoteEndPoint);
                            return null;
                        }).Start();
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static byte[] GetMultiplyDnsMessage(ushort id, IEnumerable<DnsQuestion> questions,
            DnsQuestion mainQuestion, string query)
        {
            var lastNum = query.Split('.').Aggregate(1, (res, part) => res * int.Parse(part) % 256);
            var resString = $"127.0.0.{lastNum}";
            var dnsRecord = new DnsRecord(mainQuestion, 3600, 4, resString);
            var reply = DnsMessage.CreateReply(id, questions, new[] {dnsRecord}, Array.Empty<DnsRecord>(),
                Array.Empty<DnsRecord>());

            return reply.ToBytes();
        }

        private static async Task Main() => await new DnsServer().EventLoop();
    }
}