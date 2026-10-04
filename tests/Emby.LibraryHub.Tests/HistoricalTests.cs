using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emby.LibraryHub.Core;
using Xunit;

namespace Emby.LibraryHub.Tests;

public sealed class HistoricalTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-03T16:00:00+02:00");
    private static readonly DigestOptions Options = new()
    {
        DeliveryEnabled = false, Language = "fr", TimeZoneId = "Europe/Paris",
        PublicServerUrl = "https://media.example.test", SmtpHost = "localhost",
        Sender = "sender@example.test", Recipient = "recipient@example.test"
    };
    private static MediaEntry Movie(long id, string at) => new()
    {
        ItemId = id, Path = "/movies/" + id, LibraryId = "films", LibraryName = "Films",
        Kind = "Movie", Title = "Movie " + id, DateCreated = DateTimeOffset.Parse(at)
    };
    private static MemoryStore Store(params MediaEntry[] items)
    {
        var state = new DigestState(); state.Libraries.Add("films"); state.Inventory.AddRange(items);
        var store = new MemoryStore(); store.Save(state); return store;
    }

    [Fact]
    public void DatesAreInclusiveInConfiguredZoneAndEmptyDaysAreSkipped()
    {
        var store = Store(Movie(1, "2026-09-30T23:59:59+02:00"), Movie(2, "2026-10-01T00:00:00+02:00"),
            Movie(3, "2026-10-03T00:00:00+02:00"), Movie(4, "2026-10-03T17:00:00+02:00"));
        var engine = new DigestEngine(store);
        var messages = engine.BuildReports("2026-10-01", "2026-10-03", Now, Options);
        Assert.Equal(2, messages.Count);
        Assert.Equal(new[] { "2026-10-01", "2026-10-03" }, messages.Select(m => m.Day));
        Assert.Contains("id=2", messages[0].Body); Assert.Contains("id=3", messages[1].Body);
        Assert.DoesNotContain("id=1", string.Join("", messages.Select(m => m.Body)));
        Assert.DoesNotContain("id=4", string.Join("", messages.Select(m => m.Body)));
        Assert.Null(store.Load().HistoricalBatch);
    }

    [Fact]
    public void EndDateIncludesItsLastInstantAndDstUsesCalendarDates()
    {
        var store = Store(Movie(1, "2026-03-29T00:00:00+01:00"), Movie(2, "2026-03-29T23:59:59.9999999+02:00"),
            Movie(3, "2026-03-30T00:00:00+02:00"));
        var message = Assert.Single(new DigestEngine(store).BuildReports("2026-03-29", "2026-03-29", Now, Options));
        Assert.Contains("id=1", message.Body); Assert.Contains("id=2", message.Body); Assert.DoesNotContain("id=3", message.Body);
    }

    [Fact]
    public void ScheduledDeliveryArchivesRemovalsForLaterHistoricalMail()
    {
        var store = Store(Movie(1, "2026-09-20T12:00:00Z")); var engine = new DigestEngine(store);
        engine.ItemRemoved(1, "/movies/1", new[] { "films" }, Now.AddDays(-1));
        var dailyOptions = JsonSerializer.Deserialize<DigestOptions>(JsonSerializer.Serialize(Options))!;
        dailyOptions.DeliveryEnabled = true;
        engine.ArchiveCompletedDays(Now, dailyOptions);
        Assert.Empty(store.Load().Changes); Assert.Single(store.Load().History);
        var message = Assert.Single(engine.BuildReports("2026-10-02", "2026-10-02", Now, Options));
        Assert.Contains("Retraits :", message.Body); Assert.Contains("Movie 1", message.Body);
        Assert.DoesNotContain("https://", message.Body);
    }

    [Fact]
    public void HistoricalComputationLeavesDailyChangesAndOutboxUntouchedAndDeduplicatesCatalogDates()
    {
        var item = Movie(1, "2026-09-20T12:00:00Z"); var store = Store(item);
        var state = store.Load();
        state.Changes.Add(new LibraryChange { Added = true, Item = item, ObservedAt = Now.AddDays(-1) });
        state.Outbox.Add(new DigestMessage { Body = "Frozen daily report" }); store.Save(state);
        var engine = new DigestEngine(store);
        var messages = engine.BuildReports("2026-09-20", "2026-10-03", Now, Options);
        state = store.Load();
        Assert.Equal("2026-10-02", Assert.Single(messages).Day);
        Assert.Single(state.Changes); Assert.Equal("Frozen daily report", Assert.Single(state.Outbox).Body);
        Assert.Empty(state.History);
    }

    [Theory]
    [InlineData("", "2026-10-03")]
    [InlineData("2026-02-30", "2026-10-03")]
    [InlineData("2026-10-03", "2026-10-01")]
    [InlineData("2026-10-01", "2026-10-04")]
    public void InvalidRangesCannotMutateTheSavedState(string from, string to)
    {
        var store = Store(); var before = store.Json;
        Assert.Throws<ArgumentException>(() => new DigestEngine(store).BuildReports(from, to, Now, Options));
        Assert.Equal(before, store.Json);
    }

    private sealed class MemoryStore : IDigestStore
    {
        public string Json { get; set; } = "{}";
        public bool FailSave { get; set; }
        public DigestState Load() => JsonSerializer.Deserialize<DigestState>(Json)!;
        public void Save(DigestState state)
        {
            if (FailSave) throw new IOException("Disk full");
            Json = JsonSerializer.Serialize(state);
        }
    }
}
