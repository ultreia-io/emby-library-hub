using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;

namespace Emby.LibraryHub.Core;

public sealed class Subscriber
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public long EmbyUserId { get; set; }
    public long PendingEmbyUserId { get; set; }
    public string Email { get; set; } = "";
    public string Language { get; set; } = "fr";
    public bool Confirmed { get; set; }
    public string PendingLanguage { get; set; } = "fr";
    public string ConfirmationHash { get; set; } = "";
    public DateTimeOffset Expires { get; set; }
    public string UnsubscribeToken { get; set; } = "";
    public string NextDay { get; set; } = "";
    public DigestMessage? Pending { get; set; }
    public DateTimeOffset RetryAfter { get; set; }
    public DateTimeOffset? LastDeliveredAt { get; set; }
    public string LastError { get; set; } = "";
}

public sealed class SubscriptionState
{
    public int Version { get; set; } = 1;
    public string FormKey { get; set; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public List<Subscriber> Subscribers { get; set; } = new();
}

public sealed class SubscriptionStore
{
    private readonly string path;
    private readonly Func<string, SubscriptionState> read;
    private readonly Func<SubscriptionState, string> write;
    public SubscriptionStore(string path, Func<string, SubscriptionState> read, Func<SubscriptionState, string> write)
    { this.path = path; this.read = read; this.write = write; }
    public SubscriptionState Load()
    {
        if (!File.Exists(path))
        {
            if (File.Exists(path + ".bak")) throw new InvalidDataException("Subscriber data is missing; restore its backup.");
            var created = new SubscriptionState(); Save(created); return created;
        }
        var state = read(File.ReadAllText(path, Encoding.UTF8));
        if (state == null || state.Version != 1 || state.FormKey.Length != 64 || state.Subscribers == null)
            throw new InvalidDataException("Invalid subscriber data; restore its backup.");
        return state;
    }
    public void Save(SubscriptionState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                var bytes = Encoding.UTF8.GetBytes(write(state)); stream.Write(bytes); stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak"); else File.Move(temporary, path);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

// All operations load committed data under one gate. Delivery releases it between
// recipients so unsubscribe can cancel any remaining mail, including retry queues.
public sealed class SubscriptionService
{
    private readonly SubscriptionStore store;
    private readonly SemaphoreSlim gate = new(1, 1);
    public SubscriptionService(SubscriptionStore store) => this.store = store;
    private static string Secret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static bool Equal(string a, string b) => a.Length == b.Length && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    private static bool ValidToken(string? token) => token != null && token.Length == 64 && token.All(Uri.IsHexDigit);
    private static string Mac(SubscriptionState state, string value) => Convert.ToHexString(HMACSHA256.HashData(Convert.FromHexString(state.FormKey), Encoding.UTF8.GetBytes(value)));

    public async Task<string> FormTokenAsync(string purpose, DateTimeOffset now)
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var expires = now.AddMinutes(30).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            return expires + "." + Mac(store.Load(), purpose + ":" + expires);
        }
        finally { gate.Release(); }
    }
    private static bool ValidForm(SubscriptionState state, string? form, string purpose, DateTimeOffset now)
    {
        var parts = (form ?? "").Split('.');
        return parts.Length == 2 && long.TryParse(parts[0], out var expiry) && expiry > now.ToUnixTimeSeconds() &&
            expiry <= now.AddMinutes(31).ToUnixTimeSeconds() && Equal(parts[1], Mac(state, purpose + ":" + parts[0]));
    }

