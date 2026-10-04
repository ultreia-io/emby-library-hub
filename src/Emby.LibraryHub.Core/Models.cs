using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Emby.LibraryHub.Core;

public sealed class DigestOptions
{
    // Null offers all libraries until the administrator saves an explicit selection; empty offers none.
    public string[]? BrowseLibraryIds { get; set; }
    public bool DeliveryEnabled { get; set; }
    public bool SubscriptionsEnabled { get; set; }
    public string Language { get; set; } = "fr";
    public bool IncludeVideoLinks { get; set; } = true;
    public string PublicServerUrl { get; set; } = "";
    public string TimeZoneId { get; set; } = "Europe/Paris";
    public int DeliveryHour { get; set; } = 8;
    public int DeliveryMinute { get; set; }
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string SmtpUsername { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string Sender { get; set; } = "";
    // SMTP destination is supplied per confirmed subscriber, never saved as a setting.
    [System.Xml.Serialization.XmlIgnore]
    [System.Runtime.Serialization.IgnoreDataMember]
    public string Recipient { get; set; } = "";

    public DigestOptions ForRecipient(string email, string language)
    {
        var copy = (DigestOptions)MemberwiseClone();
        copy.Recipient = email; copy.Language = language;
        return copy;
    }

    public TimeZoneInfo Validate()
    {
        if (BrowseLibraryIds != null && Array.Exists(BrowseLibraryIds, id =>
            !long.TryParse(id, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value) || value <= 0))
            throw new ArgumentException("Invalid browse library selection.");
        if (Language != "fr" && Language != "en")
            throw new ArgumentException("Email language must be fr or en.");
        if (DeliveryHour < 0 || DeliveryHour > 23 || DeliveryMinute < 0 || DeliveryMinute > 59)
            throw new ArgumentException("Invalid delivery time.");
        if (!string.IsNullOrEmpty(PublicServerUrl) &&
            (!Uri.TryCreate(PublicServerUrl, UriKind.Absolute, out var url) ||
             (url.Scheme != "https" && url.Scheme != "http") ||
             !string.IsNullOrEmpty(url.UserInfo) || !string.IsNullOrEmpty(url.Query) || !string.IsNullOrEmpty(url.Fragment)))
            throw new ArgumentException("Public server URL must be an HTTP(S) base URL without credentials, query or fragment.");
        if (DeliveryEnabled && IncludeVideoLinks && string.IsNullOrWhiteSpace(PublicServerUrl))
            throw new ArgumentException("Set the public server URL to include video links.");
        if (SubscriptionsEnabled && string.IsNullOrWhiteSpace(PublicServerUrl))
            throw new ArgumentException("Set the public server URL before enabling subscriptions.");
        return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
    }
}

public sealed class MediaEntry
{
    public string LibraryId { get; set; } = "";
    public string LibraryName { get; set; } = "";
    public long ItemId { get; set; }
    public string Path { get; set; } = "";
    public DateTimeOffset? DateCreated { get; set; }
    public string Title { get; set; } = "";
    public string Kind { get; set; } = "";
    public int? Year { get; set; }
    public string SeriesId { get; set; } = "";
    public string SeriesName { get; set; } = "";
    public int? Season { get; set; }
    public int? Episode { get; set; }
    public int? EpisodeEnd { get; set; }
    public string Key => LibraryId + "\n" + (string.IsNullOrEmpty(Path) ? "id:" + ItemId : Path);
}

public sealed class LibraryInventory
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<MediaEntry> Items { get; set; } = new List<MediaEntry>();
}

public sealed class LibraryChange
{
    public MediaEntry Item { get; set; } = new MediaEntry();
    public bool Added { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
}

public sealed class DigestMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Day { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
}

public sealed class DigestState
{
    public int Version { get; set; } = 1;
    public List<string> Libraries { get; set; } = new List<string>();
    public List<string> AdditionHistoryInitialized { get; set; } = new List<string>();
    public List<MediaEntry> Inventory { get; set; } = new List<MediaEntry>();
    public List<LibraryChange> Changes { get; set; } = new List<LibraryChange>();
    public List<DigestMessage> Outbox { get; set; } = new List<DigestMessage>();
    public List<LibraryChange> History { get; set; } = new List<LibraryChange>();
    public HistoricalBatch? HistoricalBatch { get; set; }
}

public interface IDigestStore
{
    DigestState Load();
    void Save(DigestState state);
}

public interface IDigestSender
{
    Task SendAsync(DigestMessage message, DigestOptions options, CancellationToken cancellationToken);
}
