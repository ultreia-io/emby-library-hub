namespace Emby.LibraryHub.Core;

public static class CommunityLinks
{
    public const string WebPath = "emby/LibraryHub/Archive";
    public const string RelativeWebPath = "/" + WebPath;
    public const string SubscriptionWebPath = "emby/LibraryHub/Subscriptions";
    public const string SubscriptionRelativeWebPath = "/" + SubscriptionWebPath;
    public static string Absolute(string baseUrl) => baseUrl.TrimEnd('/') + "/" + WebPath;
}
