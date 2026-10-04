using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Emby.LibraryHub.Core;

// Retained only to read and preserve development-build state files. This queue is never sent.
public sealed class HistoricalBatch
{
    public string RequestId { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Status { get; set; } = "Queued";
    public int Total { get; set; }
    public int Sent { get; set; }
    public int EmptyDays { get; set; }
    public string Error { get; set; } = "";
    public List<DigestMessage> Pending { get; set; } = new List<DigestMessage>();
}

public sealed partial class DigestEngine
{
    public static (DateTime From, DateTime To) ValidateHistoricalRange(string from, string to,
        DateTimeOffset now, DigestOptions options)
    {
        if (!DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first) ||
            !DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var last))
            throw new ArgumentException("Choose both From and To dates.");
        if (first > last) throw new ArgumentException("From must be on or before To.");
        var zone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
        if (last > TimeZoneInfo.ConvertTime(now, zone).Date)
            throw new ArgumentException("The date range cannot include future days.");
        return (first, last);
    }

    public List<DigestMessage> BuildReports(string from, string to, DateTimeOffset now, DigestOptions options,
        Func<MediaEntry, bool>? visible = null)
    {
        var (first, last) = ValidateHistoricalRange(from, to, now, options);
        return BuildHistoricalMessages(Load(), first, last, now, options, options.Validate(), visible);
    }

    private List<DigestMessage> BuildHistoricalMessages(DigestState state, DateTime first, DateTime last,
        DateTimeOffset now, DigestOptions options, TimeZoneInfo zone, Func<MediaEntry, bool>? visible = null)
    {
        var knownChanges = state.History.Concat(state.Changes).ToList();
        // Observed additions take precedence over inferred catalog dates, even
        // when their observed date falls outside this requested range.
        var observedAdditions = new HashSet<string>(knownChanges.Where(c => c.Added).Select(c => c.Item.Key));
        var knownItems = knownChanges.Select(c => c.Item).Concat(state.Inventory)
            .GroupBy(i => i.Key).Select(g => g.Last());
        var rangeChanges = knownChanges.Where(c => InRange(c.ObservedAt)).ToList();
        foreach (var item in knownItems)
            if (!observedAdditions.Contains(item.Key) && item.DateCreated is { } created &&
                created <= now && InRange(created))
                rangeChanges.Add(new LibraryChange { Item = item, Added = true, ObservedAt = created });

        var available = new HashSet<string>(state.Inventory.Select(i => i.Key));
        var messages = rangeChanges.Where(c => visible == null || visible(c.Item)).GroupBy(c => TimeZoneInfo.ConvertTime(c.ObservedAt, zone).Date)
            .OrderBy(g => g.Key)
            .Select(g => formatter.Format(g.Key, g, options.Language,
                options.IncludeVideoLinks ? options.PublicServerUrl : "", serverId, available)).ToList();
        return messages;

        bool InRange(DateTimeOffset at)
        {
            var date = TimeZoneInfo.ConvertTime(at, zone).Date;
            return at <= now && date >= first && date <= last;
        }
    }

    // Subscriber delivery covers observed calendar days from confirmation onward.
    // Historical catalog inference is reserved for the explicit admin date-range action.
    public List<DigestMessage> SubscriptionMessages(DateTime first, DateTimeOffset now, DigestOptions options,
        Func<MediaEntry, bool>? visible = null)
    {
        var zone = options.Validate();
        var today = TimeZoneInfo.ConvertTime(now, zone).Date;
        var state = Load();
        var available = new HashSet<string>(state.Inventory.Select(i => i.Key));
        var messages = state.History.Concat(state.Changes)
            .Where(c => c.ObservedAt <= now && TimeZoneInfo.ConvertTime(c.ObservedAt, zone).Date >= first &&
                TimeZoneInfo.ConvertTime(c.ObservedAt, zone).Date < today && (visible == null || visible(c.Item)))
            .GroupBy(c => TimeZoneInfo.ConvertTime(c.ObservedAt, zone).Date)
            .Where(g => g.Key >= first && g.Key < today).OrderBy(g => g.Key)
            .Select(g => formatter.Format(g.Key, g, options.Language,
                options.IncludeVideoLinks ? options.PublicServerUrl : "", serverId, available)).ToList();
        foreach (var message in messages)
        {
            var link = reports?.GetReportUrl(message.Day, options.PublicServerUrl) ?? "";
            if (link.Length > 0) message.Body += "\n\n" + (options.Language == "fr" ? "Rapport en ligne :" : "Online report:") + "\n" + link;
        }
        return messages;
    }

    public int GenerateReports(string from, string to, DateTimeOffset now, DigestOptions options)
    {
        var (first, last) = ValidateHistoricalRange(from, to, now, options);
        var messages = BuildHistoricalMessages(Load(), first, last, now, options, options.Validate());
        if (reports == null) throw new InvalidOperationException("The report archive is not configured.");
        reports.Publish(messages, options, now);
        return messages.Count;
    }

    public void PublishTrackedReports(DateTimeOffset now, DigestOptions options)
    {
        if (reports == null) return;
        var zone = options.Validate();
        var state = Load();
        var days = state.History.Concat(state.Changes).Where(c => c.ObservedAt <= now)
            .Select(c => TimeZoneInfo.ConvertTime(c.ObservedAt, zone).Date).Distinct().OrderBy(d => d).ToList();
        if (days.Count == 0) { reports.Publish(new List<DigestMessage>(), options, now, automatic: true); return; }
        var messages = BuildHistoricalMessages(state, days[0], days[days.Count - 1], now, options, zone);
        reports.Publish(messages, options, now, automatic: true);
    }

}