    public async Task<bool> RequestAsync(long userId, string email, string language, string form,
        string publicPath, DateTimeOffset now, DigestOptions options, IDigestSender sender, CancellationToken token)
    {
        if (userId <= 0) throw new UnauthorizedAccessException("An Emby account is required.");
        if (!options.SubscriptionsEnabled) throw new InvalidOperationException("Subscriptions are disabled.");
        if (language != "fr" && language != "en") throw new ArgumentException("Choose French or English.", nameof(language));
        email = (email ?? "").Trim();
        if (email.Length > 254 || email.Length == 0 || email.Any(char.IsControl) ||
            !MailboxAddress.TryParse(email, out var parsed) || parsed.Address != email || !email.Contains('@'))
            throw new ArgumentException("Enter one valid email address.", nameof(email));
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var state = store.Load();
            if (!ValidForm(state, form, "subscribe:" + userId, now)) throw new ArgumentException("This form expired. Reload the subscription page.", nameof(form));
            state.Subscribers.RemoveAll(s => !s.Confirmed && s.Expires < now.AddDays(-1));
            var subscriber = state.Subscribers.SingleOrDefault(s => string.Equals(s.Email, email, StringComparison.OrdinalIgnoreCase));
            if (subscriber == null)
            {
                subscriber = new Subscriber { Email = email, UnsubscribeToken = Secret() }; state.Subscribers.Add(subscriber);
            }
            // Never transfer an address between accounts, even with a new confirmation request.
            if (subscriber.EmbyUserId > 0 && subscriber.EmbyUserId != userId) return false;
            if (!subscriber.Confirmed && subscriber.PendingEmbyUserId > 0 && subscriber.PendingEmbyUserId != userId) return false;
            subscriber.PendingEmbyUserId = userId;
            var secret = Secret();
            subscriber.ConfirmationHash = Hash(secret); subscriber.Expires = now.AddHours(24); subscriber.PendingLanguage = language;
            store.Save(state); // Persist before SMTP; links survive restart and SMTP uncertainty.
            var link = options.PublicServerUrl.TrimEnd('/') + "/" + publicPath + (publicPath.Contains("?") ? "&" : "?") + "action=confirm&token=" + secret + "&lang=" + language;
            var fr = language == "fr";
            var message = new DigestMessage
            {
                Subject = fr ? "Confirmez votre abonnement aux nouvelles de la médiathèque" : "Confirm your library updates subscription",
                Body = (fr ? "Pour recevoir les rapports quotidiens, ouvrez ce lien et confirmez votre abonnement :" :
                    "To receive daily reports, open this link and confirm your subscription:") + "\n\n" + link + "\n\n" +
                    (fr ? "Ce lien expire dans 24 heures. Si vous n’avez rien demandé, ignorez ce message. Aucun abonnement ne sera activé." :
                    "This link expires in 24 hours. If you did not request this, ignore this message. No subscription will be activated.")
            };
            await sender.SendAsync(message, options.ForRecipient(email, language), token).ConfigureAwait(false);
            return true;
        }
        finally { gate.Release(); }
    }

