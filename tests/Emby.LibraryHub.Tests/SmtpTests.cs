using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.LibraryHub.Core;
using Xunit;

namespace Emby.LibraryHub.Tests;

public sealed class SmtpTests
{
    [Fact]
    public async Task SendsPlainTextToLocalSmtpSinkWithoutExternalDelivery()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var receive = ReceiveAsync(listener, deadline.Token);
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var options = new DigestOptions
        {
            SmtpHost = "127.0.0.1", SmtpPort = port, UseStartTls = false,
            Sender = "sender@example.test", Recipient = "recipient@example.test"
        };
        await new SmtpDigestSender().SendAsync(new DigestMessage
        {
            Id = "smtp-test", Subject = "Médiathèque", Body = "Ajouts :\n• Éléphant\nhttps://media.example.test/"
        }, options, deadline.Token);
        var captured = await receive;
        Assert.Contains("Content-Type: text/plain; charset=utf-8", captured, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Message-ID: <smtp-test@emby-library-hub.local>", captured, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("To: recipient@example.test", captured);
        Assert.DoesNotContain("text/html", captured);
        Assert.Contains("Content-Transfer-Encoding:", captured);
    }

    private static async Task<string> ReceiveAsync(TcpListener listener, CancellationToken token)
    {
        using var client = await listener.AcceptTcpClientAsync(token);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true, NewLine = "\r\n" };
        await writer.WriteLineAsync("220 localhost test sink");
        var content = new StringBuilder();
        var data = false;
        while (await reader.ReadLineAsync(token) is { } line)
        {
            if (data)
            {
                if (line == ".") { data = false; await writer.WriteLineAsync("250 accepted"); }
                else content.AppendLine(line);
            }
            else if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase)) await writer.WriteLineAsync("250 localhost");
            else if (line == "DATA") { data = true; await writer.WriteLineAsync("354 send data"); }
            else if (line == "QUIT") { await writer.WriteLineAsync("221 goodbye"); break; }
            else await writer.WriteLineAsync("250 OK");
        }
        return content.ToString();
    }
}
