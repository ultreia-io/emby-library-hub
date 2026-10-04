using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Emby.LibraryHub.Core;

public interface IDigestReportPublisher
{
    void Publish(IReadOnlyList<DigestMessage> messages, DigestOptions options, DateTimeOffset now, bool automatic = false);
    string GetReportUrl(string day, string baseUrl);
}

public sealed class ReportEntry
{
    public string Day { get; set; } = "";
    public string Title { get; set; } = "";
    public int Count { get; set; }
    public bool Partial { get; set; }
}

public sealed class ReportManifest
{
    public int Version { get; set; } = 1;
    public string ShareId { get; set; } = Guid.NewGuid().ToString("N");
    public string Language { get; set; } = "fr";
    public string AutomaticFrom { get; set; } = "";
    public List<ReportEntry> Reports { get; set; } = new List<ReportEntry>();
}

// Saved reports form a shared private archive. The HTTP adapter must require an active Emby account.
public sealed class ReportArchive : IDigestReportPublisher
{
    private readonly string directory;
    private readonly Func<string, ReportManifest> deserialize;
    private readonly Func<ReportManifest, string> serialize;
    private readonly object sync = new object();

    public ReportArchive(string directory, Func<string, ReportManifest> deserialize, Func<ReportManifest, string> serialize)
    {
        this.directory = directory; this.deserialize = deserialize; this.serialize = serialize;
        lock (sync)
        {
            Directory.CreateDirectory(directory);
            var manifest = Load();
            if (!File.Exists(ManifestPath)) Write(ManifestPath, serialize(manifest));
        }
    }

    private string ManifestPath => Path.Combine(directory, "archive.json");
    private ReportManifest Load()
    {
        if (!File.Exists(ManifestPath))
        {
            if (File.Exists(ManifestPath + ".bak")) throw new InvalidDataException("Report index is missing; restore its backup.");
            return new ReportManifest();
        }
        var manifest = deserialize(File.ReadAllText(ManifestPath, Encoding.UTF8));
        if (manifest == null || manifest.Version != 1 || !Guid.TryParseExact(manifest.ShareId, "N", out _) ||
            (!string.IsNullOrEmpty(manifest.AutomaticFrom) && !ValidDay(manifest.AutomaticFrom)) ||
            manifest.Reports.Any(r => !ValidDay(r.Day)))
            throw new InvalidDataException("Invalid report index; existing reports were not changed.");
        return manifest;
    }

    public ReportManifest Snapshot() { lock (sync) return Load(); }

    // This read has no engine, media lookup, write or publisher dependency. Atomic
    // file replacement lets readers use the last committed files during generation.
    public string? ReadPublished(string? day, string? archivePath = null, string? subscriptionPath = null)
    {
        archivePath ??= CommunityLinks.RelativeWebPath;
        subscriptionPath ??= CommunityLinks.SubscriptionRelativeWebPath;
        archivePath = System.Net.WebUtility.HtmlEncode(archivePath);
        subscriptionPath = System.Net.WebUtility.HtmlEncode(subscriptionPath);
        if (!string.IsNullOrEmpty(day) && !ValidDay(day)) return null;
        var manifest = Load();
        if (!string.IsNullOrEmpty(day) && !manifest.Reports.Any(report => report.Day == day)) return null;
        var path = Path.Combine(directory, string.IsNullOrEmpty(day) ? "index.html" : day + ".html");
        if (!File.Exists(path)) return null;
        string html;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        using (var reader = new StreamReader(stream, Encoding.UTF8)) html = reader.ReadToEnd();
        // Older saved pages used sibling .html links. Adapt navigation in the
        // response only; keep the stored content and publication timestamps intact.
        html = Regex.Replace(html, "href=\"([0-9]{4}-[0-9]{2}-[0-9]{2})\\.html\"",
            match => "href=\"" + archivePath + "/" + match.Groups[1].Value + "\"");
        html = html.Replace("href=\"index.html\"", "href=\"" + archivePath + "\"")
            .Replace("href=\"" + CommunityLinks.RelativeWebPath, "href=\"" + archivePath)
            .Replace("href=\"" + CommunityLinks.SubscriptionRelativeWebPath, "href=\"" + subscriptionPath);
        if (!html.Contains("class=\"hub-footer\"", StringComparison.Ordinal))
            html = html.Replace("</main>", ReportHtml.BrandFooter + "</main>");
        // Apply the current page layout to stored reports without rewriting their files.
        html = html.Replace("</head>", "<style>main{min-height:100vh;display:flex;flex-direction:column}main>*{flex-shrink:0}main>.hub-footer{margin-top:auto!important}</style></head>");
        return html;
    }



