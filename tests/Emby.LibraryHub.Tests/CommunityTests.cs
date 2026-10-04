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

public sealed class CommunityTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "digest-private-" + Guid.NewGuid().ToString("N"));
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-03T12:00:00Z");
    private static DigestOptions Options => new() { SubscriptionsEnabled = true, DeliveryEnabled = true,
        Language = "en", PublicServerUrl = "https://media.example.test", TimeZoneId = "Etc/UTC" };
    private SubscriptionStore Store => new(Path.Combine(directory, "subscribers.json"),
        json => JsonSerializer.Deserialize<SubscriptionState>(json)!, state => JsonSerializer.Serialize(state));
    private SubscriptionService Service => new(Store);
    private sealed class Sender : IDigestSender
    {
        public List<DigestMessage> Sent { get; } = new();
        public bool Fail;
        public Task SendAsync(DigestMessage message, DigestOptions options, CancellationToken token)
        {
            if (Fail) throw new IOException("Test failure");
            Sent.Add(JsonSerializer.Deserialize<DigestMessage>(JsonSerializer.Serialize(message))!);
            return Task.CompletedTask;
        }
    }
    private async Task<string> Request(long user, Sender sender, string email = "member@example.test")
    {
        var service = Service;
        var proof = await service.FormTokenAsync("subscribe:" + user, Now);
        await service.RequestAsync(user, email, "en", proof, CommunityLinks.SubscriptionWebPath, Now, Options, sender, default);
        return sender.Sent.Last().Body.Split("&token=")[1].Split('&')[0];
    }
    private async Task<bool> Confirm(long user, string token) => await Service.ApplyAsync(user, "confirm", token,
        await Service.FormTokenAsync("confirm:" + user + ":" + token, Now), Now, Options);
    private static List<DigestMessage> Messages(long user, DateTime first, DigestOptions options) => new()
    { new DigestMessage { Day = first.ToString("yyyy-MM-dd"), Subject = "Report", Body = "Allowed title" } };
    private Task<bool> Deliver(Sender sender, Func<long, bool> allowed, Func<long, DateTime, DigestOptions, List<DigestMessage>>? messages = null) =>
        Service.DeliverNextAsync(Now.AddDays(1).AddHours(2), Options, CommunityLinks.SubscriptionWebPath, messages ?? Messages, sender, default, allowed);

    [Fact]
    public async Task AnonymousSignupAndCrossAccountProofAreRejectedWithoutSending()
    {
        var sender = new Sender();
        var proof = await Service.FormTokenAsync("subscribe:1", Now);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service.RequestAsync(0, "member@example.test", "en", proof, CommunityLinks.SubscriptionWebPath, Now, Options, sender, default));
        await Assert.ThrowsAsync<ArgumentException>(() => Service.RequestAsync(2, "member@example.test", "en", proof, CommunityLinks.SubscriptionWebPath, Now, Options, sender, default));
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task ConfirmationAndSelfRemovalAreBoundToTheRequestingAccount()
    {
        var sender = new Sender(); var token = await Request(1, sender);
        Assert.False(await Confirm(2, token));
        Assert.True(await Confirm(1, token));
        var subscriber = Assert.Single(await Service.ListAsync());
        Assert.Equal(1, subscriber.EmbyUserId);
        await Service.RemoveOwnAsync(2, subscriber.Id);
        Assert.Single(await Service.ListAsync());
        await Service.RemoveOwnAsync(1, subscriber.Id);
        Assert.Empty(await Service.ListAsync());
    }

    [Fact]
    public async Task LegacyMembersAndPlaintextQueuesCannotSendUntilFreshAccountConfirmation()
    {
        Store.Save(new SubscriptionState { Subscribers = new() { new Subscriber { Email = "member@example.test",
            Confirmed = true, NextDay = "2011-01-01", Pending = new DigestMessage { Day = "2011-01-01", Body = "OLD PRIVATE TITLE" } } } });
        var sender = new Sender();
        Assert.False(await Deliver(sender, _ => true, (_, _, _) => throw new Exception("Legacy body must never dispatch")));
        Assert.Empty(sender.Sent);
        var token = await Request(1, sender); Assert.True(await Confirm(1, token));
        var member = Assert.Single(await Service.ListAsync());
        Assert.Null(member.Pending); Assert.Equal("2026-10-03", member.NextDay);
        sender.Sent.Clear(); Assert.True(await Deliver(sender, id => id == 1));
        Assert.DoesNotContain("OLD PRIVATE", sender.Sent.Single().Body);
        Assert.Contains("emby/LibraryHub/Subscriptions", sender.Sent.Single().Body);
        Assert.DoesNotContain("api_key", sender.Sent.Single().Body);
    }

    [Fact]
    public async Task DisabledOrDeletedAccountsNeverDeliverEvenWithAPendingMessage()
    {
        var sender = new Sender(); Assert.True(await Confirm(1, await Request(1, sender)));
        var state = Store.Load(); state.Subscribers.Single().Pending = new DigestMessage { Day = "2026-10-03", Body = "Old body" }; Store.Save(state);
        sender.Sent.Clear();
        Assert.False(await Deliver(sender, _ => false, (_, _, _) => throw new Exception("Must not prepare a disabled account report")));
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task RetryRechecksPermissionsAndNeverSendsSavedUnauthorizedBody()
    {
        var sender = new Sender(); Assert.True(await Confirm(1, await Request(1, sender)));
        sender.Sent.Clear(); sender.Fail = true;
        Assert.True(await Service.DeliverNextAsync(Now.AddDays(1), Options, CommunityLinks.SubscriptionWebPath,
            (_, first, _) => new() { new DigestMessage { Day = first.ToString("yyyy-MM-dd"), Body = "Now revoked title" } }, sender, default, _ => true));
        var id = Store.Load().Subscribers.Single().Pending!.Id;
        sender.Fail = false; Assert.True(await Deliver(sender, _ => true));
        Assert.Equal(id, sender.Sent.Single().Id);
        Assert.Contains("Allowed title", sender.Sent.Single().Body);
        Assert.DoesNotContain("revoked", sender.Sent.Single().Body);
    }

    [Fact]
    public async Task FullyRevokedReportIsDroppedWithoutSendingAndPermissionFailuresFailClosed()
    {
        var sender = new Sender(); Assert.True(await Confirm(1, await Request(1, sender)));
        var state = Store.Load(); state.Subscribers.Single().Pending = new DigestMessage { Day = "2026-10-03", Body = "Secret" }; Store.Save(state);
        sender.Sent.Clear();
        await Assert.ThrowsAsync<IOException>(() => Deliver(sender, _ => true, (_, _, _) => throw new IOException("Policy lookup failed")));
        Assert.Empty(sender.Sent);
        Assert.True(await Deliver(sender, _ => true, (_, _, _) => new()));
        Assert.Empty(sender.Sent); Assert.Null(Store.Load().Subscribers.Single().Pending);
    }

    [Fact]
    public async Task ConfirmedAddressCannotBeTakenOverByAnotherAccount()
    {
        var sender = new Sender(); Assert.True(await Confirm(1, await Request(1, sender)));
        var at = Now.AddHours(2); var service = Service;
        Assert.False(await service.RequestAsync(2, "member@example.test", "fr", await service.FormTokenAsync("subscribe:2", at), CommunityLinks.SubscriptionWebPath, at, Options, sender, default));
        Assert.Single(sender.Sent); Assert.Equal(1, Store.Load().Subscribers.Single().EmbyUserId);
        Assert.Equal(0, Store.Load().Subscribers.Single().PendingEmbyUserId);
    }

    [Fact]
    public void SubscriberEmailsStillRespectLibraryPermissions()
    {
        var allowed = new MediaEntry { ItemId = 1, LibraryId = "1", LibraryName = "Members", Title = "Allowed film", Kind = "Movie" };
        var secret = new MediaEntry { ItemId = 2, LibraryId = "2", LibraryName = "SECRET LIBRARY", Title = "SECRET FILM", Kind = "Movie" };
        var store = new AtomicStateStore(Path.Combine(directory, "state.json"), json => JsonSerializer.Deserialize<DigestState>(json)!, state => JsonSerializer.Serialize(state));
        store.Save(new DigestState { Inventory = new() { allowed, secret }, History = new() {
            new LibraryChange { Item = allowed, Added = true, ObservedAt = Now.AddDays(-1) },
            new LibraryChange { Item = secret, Added = true, ObservedAt = Now.AddDays(-1) },
            new LibraryChange { Item = secret, Added = false, ObservedAt = Now } } });
        var engine = new DigestEngine(store);
        bool Visible(MediaEntry item) => item.LibraryId == "1";
        var messages = engine.SubscriptionMessages(new DateTime(2026, 10, 1), Now.AddDays(1), Options, Visible);
        Assert.Single(messages); Assert.DoesNotContain("SECRET", messages[0].Body);
    }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
}
