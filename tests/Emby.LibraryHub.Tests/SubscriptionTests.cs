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

public sealed class SubscriptionTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "digest-subs-" + Guid.NewGuid().ToString("N"));
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-03T12:00:00Z");
    private const string PublicPath = CommunityLinks.SubscriptionWebPath;
    private SubscriptionStore Store => new(Path.Combine(directory, "subscribers.json"),
        json => JsonSerializer.Deserialize<SubscriptionState>(json)!, state => JsonSerializer.Serialize(state));
    private SubscriptionService Service() => new(Store);
    private static DigestOptions Options => new() { SubscriptionsEnabled = true, DeliveryEnabled = true,
        PublicServerUrl = "https://media.example.test", TimeZoneId = "Europe/Paris", Sender = "server@example.test" };
    private sealed class Sender : IDigestSender
    {
        public List<(DigestMessage Message, DigestOptions Options)> Sent { get; } = new();
        public string? FailAddress;
        public Task SendAsync(DigestMessage message, DigestOptions options, CancellationToken cancellationToken)
        {
            if (options.Recipient == FailAddress) throw new IOException("SMTP test failure");
            Sent.Add((JsonSerializer.Deserialize<DigestMessage>(JsonSerializer.Serialize(message))!, options));
            return Task.CompletedTask;
        }
    }
    private static string ConfirmationToken(Sender sender) => sender.Sent.Last().Message.Body.Split("&token=")[1].Split('&')[0];
    private async Task Request(SubscriptionService service, Sender sender, string email = "reader@example.test", string language = "fr", DateTimeOffset? at = null)
    {
        var now = at ?? Now;
        await service.RequestAsync(1, email, language, await service.FormTokenAsync("subscribe:1", now), PublicPath, now, Options, sender, default);
    }
    private async Task Confirm(SubscriptionService service, Sender sender, DateTimeOffset? at = null)
    {
        var now = at ?? Now; var token = ConfirmationToken(sender);
        Assert.True(await service.ApplyAsync(1, "confirm", token, await service.FormTokenAsync("confirm:1:" + token, now), now, Options));
    }
    private static List<DigestMessage> Messages(long userId, DateTime first, DigestOptions options) => new()
    {
        new DigestMessage { Day = first.ToString("yyyy-MM-dd"), Subject = options.Language == "fr" ? "Nouveautés" : "Library updates", Body = "• Test film" }
    };

    [Fact]
    public async Task SubscriptionNeedsConfirmationSurvivesRestartAndSendsPrivateLocalizedEmail()
    {
        var sender = new Sender(); var service = Service();
        await Request(service, sender, language: "en");
        Assert.Contains("Confirm your", sender.Sent.Single().Message.Subject);
        Assert.Contains("&lang=en", sender.Sent.Single().Message.Body);
        Assert.False((await service.ListAsync()).Single().Confirmed);
        Assert.False(await service.DeliverNextAsync(Now.AddDays(1), Options, PublicPath, Messages, sender, default, _ => true));
        service = Service(); await Confirm(service, sender);
        sender.Sent.Clear();
        Assert.True(await service.DeliverNextAsync(Now.AddDays(1), Options, PublicPath, Messages, sender, default, _ => true));
        var sent = sender.Sent.Single();
        Assert.Equal("reader@example.test", sent.Options.Recipient);
        Assert.Equal("Library updates", sent.Message.Subject);
        Assert.Contains("Unsubscribe:", sent.Message.Body);
        Assert.DoesNotContain("ConfirmationHash", sent.Message.Body);
        using var mime = SmtpDigestSender.CreateMessage(sent.Message, sent.Options);
        Assert.Single(mime.To); Assert.Empty(mime.Cc); Assert.Empty(mime.Bcc);
        Assert.False(await Service().DeliverNextAsync(Now.AddDays(1), Options, PublicPath, Messages, sender, default, _ => true));
    }

    [Fact]
    public async Task ConfirmationTokenExpiresIsSingleUseAndIsNotStoredInPlaintext()
    {
        var sender = new Sender(); var service = Service(); await Request(service, sender);
        var token = ConfirmationToken(sender);
        Assert.DoesNotContain(token, File.ReadAllText(Path.Combine(directory, "subscribers.json")));
        Assert.False(await service.ApplyAsync(1, "confirm", token,
            await service.FormTokenAsync("confirm:1:" + token, Now.AddHours(25)), Now.AddHours(25), Options));
        await Confirm(service, sender);
        Assert.False(await service.ApplyAsync(1, "confirm", token,
            await service.FormTokenAsync("confirm:1:" + token, Now), Now, Options));
    }

    [Fact]
    public async Task FormTokensAreBoundToActionAndExpire()
    {
        var sender = new Sender(); var service = Service();
        foreach (var token in new[] { "invalid", await service.FormTokenAsync("unsubscribe:other", Now), await service.FormTokenAsync("subscribe:1", Now.AddHours(-1)) })
            await Assert.ThrowsAsync<ArgumentException>(() => service.RequestAsync(1, "reader@example.test", "fr", token, PublicPath, Now, Options, sender, default));
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task RepeatedRequestsReplaceConfirmationAndUnsubscribedAddressesCanRejoinImmediately()
    {
        var sender = new Sender(); var service = Service(); await Request(service, sender);
        var oldToken = ConfirmationToken(sender);
        await Request(Service(), sender, email: "READER@example.test");
        Assert.Equal(2, sender.Sent.Count); Assert.Single(await service.ListAsync());
        Assert.False(await service.ApplyAsync(1, "confirm", oldToken,
            await service.FormTokenAsync("confirm:1:" + oldToken, Now), Now, Options));
        await Confirm(service, sender);
        await service.RemoveOwnAsync(1, (await service.ListAsync()).Single().Id);
        await Request(Service(), sender);
        Assert.Equal(3, sender.Sent.Count); Assert.Single(await service.ListAsync());
        await Confirm(service, sender);
        Assert.True((await service.ListAsync()).Single().Confirmed);
    }

    [Fact]
    public async Task LanguageChangesRequireFreshConfirmationAndKeepDeliveryCursor()
    {
        var sender = new Sender(); var service = Service(); await Request(service, sender); await Confirm(service, sender);
        await Request(service, sender, language: "en", at: Now.AddHours(2));
        Assert.Equal("fr", (await service.ListAsync()).Single().Language);
        await Confirm(service, sender, Now.AddHours(2));
        var subscriber = (await service.ListAsync()).Single();
        Assert.Equal("en", subscriber.Language); Assert.Equal("2026-10-03", subscriber.NextDay);
    }

    [Fact]
    public async Task UnsubscribeCancelsFailedMailAndWorksEvenWhenFeatureDisabled()
    {
        var sender = new Sender(); var service = Service(); await Request(service, sender); await Confirm(service, sender);
        sender.FailAddress = "reader@example.test";
        await service.DeliverNextAsync(Now.AddDays(1), Options, PublicPath, Messages, sender, default, _ => true);
        var subscriber = (await service.ListAsync()).Single(); Assert.NotNull(subscriber.Pending);
        var disabled = Options; disabled.SubscriptionsEnabled = false;
        var form = await service.FormTokenAsync("unsubscribe:1:" + subscriber.UnsubscribeToken, Now.AddDays(1));
        Assert.True(await service.ApplyAsync(1, "unsubscribe", subscriber.UnsubscribeToken, form, Now.AddDays(1), disabled));
        Assert.True(await service.ApplyAsync(1, "unsubscribe", subscriber.UnsubscribeToken, form, Now.AddDays(1), disabled));
        Assert.Empty(await Service().ListAsync());
        sender.FailAddress = null;
        Assert.False(await Service().DeliverNextAsync(Now.AddDays(2), Options, PublicPath, Messages, sender, default, _ => true));
    }

    [Fact]
    public async Task OneFailedRecipientDoesNotBlockOthersAndRetryRebuildsWithStableId()
    {
        var sender = new Sender(); var service = Service();
        await Request(service, sender); await Confirm(service, sender);
        await Request(service, sender, "other@example.test", "en"); await Confirm(service, sender);
        sender.Sent.Clear(); sender.FailAddress = "reader@example.test";
        Assert.True(await service.DeliverNextAsync(Now.AddDays(1), Options, PublicPath, Messages, sender, default, _ => true));
        var pending = (await service.ListAsync()).First().Pending!;
        Assert.True(await service.DeliverNextAsync(Now.AddDays(1), Options, PublicPath, Messages, sender, default, _ => true));
        Assert.Equal("other@example.test", sender.Sent.Single().Options.Recipient);
        sender.FailAddress = null;
        Assert.True(await Service().DeliverNextAsync(Now.AddDays(1).AddHours(2), Options, PublicPath,
            Messages, sender, default, _ => true));
        Assert.Equal(pending.Id, sender.Sent.Last().Message.Id); Assert.Equal(pending.Body, sender.Sent.Last().Message.Body);
    }

    [Fact]
    public async Task EmptyDaysAdvanceCursorWithoutMailAndDisabledDeliveryDoesNotAdvance()
    {
        var sender = new Sender(); var service = Service(); await Request(service, sender); await Confirm(service, sender);
        sender.Sent.Clear(); var disabled = Options; disabled.DeliveryEnabled = false;
        Assert.False(await service.DeliverNextAsync(Now.AddDays(1), disabled, PublicPath, Messages, sender, default, _ => true));
        Assert.Equal("2026-10-03", (await service.ListAsync()).Single().NextDay);
        Assert.True(await service.DeliverNextAsync(Now.AddDays(1), Options, PublicPath, (_, _, _) => new(), sender, default, _ => true));
        Assert.Empty(sender.Sent); Assert.Equal("2026-10-04", (await service.ListAsync()).Single().NextDay);
    }

    [Fact]
    public async Task ScheduleUsesServerTimezoneAndWaitsUntilDeliveryHour()
    {
        var sender = new Sender(); var service = Service(); await Request(service, sender); await Confirm(service, sender);
        sender.Sent.Clear();
        Assert.False(await service.DeliverNextAsync(DateTimeOffset.Parse("2026-10-04T05:59:00Z"), Options, PublicPath, Messages, sender, default, _ => true));
        Assert.True(await service.DeliverNextAsync(DateTimeOffset.Parse("2026-10-04T06:00:00Z"), Options, PublicPath, Messages, sender, default, _ => true));
        Assert.Single(sender.Sent);
    }

    [Fact]
    public async Task AdminRemovalDeletesSubscriberAndPendingDelivery()
    {
        var sender = new Sender(); var service = Service(); await Request(service, sender); await Confirm(service, sender);
        await service.RemoveAsync((await service.ListAsync()).Single().Id);
        Assert.Empty(await Service().ListAsync());
        Assert.False(await service.DeliverNextAsync(Now.AddDays(1), Options, PublicPath, Messages, sender, default, _ => true));
    }

    [Theory]
    [InlineData("Name <reader@example.test>")]
    [InlineData("reader@example.test\r\nBcc:other@example.test")]
    [InlineData("a@example.test,b@example.test")]
    public async Task RejectsMultipleAddressesDisplayNamesAndHeaderInjection(string email)
    {
        var sender = new Sender(); var service = Service();
        await Assert.ThrowsAsync<ArgumentException>(() => Request(service, sender, email));
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public void MissingOrCorruptSubscriberStoreNeverSilentlyResets()
    {
        var store = Store; store.Save(new()); store.Save(new()); File.Delete(Path.Combine(directory, "subscribers.json"));
        Assert.Throws<InvalidDataException>(() => store.Load());
        File.WriteAllText(Path.Combine(directory, "subscribers.json"), "{bad");
        Assert.Throws<JsonException>(() => store.Load());
    }
    [Fact]
    public void SubscriberReportsUseObservedCalendarDaysInPreferredLanguageWithoutOldCatalogBackfill()
    {
        var state = new DigestState();
        var item = new MediaEntry { LibraryId = "1", LibraryName = "Films", ItemId = 1, Title = "Le film", DateCreated = Now.AddYears(-1) };
        state.Inventory.Add(item);
        state.Inventory.Add(new MediaEntry { LibraryId = "1", ItemId = 99, Title = "Unobserved catalog item", DateCreated = Now });
        state.History.Add(new LibraryChange { Item = item, Added = true, ObservedAt = Now.AddDays(-1) });
        state.Changes.Add(new LibraryChange { Item = item, Added = false, ObservedAt = DateTimeOffset.Parse("2026-10-03T22:30:00Z") });
        var store = new AtomicStateStore(Path.Combine(directory, "digest.json"), json => JsonSerializer.Deserialize<DigestState>(json)!, value => JsonSerializer.Serialize(value));
        store.Save(state);
        var engine = new DigestEngine(store);
        var messages = engine.SubscriptionMessages(new DateTime(2026, 10, 3), Now.AddDays(2), Options.ForRecipient("en@example.test", "en"));
        var message = Assert.Single(messages);
        Assert.Equal("2026-10-04", message.Day); Assert.Contains("Removed:", message.Body);
        Assert.DoesNotContain("Unobserved catalog", message.Body);
        Assert.Empty(engine.SubscriptionMessages(new DateTime(2026, 10, 3), Now.AddDays(1), Options));
        var french = Assert.Single(engine.SubscriptionMessages(new DateTime(2026, 10, 3), Now.AddDays(2), Options));
        Assert.Contains("Retraits :", french.Body);
        Assert.Single(store.Load().History); Assert.Single(store.Load().Changes); Assert.Empty(store.Load().Outbox);
    }

    [Fact]
    public async Task MerelyOpeningConfirmationAndUnsubscribeFormsDoesNotChangeMembership()
    {
        var sender = new Sender(); var service = Service(); await Request(service, sender);
        var token = ConfirmationToken(sender);
        _ = await service.FormTokenAsync("confirm:1:" + token, Now);
        Assert.False((await service.ListAsync()).Single().Confirmed);
        await Confirm(service, sender);
        var unsubscribe = (await service.ListAsync()).Single().UnsubscribeToken;
        _ = await service.FormTokenAsync("unsubscribe:1:" + unsubscribe, Now);
        Assert.True((await service.ListAsync()).Single().Confirmed);
    }

    [Fact]
    public async Task BacklogSendsAtMostOneReportPerSubscriberPerCalendarDayAcrossRestarts()
    {
        var sender = new Sender(); var service = Service(); await Request(service, sender); await Confirm(service, sender);
        sender.Sent.Clear();
        var due = Now.AddDays(4);
        Assert.True(await service.DeliverNextAsync(due, Options, PublicPath, Messages, sender, default, _ => true));
        var id = sender.Sent.Single().Message.Id;
        Assert.False(await service.DeliverNextAsync(due.AddMinutes(5), Options, PublicPath, Messages, sender, default, _ => true));
        Assert.False(await Service().DeliverNextAsync(due.AddHours(1), Options, PublicPath, Messages, sender, default, _ => true));
        Assert.Single(sender.Sent);
        Assert.True(await Service().DeliverNextAsync(due.AddDays(1), Options, PublicPath, Messages, sender, default, _ => true));
        Assert.Equal(2, sender.Sent.Count); Assert.NotEqual(id, sender.Sent.Last().Message.Id);
        Assert.Equal(new[] { "2026-10-03", "2026-10-04" }, sender.Sent.Select(s => s.Message.Day));
    }

    [Fact]
    public async Task ArchiveResetRegenerationAndPreviewNeverChangeSubscriberDeliveryState()
    {
        var sender = new Sender(); var service = Service(); await Request(service, sender); await Confirm(service, sender);
        var digestPath = Path.Combine(directory, "digest.json");
        var digestStore = new AtomicStateStore(digestPath, json => JsonSerializer.Deserialize<DigestState>(json)!, value => JsonSerializer.Serialize(value));
        var item = new MediaEntry { ItemId = 1, LibraryId = "films", Kind = "Movie", Title = "Daily movie" };
        var state = new DigestState { Changes = new() { new LibraryChange { Item = item, Added = true, ObservedAt = Now } }, Inventory = new() { item } };
        // Development queues remain readable but cannot be dispatched by this version.
        state.Outbox.Add(new DigestMessage { Body = "Retired daily mail" });
        state.HistoricalBatch = new HistoricalBatch { Pending = new() { new DigestMessage { Body = "Retired manual mail" } } };
        digestStore.Save(state);
        var archive = new ReportArchive(Path.Combine(directory, "reports"), json => JsonSerializer.Deserialize<ReportManifest>(json)!, value => JsonSerializer.Serialize(value));
        var engine = new DigestEngine(digestStore, "server", archive);
        engine.ArchiveCompletedDays(Now.AddDays(1), Options);
        sender.Sent.Clear();
        await service.DeliverNextAsync(Now.AddDays(1), Options, PublicPath,
            (_, first, options) => engine.SubscriptionMessages(first, Now.AddDays(1), options), sender, default, _ => true);
        var before = File.ReadAllText(Path.Combine(directory, "subscribers.json"));
        for (var i = 0; i < 2; i++)
        {
            archive.Reset(Now.AddDays(1), Options);
            engine.GenerateReports("2026-10-01", "2026-10-04", Now.AddDays(1), Options);
            engine.PublishTrackedReports(Now.AddDays(1), Options);
            engine.Preview(Now.AddDays(1), Options);
        }
        Assert.Equal(before, File.ReadAllText(Path.Combine(directory, "subscribers.json")));
        Assert.False(await Service().DeliverNextAsync(Now.AddDays(1).AddHours(1), Options, PublicPath, Messages, sender, default, _ => true));
        Assert.Single(sender.Sent);
        Assert.Equal("Retired daily mail", Assert.Single(digestStore.Load().Outbox).Body);
        Assert.Equal("Retired manual mail", Assert.Single(digestStore.Load().HistoricalBatch!.Pending).Body);
    }

    [Fact]
    public async Task SaveFailureBeforeSmtpAndCancellationCannotSendSubscriberMail()
    {
        var sender = new Sender(); var service = Service(); await Request(service, sender); await Confirm(service, sender);
        sender.Sent.Clear();
        var failing = new SubscriptionService(new SubscriptionStore(Path.Combine(directory, "subscribers.json"),
            json => JsonSerializer.Deserialize<SubscriptionState>(json)!, _ => throw new IOException("Disk full")));
        await Assert.ThrowsAsync<IOException>(() => failing.DeliverNextAsync(Now.AddDays(1), Options, PublicPath, Messages, sender, default, _ => true));
        Assert.Empty(sender.Sent); Assert.Null((await Service().ListAsync()).Single().Pending);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.DeliverNextAsync(Now.AddDays(1), Options, PublicPath, Messages, sender, cancelled.Token, _ => true));
        Assert.Empty(sender.Sent);
    }

    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