    public string GetReportUrl(string day, string baseUrl)
    {
        if (!ValidDay(day)) throw new ArgumentException("Invalid report date.");
        if (string.IsNullOrWhiteSpace(baseUrl)) return "";
        return baseUrl.TrimEnd('/') + "/" + CommunityLinks.WebPath + "/" + day;
    }

    public void Publish(IReadOnlyList<DigestMessage> messages, DigestOptions options, DateTimeOffset now, bool automatic = false)
    {
        lock (sync)
        {
            var manifest = Load();
            manifest.Language = options.Language;
            var today = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId)).Date;
            foreach (var message in messages)
            {
                if (!ValidDay(message.Day)) throw new ArgumentException("Invalid report date.");
                if (automatic && string.CompareOrdinal(message.Day, manifest.AutomaticFrom) < 0) continue;
                var day = DateTime.ParseExact(message.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (day > today) throw new ArgumentException("Cannot publish a future report.");
                var entry = new ReportEntry
                {
                    Day = message.Day, Title = message.Subject, Partial = day == today,
                    Count = message.Body.Split('\n').Count(line => line.StartsWith("• ", StringComparison.Ordinal))
                };
                Write(Path.Combine(directory, message.Day + ".html"), ReportHtml.Day(message, options.Language, entry.Partial));
                manifest.Reports.RemoveAll(r => r.Day == message.Day);
                manifest.Reports.Add(entry);
            }
            manifest.Reports = manifest.Reports.OrderByDescending(r => r.Day, StringComparer.Ordinal).ToList();
            Write(ManifestPath, serialize(manifest));
            Write(Path.Combine(directory, "index.html"), ReportHtml.Index(manifest));
        }
    }

    // Only the published index changes. Keep source history, mail queues, subscriber
    // data, and the share identifier; old pages are unreachable until regenerated.
    public int Reset(DateTimeOffset now, DigestOptions options)
    {
        var zone = options.Validate();
        lock (sync)
        {
            var manifest = Load();
            var count = manifest.Reports.Count;
            var backup = Path.Combine(directory, "reset-backups", now.UtcDateTime.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(backup);
            // Complete the backup before changing the live manifest.
            File.Copy(ManifestPath, Path.Combine(backup, "archive.json"));
            if (File.Exists(Path.Combine(directory, "index.html")))
                File.Copy(Path.Combine(directory, "index.html"), Path.Combine(backup, "index.html"));
            foreach (var entry in manifest.Reports)
            {
                var file = entry.Day + ".html";
                if (File.Exists(Path.Combine(directory, file)))
                    File.Copy(Path.Combine(directory, file), Path.Combine(backup, file));
            }
            manifest.Reports.Clear();
            manifest.AutomaticFrom = TimeZoneInfo.ConvertTime(now, zone).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Write(ManifestPath, serialize(manifest));
            Write(Path.Combine(directory, "index.html"), ReportHtml.Index(manifest));
            return count;
        }
    }

    public string? Read(string shareId, string page)
    {
        lock (sync)
        {
            var manifest = Load();
            if (shareId == null || shareId.Length != 32 || !CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(shareId), Encoding.ASCII.GetBytes(manifest.ShareId))) return null;
            if (page == "index.html") return ReportHtml.Index(manifest);
            if (page == null || !page.EndsWith(".html", StringComparison.Ordinal)) return null;
            var day = page.Substring(0, page.Length - 5);
            if (!ValidDay(day) || !manifest.Reports.Any(r => r.Day == day)) return null;
            var path = Path.Combine(directory, day + ".html");
            return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;
        }
    }

    private static bool ValidDay(string day) => day != null && day.Length == 10 &&
        DateTime.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static void Write(string path, string content)
    {
        if (File.Exists(path) && File.ReadAllText(path, Encoding.UTF8) == content) return;
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, content, new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
