using System;
using System.Collections.Generic;
using System.Globalization;
using Emby.LibraryHub.Core;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

namespace Emby.LibraryHub;

public sealed class ReportsInfo
{
    public string Path { get; set; } = "";
    public int Count { get; set; }
    public string Error { get; set; } = "";
}

[Route("/LibraryHub/Reports/Link", "GET")]
public sealed class GetReportsLink : IReturn<ReportsInfo> { }

// A shareable navigation link contains no token and always leads through Emby sign-in.
[Authenticated]
public sealed class ReportsLinkApi : IService
{
    public ReportsInfo Get(GetReportsLink request) => new ReportsInfo { Path = CommunityLinks.WebPath };
}

[Route("/LibraryHub/Reports/Generate", "POST")]
public sealed class GenerateReports : IReturn<ReportsInfo>
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
}

public sealed class ReportCalendarInfo { public string Today { get; set; } = ""; }

[Route("/LibraryHub/Reports/Calendar", "GET")]
public sealed class GetReportCalendar : IReturn<ReportCalendarInfo> { }

[Route("/LibraryHub/Reports/Reset", "POST")]
public sealed class ResetReports : IReturn<ReportsInfo> { }

[Authenticated(Roles = "Admin")]
public sealed class ReportsAdminApi : IService
{
    public ReportCalendarInfo Get(GetReportCalendar request) => new ReportCalendarInfo
    {
        Today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById(Plugin.Instance.Configuration.Digest.TimeZoneId))
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
    };

    public async Task<ReportsInfo> Post(ResetReports request)
    {
        var count = await DigestCoordinator.Instance.ResetReportsAsync().ConfigureAwait(false);
        return new ReportsInfo { Count = count, Path = CommunityLinks.WebPath };
    }

    public async Task<ReportsInfo> Post(GenerateReports request)
    {
        try
        {
            var count = await DigestCoordinator.Instance.GenerateReportsAsync(request.From, request.To).ConfigureAwait(false);
            return new ReportsInfo { Count = count, Path = CommunityLinks.WebPath };
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            return new ReportsInfo { Error = ex.Message };
        }
    }
}

public sealed class PrivateReportInfo { public string Html { get; set; } = ""; }

[Route("/LibraryHub/Community/Reports", "GET")]
public sealed class GetPrivateReport : IReturn<PrivateReportInfo> { public string Day { get; set; } = ""; }

[Authenticated]
public sealed class CommunityReportsApi : IService, IRequiresRequest
{
    private readonly IAuthorizationContext authorization;
    public CommunityReportsApi(IAuthorizationContext authorization) => this.authorization = authorization;
    public IRequest Request { get; set; } = null!;
    public async Task<PrivateReportInfo> Get(GetPrivateReport request)
    {
        Request.Response.AddHeader("Cache-Control", "no-store");
        Request.Response.AddHeader("Referrer-Policy", "no-referrer");
        var userId = DigestCoordinator.Instance.Access.Require(authorization.GetAuthorizationInfo(Request).UserId);
        var root = StandalonePagesApi.CanonicalRoot(Request);
        var html = await DigestCoordinator.Instance.ReadStandaloneReportAsync(userId, request.Day, root + "/Archive", root + "/Subscriptions").ConfigureAwait(false);
        if (html == null) Request.Response.StatusCode = 404;
        return new PrivateReportInfo { Html = html ?? "" };
    }
}
