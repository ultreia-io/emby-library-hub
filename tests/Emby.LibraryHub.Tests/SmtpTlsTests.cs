using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.LibraryHub.Core;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Xunit;

namespace Emby.LibraryHub.Tests;

public sealed class SmtpTlsTests
{
    [Theory]
    [InlineData(465, true, true)]
    [InlineData(465, false, true)]
    [InlineData(587, true, false)]
    public async Task AuthenticatingAndSendingUsesRequiredTlsMode(int configuredPort, bool startTls, bool implicitTls)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var certificate = CreateCertificate();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var receive = ReceiveAsync(listener, certificate, implicitTls, deadline.Token);
        var localPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        var options = Options(configuredPort, startTls);
        var sender = new SmtpDigestSender(() => new RedirectedClient(configuredPort, localPort, certificate));
        await sender.SendAsync(new DigestMessage
        {
            Id = "tls-test", Subject = "Médiathèque", Body = "Ajouts :\n• Éléphant"
        }, options, deadline.Token);
        var received = await receive;
        Assert.True(received.AuthenticatedOverTls);
        using var data = new MemoryStream(Encoding.ASCII.GetBytes(received.Data));
        using var message = await MimeMessage.LoadAsync(data, deadline.Token);
        Assert.Equal("Médiathèque", message.Subject);
        Assert.Equal("Ajouts :\n• Éléphant", (message.TextBody ?? throw new InvalidDataException("Missing plain-text body")).Replace("\r\n", "\n").TrimEnd('\n'));
        Assert.Equal("tls-test@emby-library-hub.local", message.MessageId);
    }

    [Fact]
    public async Task UntrustedServerCertificateIsRejected()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var certificate = CreateCertificate();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var receive = ReceiveAsync(listener, certificate, true, deadline.Token);
        var localPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        // No validation callback: this exercises MailKit's normal certificate checks.
        var sender = new SmtpDigestSender(() => new RedirectedClient(465, localPort));
        await Assert.ThrowsAsync<SslHandshakeException>(() =>
            sender.SendAsync(new DigestMessage(), Options(465, true), deadline.Token));
        Assert.False((await receive).AuthenticatedOverTls);
    }

    [Fact]
    public async Task AuthenticationCannotFallBackToCleartext()
    {
        var created = false;
        var sender = new SmtpDigestSender(() => { created = true; return new SmtpClient(); });
        await Assert.ThrowsAsync<ArgumentException>(() =>
            sender.SendAsync(new DigestMessage(), Options(587, false), default));
        Assert.False(created);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExistingPort465BackupsPassValidationWithoutChangingTheirTlsFlag(bool legacyFlag)
    {
        var options = Options(465, legacyFlag);
        options.Validate();
        SmtpDigestSender.Validate(options);
        Assert.Equal(legacyFlag, options.UseStartTls);
    }

    private static DigestOptions Options(int port, bool startTls) => new()
    {
        SmtpHost = "localhost", SmtpPort = port, UseStartTls = startTls,
        Sender = "sender@example.test", Recipient = "recipient@example.test",
        SmtpUsername = "test-user", SmtpPassword = "test-password"
    };

    private sealed class RedirectedClient : SmtpClient
    {
        private readonly int expectedPort;
        private readonly int localPort;
        public RedirectedClient(int expectedPort, int localPort, X509Certificate2? trustedTestCertificate = null)
        {
            this.expectedPort = expectedPort;
            this.localPort = localPort;
            // Only local tests pin their self-signed certificate. Production never sets this callback.
            if (trustedTestCertificate != null)
                ServerCertificateValidationCallback = (_, certificate, _, _) =>
                    certificate?.GetCertHashString() == trustedTestCertificate.GetCertHashString();
        }
        public override Task ConnectAsync(string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto,
            CancellationToken cancellationToken = default)
        {
            Assert.Equal(expectedPort, port);
            Assert.Equal(expectedPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, options);
            return base.ConnectAsync(host, localPort, options, cancellationToken);
        }
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder(); names.AddDnsName("localhost");
        request.CertificateExtensions.Add(names.Build());
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    private sealed class Capture
    {
        public bool AuthenticatedOverTls { get; set; }
        public string Data { get; set; } = "";
    }

    private static async Task<Capture> ReceiveAsync(TcpListener listener, X509Certificate2 certificate,
        bool implicitTls, CancellationToken token)
    {
        var capture = new Capture();
        using var client = await listener.AcceptTcpClientAsync(token);
        Stream stream = client.GetStream();
        StreamReader? reader = null;
        StreamWriter? writer = null;
        SslStream? tls = null;
        async Task UpgradeAsync()
        {
            reader?.Dispose(); writer?.Dispose();
            tls = new SslStream(stream, true);
            await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
            {
                ServerCertificate = certificate,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }, token);
            stream = tls;
        }
        void OpenText()
        {
            reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
            writer = new StreamWriter(stream, Encoding.ASCII, 1024, true) { AutoFlush = true, NewLine = "\r\n" };
        }
        try
        {
            if (implicitTls) await UpgradeAsync();
            OpenText();
            await writer!.WriteLineAsync("220 localhost TLS test sink");
            var body = new StringBuilder();
            var data = false;
            while (await reader!.ReadLineAsync(token) is { } line)
            {
                if (data)
                {
                    if (line == ".") { data = false; await writer!.WriteLineAsync("250 accepted"); }
                    else body.AppendLine(line.StartsWith("..") ? line[1..] : line);
                }
                else if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase))
                    await writer!.WriteLineAsync(tls == null ? "250-localhost\r\n250 STARTTLS" : "250-localhost\r\n250 AUTH PLAIN");
                else if (line == "STARTTLS")
                {
                    await writer!.WriteLineAsync("220 ready for TLS");
                    await UpgradeAsync(); OpenText();
                }
                else if (line.StartsWith("AUTH PLAIN "))
                {
                    Assert.NotNull(tls);
                    Assert.Equal("\0test-user\0test-password", Encoding.UTF8.GetString(Convert.FromBase64String(line[11..])));
                    capture.AuthenticatedOverTls = tls!.IsAuthenticated && tls.IsEncrypted;
                    await writer!.WriteLineAsync("235 authenticated");
                }
                else if (line == "DATA") { data = true; await writer!.WriteLineAsync("354 send data"); }
                else if (line == "QUIT") { await writer!.WriteLineAsync("221 goodbye"); break; }
                else await writer!.WriteLineAsync("250 OK");
            }
            capture.Data = body.ToString();
        }
        catch (IOException) when (!capture.AuthenticatedOverTls) { }
        catch (System.Security.Authentication.AuthenticationException) when (!capture.AuthenticatedOverTls) { }
        finally { reader?.Dispose(); writer?.Dispose(); tls?.Dispose(); }
        return capture;
    }
}
