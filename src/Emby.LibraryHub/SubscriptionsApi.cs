using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.LibraryHub.Core;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

namespace Emby.LibraryHub;

[Route("/LibraryHub/Community/Subscription", "GET")]
public sealed class GetMySubscription : IReturn<MySubscriptionInfo>
{
    public string Action { get; set; } = "subscribe";
    public string Token { get; set; } = "";
}
[Route("/LibraryHub/Community/Subscription", "POST")]
public sealed class UpdateMySubscription : IReturn<SubscriptionResult>
{
    public string Action { get; set; } = "subscribe";
    public string Token { get; set; } = "";
    public string FormToken { get; set; } = "";
    public string Email { get; set; } = "";
    public string Language { get; set; } = "fr";
}
public sealed class MySubscriptionInfo
{
    public bool Enabled { get; set; }
    public string FormToken { get; set; } = "";
    public List<SubscriberInfo> Subscriptions { get; set; } = new();
}
public sealed class SubscriptionResult
{
    public bool Success { get; set; }
    public string ErrorCode { get; set; } = "";
}

[Authenticated]
public sealed class CommunitySubscriptionsApi : IService, IRequiresRequest
{
    private readonly IAuthorizationContext authorization;
    public CommunitySubscriptionsApi(IAuthorizationContext authorization) => this.authorization = authorization;
    public IRequest Request { get; set; } = null!;
    private long UserId()
    {
        Request.Response.AddHeader("Cache-Control", "no-store");
        return DigestCoordinator.Instance.Access.Require(authorization.GetAuthorizationInfo(Request).UserId);
    }
    private static string Purpose(string action, long userId, string token)
    {
        if (action != "subscribe" && action != "confirm" && action != "unsubscribe") throw new ArgumentException("Invalid action.");
        if (token == null || token.Length > 64) throw new ArgumentException("Invalid link.");
        return action + ":" + userId + (action == "subscribe" ? "" : ":" + token);
    }
    // Emby 4.11 reliably unwraps Task<object>; other async task results can become HTTP 204.
    public async Task<object> Get(GetMySubscription request)
    {
        var id = UserId();
        var service = DigestCoordinator.Instance.Subscriptions;
        var list = await service.ListAsync().ConfigureAwait(false);
        return new MySubscriptionInfo
        {
            Enabled = Plugin.Instance.Configuration.Digest.SubscriptionsEnabled,
            FormToken = await service.FormTokenAsync(Purpose(request.Action, id, request.Token), DateTimeOffset.UtcNow).ConfigureAwait(false),
            Subscriptions = list.Where(s => s.EmbyUserId == id || s.PendingEmbyUserId == id)
                .Select(s => new SubscriberInfo { Id = s.Id, Email = s.Email, Language = s.Language,
                    Status = s.Confirmed && s.EmbyUserId == id ? "Subscribed" : "Awaiting confirmation" }).ToList()
        };
    }
    public async Task<object> Post(UpdateMySubscription request)
    {
        var id = UserId();
        _ = Purpose(request.Action, id, request.Token);
        var service = DigestCoordinator.Instance.Subscriptions;
        var options = Plugin.Instance.Configuration.Digest;
        if (request.Action == "subscribe")
        {
            if (!options.SubscriptionsEnabled) return new SubscriptionResult { ErrorCode = "paused" };
            try
            {
                var sent = await service.RequestAsync(id, request.Email, request.Language, request.FormToken,
                    DigestCoordinator.Instance.SubscriptionPath, DateTimeOffset.UtcNow,
                    options, new SmtpDigestSender(), CancellationToken.None).ConfigureAwait(false);
                return new SubscriptionResult { Success = sent, ErrorCode = sent ? "" : "addressInUse" };
            }
            catch (ArgumentException error)
            {
                return new SubscriptionResult { ErrorCode = error.ParamName switch
                { "email" => "invalidEmail", "language" => "invalidLanguage", "form" => "expiredForm", _ => "requestFailed" } };
            }
            catch (Exception error)
            {
                DigestCoordinator.Instance.LogSubscriptionFailure(error);
                return new SubscriptionResult { ErrorCode = "requestFailed" };
            }
        }
        return new SubscriptionResult { Success = await service.ApplyAsync(id, request.Action, request.Token,
            request.FormToken, DateTimeOffset.UtcNow, options).ConfigureAwait(false) };
    }
}

[Route("/LibraryHub/Community/Subscription/Remove", "POST")]
public sealed class RemoveMySubscription : IReturn<SubscriptionResult> { public string Id { get; set; } = ""; }

[Authenticated]
public sealed class CommunityUnsubscribeApi : IService, IRequiresRequest
{
    private readonly IAuthorizationContext authorization;
    public CommunityUnsubscribeApi(IAuthorizationContext authorization) => this.authorization = authorization;
    public IRequest Request { get; set; } = null!;
    public async Task<object> Post(RemoveMySubscription request)
    {
        var id = DigestCoordinator.Instance.Access.Require(authorization.GetAuthorizationInfo(Request).UserId);
        await DigestCoordinator.Instance.Subscriptions.RemoveOwnAsync(id, request.Id).ConfigureAwait(false);
        return new SubscriptionResult { Success = true };
    }
}

public sealed class SubscriberInfo
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string Language { get; set; } = "";
    public string Status { get; set; } = "";
    public string Error { get; set; } = "";
}
public sealed class SubscribersInfo
{
    public string Path { get; set; } = "";
    public List<SubscriberInfo> Subscribers { get; set; } = new();
}
[Route("/LibraryHub/Subscribers", "GET")]
public sealed class GetSubscribers : IReturn<SubscribersInfo> { }
[Route("/LibraryHub/Subscribers/Remove", "POST")]
public sealed class RemoveSubscriber : IReturn<SubscribersInfo> { public string Id { get; set; } = ""; }

[Authenticated(Roles = "Admin")]
public sealed class SubscribersAdminApi : IService
{
    public async Task<SubscribersInfo> Get(GetSubscribers request)
    {
        var list = await DigestCoordinator.Instance.Subscriptions.ListAsync().ConfigureAwait(false);
        return new SubscribersInfo
        {
            Path = DigestCoordinator.Instance.SubscriptionPath,
            Subscribers = list.Select(s => new SubscriberInfo { Id = s.Id, Email = s.Email, Language = s.Language,
                Status = s.EmbyUserId == 0 ? "Needs account confirmation" : s.Confirmed ? "Subscribed" : s.Expires <= DateTimeOffset.UtcNow ? "Expired" : "Awaiting confirmation", Error = s.LastError }).ToList()
        };
    }
    public async Task<SubscribersInfo> Post(RemoveSubscriber request)
    {
        if (!Guid.TryParseExact(request.Id, "N", out _)) throw new ArgumentException("Invalid subscriber.");
        await DigestCoordinator.Instance.Subscriptions.RemoveAsync(request.Id).ConfigureAwait(false);
        return await Get(new GetSubscribers()).ConfigureAwait(false);
    }
}
