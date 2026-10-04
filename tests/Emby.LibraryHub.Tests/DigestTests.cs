using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emby.LibraryHub.Core;
using Xunit;
using MimeKit;

namespace Emby.LibraryHub.Tests;

public sealed class DigestTests
{
    private static readonly DateTimeOffset Day = DateTimeOffset.Parse("2026-10-03T12:00:00Z");
    private static readonly DateTimeOffset Due = DateTimeOffset.Parse("2026-10-04T06:00:00Z");
    private static MediaEntry Movie(long id, string title = "Dune : Deuxième partie") => new()
    {
        ItemId = id, Title = title, LibraryId = "films", LibraryName = "Films", Path = "/movies/" + id + ".mkv",
        Kind = "Movie", Year = 2024
    };
    private static LibraryInventory Library(params MediaEntry[] items) => new() { Id = "films", Name = "Films", Items = items.ToList() };
    private static DigestOptions Options(string language = "fr") => new()
    {
        DeliveryEnabled = true, Language = language, PublicServerUrl = "https://media.example.test",
        Sender = "sender@example.test", Recipient = "recipient@example.test", SmtpHost = "smtp.example.test"
    };
    private static (MemoryStore Store, DigestEngine Engine, RecordingSender Sender) Setup()
    {
        var store = new MemoryStore();
        return (store, new DigestEngine(store, "server-test"), new RecordingSender());
    }

    [Fact]
    public void FirstRunAndUnchangedLibrariesSendNothing()
    {
        var (store, engine, sender) = Setup();
        engine.Reconcile(new[] { Library(Movie(1)) }, Day);
        engine.Reconcile(new[] { Library(Movie(1)) }, Day.AddHours(1));
        Assert.Empty(engine.SubscriptionMessages(DateTime.MinValue, Due, Options()));
        Assert.Empty(store.Load().Changes);
        Assert.Empty(sender.Messages);
    }

    [Theory]
    [InlineData("fr", "Médiathèque — 3 octobre 2026", "Ajouts :", "Retraits :")]
    [InlineData("en", "Media library — 3 October 2026", "Added:", "Removed:")]
    public void FormatsLocalizedChangesAndOnlyLinksAddedVideos(string language, string heading, string added, string removed)
    {
        var (_, engine, sender) = Setup();
        engine.Reconcile(new[] { Library(Movie(1, "Alien")) }, Day.AddDays(-1));
        engine.Reconcile(new[] { Library(Movie(2)) }, Day);
        var message = Assert.Single(engine.SubscriptionMessages(DateTime.MinValue, Due, Options(language)));
        Assert.Equal(heading, message.Subject);
        Assert.Contains(added, message.Body);
        Assert.Contains(removed, message.Body);
        Assert.Contains("https://media.example.test/web/index.html#!/item?id=2&serverId=server-test", message.Body);
        Assert.DoesNotContain("id=1&", message.Body);
        Assert.DoesNotContain("/movies/", message.Body);
        Assert.DoesNotContain("<html", message.Body);
    }

    [Fact]
    public void RepeatedEventsDoNotDuplicateReportEntries()
    {
        var (_, engine, sender) = Setup();
        engine.Reconcile(new[] { Library() }, Day);
        engine.ItemAdded(Movie(1), Day);
        engine.ItemAdded(Movie(1), Day);
        engine.Reconcile(new[] { Library(Movie(1)) }, Day);
        var message = Assert.Single(engine.SubscriptionMessages(DateTime.MinValue, Due, Options()));
        Assert.Equal(1, message.Body.Split("Dune").Length - 1);
    }

    [Fact]
    public void PreviousCalendarDayAndDeliveryTimeUseConfiguredTimezone()
    {
        var (store, engine, sender) = Setup();
        engine.Reconcile(new[] { Library() }, Day);
        engine.ItemAdded(Movie(1), DateTimeOffset.Parse("2026-10-03T21:59:59Z"));
        engine.ItemAdded(Movie(2), DateTimeOffset.Parse("2026-10-03T22:00:00Z"));
        var message = Assert.Single(engine.SubscriptionMessages(DateTime.MinValue, Due, Options()));
        Assert.Contains("id=1&", message.Body); Assert.DoesNotContain("id=2&", message.Body);
        engine.ArchiveCompletedDays(Due, Options());
        Assert.Single(store.Load().Changes); Assert.Single(store.Load().History);
    }