    // GET only renders confirmation. Mail scanners cannot subscribe or unsubscribe.
    public async Task<bool> ApplyAsync(long userId, string action, string token, string form, DateTimeOffset now, DigestOptions options)
    {
        if (userId <= 0 || !ValidToken(token)) return false;
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var state = store.Load();
            if (!ValidForm(state, form, action + ":" + userId + ":" + token, now)) return false;
            if (action == "unsubscribe")
            {
                state.Subscribers.RemoveAll(s => s.EmbyUserId == userId && Equal(s.UnsubscribeToken, token));
                store.Save(state); return true; // Idempotent, including already-removed subscriptions.
            }
            if (action != "confirm" || !options.SubscriptionsEnabled) return false;
            var hash = Hash(token);
            var subscriber = state.Subscribers.SingleOrDefault(s => s.PendingEmbyUserId == userId && Equal(s.ConfirmationHash, hash) && s.Expires > now);
            if (subscriber == null) return false;
            if (!subscriber.Confirmed || subscriber.EmbyUserId == 0)
                subscriber.NextDay = TimeZoneInfo.ConvertTime(now, options.Validate()).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (subscriber.EmbyUserId == 0)
            {
                // Old public-list records cannot send until a signed-in owner confirms anew.
                subscriber.Pending = null; subscriber.RetryAfter = default; subscriber.LastError = "";
                subscriber.UnsubscribeToken = Secret();
            }
            subscriber.EmbyUserId = userId; subscriber.PendingEmbyUserId = 0;
            subscriber.Confirmed = true; subscriber.Language = subscriber.PendingLanguage;
            subscriber.ConfirmationHash = "";
            store.Save(state); return true;
        }
        finally { gate.Release(); }
    }

    public async Task<bool> DeliverNextAsync(DateTimeOffset now, DigestOptions options, string publicPath,
        Func<long, DateTime, DigestOptions, List<DigestMessage>> messages, IDigestSender sender, CancellationToken token,
        Func<long, bool> accountAllowed)
    {
        if (!options.SubscriptionsEnabled || !options.DeliveryEnabled) return false;
        var local = TimeZoneInfo.ConvertTime(now, options.Validate());
        if (local.TimeOfDay < new TimeSpan(options.DeliveryHour, options.DeliveryMinute, 0)) return false;
        var today = local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var state = store.Load();
            var subscriber = state.Subscribers.FirstOrDefault(s => s.Confirmed && s.EmbyUserId > 0 && accountAllowed(s.EmbyUserId) && s.RetryAfter <= now &&
                (!s.LastDeliveredAt.HasValue || TimeZoneInfo.ConvertTime(s.LastDeliveredAt.Value, options.Validate()).Date < local.Date) &&
                (s.Pending != null || string.CompareOrdinal(s.NextDay, today) < 0));
            if (subscriber == null) return false;
            var deliveryOptions = options.ForRecipient(subscriber.Email, subscriber.Language);
            // Rebuild even a retry from current permissions. A saved plaintext body is never authorization.
            var first = DateTime.ParseExact(subscriber.Pending?.Day ?? subscriber.NextDay, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var fresh = messages(subscriber.EmbyUserId, first, deliveryOptions).FirstOrDefault();
            if (fresh == null)
            {
                subscriber.Pending = null; subscriber.NextDay = today; subscriber.LastError = "";
                store.Save(state); return true;
            }
            if (subscriber.Pending?.Day == fresh.Day) fresh.Id = subscriber.Pending.Id;
            var url = options.PublicServerUrl.TrimEnd('/') + "/" + publicPath + (publicPath.Contains("?") ? "&" : "?") + "action=unsubscribe&token=" + subscriber.UnsubscribeToken + "&lang=" + subscriber.Language;
            fresh.Body += "\n\n" + (subscriber.Language == "fr" ? "Se désabonner :" : "Unsubscribe:") + "\n" + url;
            subscriber.Pending = fresh;
            store.Save(state);
            if (!accountAllowed(subscriber.EmbyUserId)) return false;
            try { await sender.SendAsync(subscriber.Pending, deliveryOptions, token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch
            {
                subscriber.RetryAfter = now.AddHours(1);
                subscriber.LastError = "Delivery failed; retry scheduled.";
                store.Save(state); return true; // One failing mailbox must not block the rest.
            }
            subscriber.NextDay = DateTime.ParseExact(subscriber.Pending.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                .AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            subscriber.Pending = null; subscriber.LastError = ""; subscriber.RetryAfter = default;
            subscriber.LastDeliveredAt = now;
            store.Save(state); return true;
        }
        finally { gate.Release(); }
    }

    public async Task<List<Subscriber>> ListAsync()
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try { return store.Load().Subscribers; }
        finally { gate.Release(); }
    }
    public async Task RemoveOwnAsync(long userId, string id)
    {
        if (userId <= 0) throw new UnauthorizedAccessException();
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var state = store.Load();
            state.Subscribers.RemoveAll(s => s.Id == id && (s.EmbyUserId == userId || (!s.Confirmed && s.PendingEmbyUserId == userId)));
            store.Save(state);
        }
        finally { gate.Release(); }
    }
    public async Task RemoveAsync(string id)
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try { var state = store.Load(); state.Subscribers.RemoveAll(s => s.Id == id); store.Save(state); }
        finally { gate.Release(); }
    }
}
