using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Emby.LibraryHub.Core;
using Xunit;

namespace Emby.LibraryHub.Tests;

public sealed class ReportArchiveTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "digest-reports-" + Guid.NewGuid().ToString("N"));
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-03T14:00:00Z");
    private static DigestOptions Options => new() { Language = "fr", PublicServerUrl = "https://media.example.test" };
    private ReportArchive Archive() => new(directory, text => JsonSerializer.Deserialize<ReportManifest>(text)!,
        manifest => JsonSerializer.Serialize(manifest));
    private static DigestMessage Message(string day = "2026-10-03") => new()
    {
        Day = day, Subject = "Médiathèque — " + day,
        Body = "Médiathèque\n\nSéries\nAjouts :\n• Baron Noir — Saison 2\n  https://media.example.test/web/index.html#!/item?id=100&serverId=test"
    };
    private static string Token(ReportArchive archive) => archive.Snapshot().ShareId;

    [Fact]
    public void PrivateDiskArchiveSurvivesRestartAndOnlineLinksRequireEmby()
    {
        var archive = Archive(); var path = archive.Snapshot().ShareId;
        archive.Publish(new[] { Message() }, Options, Now);
        archive = Archive(); Assert.Equal(path, archive.Snapshot().ShareId);
        var html = archive.Read(Token(archive), "2026-10-03.html")!;
        Assert.Contains("Baron Noir", html); Assert.Contains("Saison 2", html);
        Assert.Contains("id=100&amp;serverId=test", html);
        Assert.Contains("target=\"_top\"", html);
        Assert.DoesNotContain("SmtpPassword", html); Assert.DoesNotContain("/movies", html);
        Assert.Contains("2026-10-03.html", archive.Read(Token(archive), "index.html"));
        Assert.True(File.Exists(Path.Combine(directory, "2026-10-03.html")));
        Assert.Equal("https://media.example.test/" + CommunityLinks.WebPath + "/2026-10-03",
            archive.GetReportUrl("2026-10-03", Options.PublicServerUrl));
    }

    [Fact]
    public void PrivateIndexSeparatesDailyReportsFromSubscriptionManagement()
    {
        var html = ReportHtml.Index(new ReportManifest
        {
            Reports = new() { new ReportEntry { Day = "2026-10-03", Count = 1 } }
        }, CommunityLinks.RelativeWebPath);
        Assert.Contains("href=\"" + CommunityLinks.SubscriptionRelativeWebPath + "\"", html);
        Assert.Contains("href=\"" + CommunityLinks.RelativeWebPath + "/2026-10-03\"", html);
        Assert.NotEqual(CommunityLinks.WebPath, CommunityLinks.SubscriptionWebPath);
    }

    [Fact]
    public void ArchiveBrowsingReadsSavedContentWithoutLoadingHistoryOrWritingFiles()
    {
        var archive = Archive();
        archive.Publish(new[] { Message() }, Options, Now);
        File.WriteAllText(Path.Combine(directory, "state.json"), "Deliberately unreadable history");
        var dayFile = Path.Combine(directory, "2026-10-03.html");
        File.AppendAllText(dayFile, "<!-- saved-report-sentinel -->");
        var before = Directory.GetFiles(directory).ToDictionary(path => path,
            path => (Text: File.ReadAllText(path), Time: File.GetLastWriteTimeUtc(path)));
        var index = archive.ReadPublished("")!;
        var day = archive.ReadPublished("2026-10-03")!;
        Assert.Contains("saved-report-sentinel", day);
        Assert.Contains(CommunityLinks.RelativeWebPath + "/2026-10-03", index);
        Assert.Contains(CommunityLinks.SubscriptionRelativeWebPath, index);
        Assert.Contains("href=\"" + CommunityLinks.RelativeWebPath + "\"", day);
        Assert.Contains("Baron Noir", day);
        Assert.Equal(before.Count, Directory.GetFiles(directory).Length);
        foreach (var saved in before)
        {
            Assert.Equal(saved.Value.Text, File.ReadAllText(saved.Key));
            Assert.Equal(saved.Value.Time, File.GetLastWriteTimeUtc(saved.Key));
        }
        File.Delete(dayFile);
        Assert.Equal(index, archive.ReadPublished("")); // Listing reads no daily files.
        Assert.Null(archive.ReadPublished("2026-10-03"));
    }

    [Fact]
    public async System.Threading.Tasks.Task ArchiveBrowsingDoesNotWaitForThePublicationLock()
    {
        var archive = Archive(); archive.Publish(new[] { Message() }, Options, Now);
        var sync = typeof(ReportArchive).GetField("sync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(archive)!;
        using var release = new System.Threading.ManualResetEventSlim();
        var held = new System.Threading.Tasks.TaskCompletionSource(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
        var writer = System.Threading.Tasks.Task.Run(() => { lock (sync) { held.SetResult(); release.Wait(); } });
        await held.Task;
        try
        {
            var read = System.Threading.Tasks.Task.Run(() => archive.ReadPublished(""));
            Assert.NotNull(await read.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally { release.Set(); await writer; }
    }

    [Theory]
    [InlineData("../../state.json")]
    [InlineData("2026-10-03.html")]
    [InlineData("2026-02-30")]
    [InlineData("2020-01-01")]
    public void SavedArchiveRejectsInvalidOrUnpublishedDays(string day)
    {
        var archive = Archive(); archive.Publish(new[] { Message() }, Options, Now);
        Assert.Null(archive.ReadPublished(day));
        archive.Reset(Now, Options);
        Assert.Null(archive.ReadPublished("2026-10-03"));
    }

    [Theory]
    [InlineData("../archive.json")]
    [InlineData("2026-10-03.html.bak")]
    [InlineData("archive.json")]
    [InlineData("%2e%2e%2fstate.json")]
    [InlineData("2026-02-30.html")]
    [InlineData("2026-10-02.html")]
    public void PublicReadsRejectTraversalPrivateFilesAndUnpublishedDays(string page)
    {
        var archive = Archive(); archive.Publish(new[] { Message() }, Options, Now);
        Assert.Null(archive.Read(Token(archive), page));
        Assert.Null(archive.Read(new string('0', 32), "index.html"));
    }

    [Fact]
    public void TitlesAndUrlsAreEscapedAndUnsafeLinksAreNotRendered()
    {
        var message = Message(); message.Subject = "<script>alert(1)</script>";
        message.Body = "Title\n\n<img src=x onerror=alert(1)>\nAjouts :\n• <script>bad()</script>\n  javascript:alert(1)";
        var html = ReportHtml.Day(message, "fr", false);
        Assert.DoesNotContain("<script>", html); Assert.DoesNotContain("<img ", html);
        Assert.Contains("&lt;script&gt;", html); Assert.DoesNotContain("javascript:", html);
        Assert.Contains("noindex,nofollow,noarchive", html);
    }

    [Fact]
    public void RegenerationKeepsOneDayAndFinalizesTodaysLabel()
    {
        var archive = Archive(); archive.Publish(new[] { Message() }, Options, Now);
        Assert.Contains("Journée en cours", archive.Read(Token(archive), "2026-10-03.html"));
        archive.Publish(new[] { Message() }, Options, Now.AddDays(1));
        Assert.DoesNotContain("Journée en cours", archive.Read(Token(archive), "2026-10-03.html"));
        var manifest = JsonSerializer.Deserialize<ReportManifest>(File.ReadAllText(Path.Combine(directory, "archive.json")))!;
        Assert.Single(manifest.Reports); Assert.False(manifest.Reports[0].Partial);
    }

    [Fact]
    public void CorruptManifestDoesNotRotateShareIdOrOverwriteReports()
    {
        var archive = Archive(); archive.Publish(new[] { Message() }, Options, Now);
        File.WriteAllText(Path.Combine(directory, "archive.json"), "{broken");
        Assert.Throws<JsonException>(() => Archive());
        Assert.True(File.Exists(Path.Combine(directory, "2026-10-03.html")));
        Assert.Equal("{broken", File.ReadAllText(Path.Combine(directory, "archive.json")));
    }

    [Fact]
    public void HtmlOnlyGenerationLeavesAllDailyAndEmailQueuesUntouched()
    {
        var archive = Archive(); var store = new Store();
        var state = new DigestState();
        state.Inventory.Add(new MediaEntry { ItemId = 10, Kind = "Movie", Title = "Movie", LibraryId = "films",
            LibraryName = "Films", Path = "/movies/10", DateCreated = Now.AddDays(-1) });
        state.Outbox.Add(new DigestMessage { Body = "Keep this pending email" }); store.Save(state);
        var before = store.Json;
        var engine = new DigestEngine(store, "server", archive);
        Assert.Equal(1, engine.GenerateReports("2026-10-01", "2026-10-03", Now, Options));
        Assert.Equal(before, store.Json);
        Assert.NotNull(archive.Read(Token(archive), "2026-10-02.html"));
        Assert.Null(store.Load().HistoricalBatch);
    }

    [Fact]
    public void ResetBacksUpReportsAndPreservesShareIdAcrossRestart()
    {
        var archive = Archive(); var path = archive.Snapshot().ShareId; var token = Token(archive);
        archive.Publish(new[] { Message("2011-04-09"), Message("2026-10-03") }, Options, Now);
        var old = archive.Read(token, "2011-04-09.html");
        Assert.Equal(2, archive.Reset(Now, Options));
        archive = Archive(); Assert.Equal(path, archive.Snapshot().ShareId);
        Assert.Null(archive.Read(token, "2011-04-09.html"));
        Assert.Null(archive.Read(token, "2026-10-03.html"));
        Assert.Contains("Aucun rapport", archive.Read(token, "index.html"));
        var backup = Assert.Single(Directory.GetDirectories(Path.Combine(directory, "reset-backups")));
        Assert.Equal(old, File.ReadAllText(Path.Combine(backup, "2011-04-09.html")));
        Assert.Equal(2, JsonSerializer.Deserialize<ReportManifest>(File.ReadAllText(Path.Combine(backup, "archive.json")))!.Reports.Count);
        Assert.Null(archive.Read(token, "reset-backups/" + Path.GetFileName(backup) + "/archive.json"));
    }

    [Fact]
    public void ResetBlocksOldAutomaticReportsButAllowsExplicitRebuild()
    {
        var archive = Archive();
        var store = new Store(); var state = new DigestState();
        state.History.Add(new LibraryChange { Item = new MediaEntry { ItemId = 10, Title = "Old film", LibraryId = "films", Kind = "Movie" }, Added = true, ObservedAt = Now.AddDays(-1) });
        state.Changes.Add(new LibraryChange { Item = new MediaEntry { ItemId = 11, Title = "Today film", LibraryId = "films", Kind = "Movie" }, Added = true, ObservedAt = Now });
        state.Outbox.Add(new DigestMessage { Body = "Pending email" }); store.Save(state);
        var engine = new DigestEngine(store, "server", archive);
        engine.PublishTrackedReports(Now, Options);
        Assert.NotNull(archive.Read(Token(archive), "2026-10-02.html"));
        var before = store.Json; archive.Reset(Now, Options);
        engine.PublishTrackedReports(Now, Options);
        Assert.Null(archive.Read(Token(archive), "2026-10-02.html"));
        Assert.NotNull(archive.Read(Token(archive), "2026-10-03.html"));
        Assert.Equal(1, engine.GenerateReports("2026-10-02", "2026-10-02", Now, Options));
        Assert.NotNull(archive.Read(Token(archive), "2026-10-02.html"));
        Assert.Equal(before, store.Json);
    }

    [Fact]
    public void ResetBackupFailureLeavesArchiveUntouched()
    {
        var archive = Archive(); archive.Publish(new[] { Message() }, Options, Now);
        var manifest = File.ReadAllText(Path.Combine(directory, "archive.json"));
        File.WriteAllText(Path.Combine(directory, "reset-backups"), "Not a directory");
        Assert.ThrowsAny<IOException>(() => archive.Reset(Now, Options));
        Assert.Equal(manifest, File.ReadAllText(Path.Combine(directory, "archive.json")));
        Assert.NotNull(archive.Read(Token(archive), "2026-10-03.html"));
    }

    [Fact]
    public void ResetCutoffUsesConfiguredCalendarDayAtMidnight()
    {
        var archive = Archive();
        archive.Reset(DateTimeOffset.Parse("2026-10-03T22:30:00Z"), Options);
        archive.Publish(new[] { Message("2026-10-03"), Message("2026-10-04") }, Options, Now.AddDays(1), automatic: true);
        Assert.Null(archive.Read(Token(archive), "2026-10-03.html"));
        Assert.NotNull(archive.Read(Token(archive), "2026-10-04.html"));
    }

    private sealed class Store : IDigestStore
    {
        public string Json { get; set; } = "{}";
        public DigestState Load() => JsonSerializer.Deserialize<DigestState>(Json)!;
        public void Save(DigestState state) => Json = JsonSerializer.Serialize(state);
    }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