    [Theory]
    [InlineData("2026-03-29T06:00:00Z", "2026-03-28T23:00:00Z")]
    [InlineData("2026-10-25T07:00:00Z", "2026-10-24T22:00:00Z")]
    public void DstTransitionsKeepCurrentDayOutOfReport(string dueTime, string midnight)
    {
        var (_, engine, sender) = Setup();
        var start = DateTimeOffset.Parse(midnight);
        engine.Reconcile(new[] { Library() }, start.AddDays(-1));
        engine.ItemAdded(Movie(1), start.AddSeconds(-1));
        engine.ItemAdded(Movie(2), start);
        var message = Assert.Single(engine.SubscriptionMessages(DateTime.MinValue, DateTimeOffset.Parse(dueTime), Options()));
        Assert.Contains("id=1&", message.Body); Assert.DoesNotContain("id=2&", message.Body);
    }

    [Fact]
    public void OfflineLibraryIsNotReportedAsRemoved()
    {
        var (store, engine, sender) = Setup();
        engine.Reconcile(new[] { Library(Movie(1)) }, Day);
        engine.Reconcile(Array.Empty<LibraryInventory>(), Day.AddHours(1));
        engine.ItemRemoved(1, "/movies", Array.Empty<string>(), Day);
        Assert.Empty(engine.SubscriptionMessages(DateTime.MinValue, Due, Options()));
        Assert.Single(store.Load().Inventory);
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public void DirectoryDeletionCapturesChildrenButNotSiblingPrefix()
    {
        var (store, engine, _) = Setup();
        var first = Movie(1); first.Path = "/movies/season/1.mkv";
        var sibling = Movie(2); sibling.Path = "/movies/season-other/2.mkv";
        engine.Reconcile(new[] { Library(first, sibling) }, Day);
        engine.ItemRemoved(999, "/movies/season", new[] { "films" }, Day);
        Assert.Equal(1, Assert.Single(store.Load().Changes).Item.ItemId);
        Assert.Equal(2, Assert.Single(store.Load().Inventory).ItemId);
    }

    [Fact]
    public void MetadataOnlyChangesRefreshPendingTitlesWithoutAddingEvents()
    {
        var (store, engine, _) = Setup();
        engine.Reconcile(new[] { Library() }, Day);
        engine.ItemAdded(Movie(1, "raw_filename"), Day);
        engine.Reconcile(new[] { Library(Movie(1, "Correct title")) }, Day);
        Assert.Equal("Correct title", Assert.Single(store.Load().Changes).Item.Title);
        Assert.Contains("Correct title", engine.Preview(Day, Options()).Body);
    }

    [Fact]
    public void AdditionAndRemovalSameDayAppearWithoutDeadLink()
    {
        var (_, engine, _) = Setup();
        engine.Reconcile(new[] { Library() }, Day);
        engine.ItemAdded(Movie(1), Day);
        engine.ItemRemoved(1, "/movies/1.mkv", new[] { "films" }, Day.AddHours(1));
        var preview = engine.Preview(Day, Options());
        Assert.Contains("Ajouts :", preview.Body);
        Assert.Contains("Retraits :", preview.Body);
        Assert.DoesNotContain("https://", preview.Body);
    }

    [Theory]
    [InlineData("fr", "Saison")]
    [InlineData("en", "Season")]
    public void EpisodesUseOneTitleAndSeriesLinkPerSeason(string language, string seasonLabel)
    {
        var changes = new[] { 3, 4, 5, 7 }.Select(number => new LibraryChange
        {
            Added = true, ObservedAt = Day, Item = new MediaEntry
            {
                ItemId = number, LibraryId = "tv", LibraryName = "Séries", Path = "/tv/" + number,
                Kind = "Episode", Title = "Individual episode", SeriesName = "Severance", SeriesId = "100",
                Season = number == 7 ? 3 : 2, Episode = number
            }
        });
        var body = new DigestFormatter().Format(Day.Date, changes, language, "https://media.example.test/", "test").Body;
        foreach (var season in new[] { 2, 3 })
            Assert.Contains("• Severance — " + seasonLabel + " " + season +
                "\n  https://media.example.test/web/index.html#!/item?id=100&serverId=test", body);
        Assert.Equal(2, body.Split("• ").Length - 1);
        Assert.Equal(2, body.Split("https://").Length - 1);
        Assert.DoesNotContain("Individual episode", body);
    }

    [Theory]
    [InlineData("0", true, true)]
    [InlineData("", true, true)]
    [InlineData("100", false, true)]
    [InlineData("100", true, false)]
    public void SeriesLinksAreOmittedWhenUnknownRemovedOrLinksDisabled(string seriesId, bool added, bool linksEnabled)
    {
        var entry = new MediaEntry { Kind = "Episode", ItemId = 20, Path = "/episode", LibraryId = "tv",
            SeriesName = "Show", SeriesId = seriesId };
        var body = new DigestFormatter().Format(Day.Date,
            new[] { new LibraryChange { Added = added, Item = entry } }, "en",
            linksEnabled ? "https://media.example.test" : "").Body;
        Assert.EndsWith("• Show", body);
        Assert.DoesNotContain("https://", body);
    }

    [Fact]
    public void UnnumberedEpisodesStillGroupAndLinkIfAnAddedEpisodeRemainsAvailable()
    {
        var first = new MediaEntry { Kind = "Episode", ItemId = 20, Path = "/a", LibraryId = "tv",
            SeriesName = "Show", SeriesId = "100" };
        var second = new MediaEntry { Kind = "Episode", ItemId = 21, Path = "/b", LibraryId = "tv",
            SeriesName = "Show", SeriesId = "100" };
        var changes = new[] { new LibraryChange { Added = true, Item = first },
            new LibraryChange { Added = true, Item = second } };
        var formatter = new DigestFormatter();
        var body = formatter.Format(Day.Date, changes, "en", "https://media.example.test", "",
            new System.Collections.Generic.HashSet<string> { second.Key }).Body;
        Assert.EndsWith("• Show\n  https://media.example.test/web/index.html#!/item?id=100", body);
        body = formatter.Format(Day.Date, changes, "en", "https://media.example.test", "",
            new System.Collections.Generic.HashSet<string>()).Body;
        Assert.DoesNotContain("https://", body);
    }

    [Fact]
    public void DelayedAdditionReportOmitsLinkIfVideoHasSinceBeenRemoved()
    {
        var (_, engine, sender) = Setup();
        engine.Reconcile(new[] { Library() }, Day.AddDays(-1));
        engine.ItemAdded(Movie(1), Day);
        engine.ItemRemoved(1, "/movies/1.mkv", new[] { "films" }, Day.AddDays(1));
        var messages = engine.SubscriptionMessages(DateTime.MinValue, Due.AddDays(1), Options());
        Assert.Equal(2, messages.Count);
        Assert.All(messages, m => Assert.DoesNotContain("https://", m.Body));
    }

    [Fact]
    public void PreviewIsReadOnlyAndLanguageChangesImmediately()
    {
        var (store, engine, _) = Setup();
        engine.Reconcile(new[] { Library() }, Day);
        engine.ItemAdded(Movie(1), Day);
        var before = store.Json;
        Assert.Contains("Ajouts :", engine.Preview(Day, Options("fr")).Body);
        Assert.Contains("Added:", engine.Preview(Day, Options("en")).Body);
        Assert.Equal(before, store.Json);
    }

    [Fact]
    public void MultipleMissedDaysAreFormattedInOrder()
    {
        var (_, engine, sender) = Setup();
        engine.Reconcile(new[] { Library() }, Day.AddDays(-2));
        engine.ItemAdded(Movie(1), Day.AddDays(-1));
        engine.ItemAdded(Movie(2), Day);
        var messages = engine.SubscriptionMessages(DateTime.MinValue, Due, Options());
        Assert.Equal(new[] { "2026-10-02", "2026-10-03" }, messages.Select(m => m.Day));
    }

    [Fact]
    public void TitleNewlinesCannotInjectSections()
    {
        var (_, engine, _) = Setup();
        engine.Reconcile(new[] { Library() }, Day);
        engine.ItemAdded(Movie(1, "Title\r\nInjected\ttext"), Day);
        var options = Options(); options.IncludeVideoLinks = false;
        var body = engine.Preview(Day, options).Body;
        Assert.Contains("• Title Injected text", body);
        Assert.DoesNotContain("https://", body);
    }

    [Fact]
    public void MailIsUtf8PlainTextWithStableMessageId()
    {
        var message = new DigestMessage { Id = "test-id", Subject = "Médiathèque", Body = "Ajouts :\n• Éléphant" };
        using var mail = SmtpDigestSender.CreateMessage(message, Options());
        var body = Assert.IsType<TextPart>(mail.Body);
        Assert.Equal("text/plain", body.ContentType.MimeType);
        Assert.Equal("utf-8", body.ContentType.Charset);
        Assert.Equal("Médiathèque", mail.Subject);
        Assert.Equal("test-id@emby-library-hub.local", mail.MessageId);
        Assert.Equal(message.Body, body.Text.Replace("\r\n", "\n"));
        Assert.DoesNotContain("Médiathèque", body.Text);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:secret@example.test")]
    [InlineData("https://example.test?api_key=secret")]
    [InlineData("https://example.test/#fragment")]
    public void InvalidPublicUrlsAreRejected(string url)
    {
        var options = Options(); options.PublicServerUrl = url;
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void UnknownStateVersionDoesNotResetHistory()
    {
        var store = new MemoryStore { Json = "{\"Version\":2}" };
        Assert.Throws<InvalidOperationException>(() => new DigestEngine(store).Reconcile(new[] { Library() }, Day));
        Assert.Equal("{\"Version\":2}", store.Json);
    }

    [Fact]
    public void UpgradeRecoversEarlierAdditionsWithoutRepeatingAfterRestartOrDelivery()
    {
        var (store, engine, sender) = Setup();
        var movie = Movie(240205, "Ghost in the Shell Arise Border 4 Ghost Stands Alone");
        // A legacy inventory has no DateCreated or initialization marker.
        store.Json = JsonSerializer.Serialize(new
        {
            Version = 1, Libraries = new[] { "films" }, Inventory = new[] { movie },
            Changes = Array.Empty<LibraryChange>(), Outbox = Array.Empty<DigestMessage>()
        });
        movie.DateCreated = DateTimeOffset.Parse("2026-10-03T12:06:25+02:00");
        var now = DateTimeOffset.Parse("2026-10-03T16:00:00+02:00");
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
        engine.Reconcile(new[] { Library(movie) }, now, zone);
        Assert.Equal(movie.DateCreated, Assert.Single(store.Load().Changes).ObservedAt);
        Assert.Contains("id=240205&", engine.Preview(now, Options()).Body);
        engine = new DigestEngine(store);
        engine.Reconcile(new[] { Library(movie) }, now.AddHours(1), zone);
        Assert.Single(store.Load().Changes);
        Assert.Empty(engine.SubscriptionMessages(DateTime.MinValue, now, Options()));
        Assert.Single(engine.SubscriptionMessages(DateTime.MinValue, Due, Options()));
        engine.ArchiveCompletedDays(Due, Options());
        engine.Reconcile(new[] { Library(movie) }, Due, zone);
        Assert.Empty(store.Load().Changes); Assert.Single(store.Load().History);
        Assert.Single(engine.SubscriptionMessages(DateTime.MinValue, Due, Options()));
    }

    [Fact]
    public void FirstInventoryImportsOnlyTodaysKnownDatesInConfiguredTimezone()
    {
        var (store, engine, _) = Setup();
        var midnight = DateTimeOffset.Parse("2026-10-03T00:00:00+02:00");
        var old = Movie(1); old.DateCreated = midnight.AddTicks(-1);
        var today = Movie(2); today.DateCreated = midnight;
        var unknown = Movie(3);
        var future = Movie(4); future.DateCreated = Day.AddHours(1);
        engine.Reconcile(new[] { Library(old, today, unknown, future) }, Day,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"));
        Assert.Equal(2, Assert.Single(store.Load().Changes).Item.ItemId);
        Assert.Equal(4, store.Load().Inventory.Count);
    }

    [Fact]
    public void RecoveryDeduplicatesExistingEventsAndNewlyDiscoveredItems()
    {
        var (store, engine, _) = Setup();
        var existing = Movie(1); existing.DateCreated = Day.AddHours(-1);
        var discovered = Movie(2); discovered.DateCreated = Day.AddHours(-1);
        var state = new DigestState(); state.Libraries.Add("films"); state.Inventory.Add(existing);
        state.Changes.Add(new LibraryChange { Item = existing, Added = true, ObservedAt = Day.AddHours(-1) });
        store.Save(state);
        engine.Reconcile(new[] { Library(existing, discovered) }, Day);
        Assert.Equal(2, store.Load().Changes.Count);
        Assert.Equal(2, store.Load().Changes.Select(c => c.Item.Key).Distinct().Count());
    }

    [Fact]
    public void RecoveryMarkerAndChangesCommitTogetherAndOfflineLibrariesAreDeferred()
    {
        var (store, engine, _) = Setup();
        var movie = Movie(1); movie.DateCreated = Day.AddHours(-1);
        engine.Reconcile(Array.Empty<LibraryInventory>(), Day);
        Assert.Empty(store.Load().AdditionHistoryInitialized);
        store.FailSave = true;
        Assert.Throws<IOException>(() => engine.Reconcile(new[] { Library(movie) }, Day));
        Assert.Empty(store.Load().Changes);
        Assert.Empty(store.Load().AdditionHistoryInitialized);
        store.FailSave = false;
        engine.Reconcile(new[] { Library(movie) }, Day);
        Assert.Single(store.Load().Changes);
        Assert.Equal("films", Assert.Single(store.Load().AdditionHistoryInitialized));
    }

    [Fact]
    public void CompleteCalendarDayIncludesAdditionsAndRemovalsAtBothMidnightBoundaries()
    {
        var (_, engine, sender) = Setup();
        var start = DateTimeOffset.Parse("2026-10-03T00:00:00+02:00");
        var end = start.AddDays(1);
        engine.Reconcile(new[] { Library(Movie(10, "RemoveBefore"), Movie(11, "RemoveStart"),
            Movie(12, "RemoveEnd"), Movie(13, "RemoveAfter")) }, start.AddDays(-1));
        foreach (var item in new[] { (1, "AddBefore", start.AddTicks(-1)), (2, "AddStart", start),
            (3, "AddEnd", end.AddTicks(-1)), (4, "AddAfter", end) })
            engine.ItemAdded(Movie(item.Item1, item.Item2), item.Item3);
        foreach (var item in new[] { (10, start.AddTicks(-1)), (11, start), (12, end.AddTicks(-1)), (13, end) })
            engine.ItemRemoved(item.Item1, "/movies/" + item.Item1 + ".mkv", new[] { "films" }, item.Item2);
        var body = Assert.Single(engine.SubscriptionMessages(DateTime.MinValue, Due, Options()), m => m.Day == "2026-10-03").Body;
        foreach (var title in new[] { "AddStart", "AddEnd", "RemoveStart", "RemoveEnd" }) Assert.Contains(title, body);
        foreach (var title in new[] { "AddBefore", "AddAfter", "RemoveBefore", "RemoveAfter" }) Assert.DoesNotContain(title, body);
    }

    private sealed class MemoryStore : IDigestStore
    {
        public string Json { get; set; } = JsonSerializer.Serialize(new DigestState());
        public bool FailSave { get; set; }
        public DigestState Load() => JsonSerializer.Deserialize<DigestState>(Json)!;
        public void Save(DigestState state)
        {
            if (FailSave) throw new IOException("Disk full");
            Json = JsonSerializer.Serialize(state);
        }
    }

    private sealed class RecordingSender : IDigestSender
    {
        public List<DigestMessage> Messages { get; } = new();
        public bool Fail { get; set; }
        public Task SendAsync(DigestMessage message, DigestOptions options, CancellationToken cancellationToken)
        {
            if (Fail) throw new IOException("SMTP unavailable");
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
