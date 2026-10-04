using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Emby.LibraryHub.Core;

public sealed class SmtpDigestSender : IDigestSender
{
    private readonly Func<SmtpClient> createClient;

    public SmtpDigestSender() : this(() => new SmtpClient()) { }

    // Allows local protocol tests without weakening production certificate validation.
    public SmtpDigestSender(Func<SmtpClient> createClient) => this.createClient = createClient;

    public static void Validate(DigestOptions options)
    {
        ValidateTransport(options);
        _ = MailboxAddress.Parse(options.Recipient);
    }

    public static void ValidateTransport(DigestOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SmtpHost)) throw new ArgumentException("SMTP host is required.");
        if (options.SmtpPort < 1 || options.SmtpPort > 65535)
            throw new ArgumentException("Use an SMTP port between 1 and 65535.");
        if (options.SmtpPort != 465 && !options.UseStartTls && !string.IsNullOrEmpty(options.SmtpUsername))
            throw new ArgumentException("SMTP authentication requires TLS: use port 465 or enable STARTTLS.");
        _ = MailboxAddress.Parse(options.Sender);
    }

    public async Task SendAsync(DigestMessage message, DigestOptions options, CancellationToken cancellationToken)
    {
        Validate(options);
        cancellationToken.ThrowIfCancellationRequested();
        using (var mail = CreateMessage(message, options))
        using (var client = createClient())
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            client.Timeout = 30000;
            var security = options.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect
                : options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(options.SmtpHost, options.SmtpPort, security, timeout.Token).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(options.SmtpUsername))
                await client.AuthenticateAsync(options.SmtpUsername, options.SmtpPassword, timeout.Token).ConfigureAwait(false);
            await client.SendAsync(mail, timeout.Token).ConfigureAwait(false);
            // SMTP has accepted the message. Closing locally avoids retrying an accepted
            // message just because the peer fails or stalls during a subsequent QUIT.
            await client.DisconnectAsync(false, CancellationToken.None).ConfigureAwait(false);
        }
    }

    public static MimeMessage CreateMessage(DigestMessage message, DigestOptions options)
    {
        var mail = new MimeMessage();
        mail.From.Add(MailboxAddress.Parse(options.Sender));
        mail.To.Add(MailboxAddress.Parse(options.Recipient));
        mail.Subject = message.Subject;
        mail.MessageId = message.Id + "@emby-library-hub.local";
        var body = new TextPart("plain");
        body.SetText(Encoding.UTF8, message.Body.Replace("\r\n", "\n").Replace("\n", "\r\n"));
        mail.Body = body;
        return mail;
    }
}
