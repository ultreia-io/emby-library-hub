using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

namespace Emby.LibraryHub;

[Route("/LibraryHub/Archive", "GET")]
[Route("/LibraryHub/Archive/{Day}", "GET")]
public sealed class GetArchivePage { public string Day { get; set; } = ""; }
[Route("/LibraryHub/Subscriptions", "GET")]
public sealed class GetSubscriptionPage { }
[Route("/LibraryHub/Browse", "GET")]
public sealed class GetBrowsePage { }
[Route("/LibraryHub/Search", "GET")]
public sealed class GetSearchPage { }
[Route("/LibraryHub/Assets/{Name}", "GET")]
public sealed class GetStandaloneAsset { public string Name { get; set; } = ""; }

// Only the login shell and fixed assets are anonymous. All report and membership
// data stays behind the existing authenticated APIs, including account checks.
[Unauthenticated]
public sealed class StandalonePagesApi : IService, IRequiresRequest
{
    private readonly IHttpResultFactory results;
    public StandalonePagesApi(IHttpResultFactory results) { this.results = results; }
    public IRequest Request { get; set; } = null!;
    public object Get(GetArchivePage request) => Page();
    public object Get(GetSubscriptionPage request) => Page();
    public object Get(GetBrowsePage request) => Page();
    public object Get(GetSearchPage request) => Page();
    private object Page()
    {
        var root = CanonicalRoot(Request);
        var html = Read("page.html").Replace("{{ROOT}}", WebUtility.HtmlEncode(root));
        return results.GetResult(Request, new MemoryStream(Encoding.UTF8.GetBytes(html)), "text/html; charset=utf-8", Headers());
    }
    internal static string RequestTarget(IRequest request)
    {
        var target = request.RawUrl ?? request.PathInfo;
        if (Uri.TryCreate(target, UriKind.Absolute, out var absolute) && (absolute.Scheme == "http" || absolute.Scheme == "https"))
            return absolute.PathAndQuery;
        return target;
    }
    internal static string CanonicalRoot(IRequest request)
    {
        var path = RequestTarget(request).Split('?')[0];
        var offset = path.LastIndexOf("/LibraryHub/", StringComparison.OrdinalIgnoreCase);
        return (offset >= 0 ? path.Substring(0, offset) : "/emby") + "/LibraryHub";
    }
    public object Get(GetStandaloneAsset request)
    {
        var type = request.Name switch
        {
            "app.js" or "browse.js" => "application/javascript; charset=utf-8",
            "logo.svg" or "favicon.svg" => "image/svg+xml",
            "favicon.png" => "image/png",
            _ => null
        };
        if (type == null) { Request.Response.StatusCode = 404; return ""; }
        return results.GetResult(Request,
            typeof(StandalonePagesApi).Assembly.GetManifestResourceStream("Emby.LibraryHub.Standalone." + request.Name)!, type, Headers());
    }

    private static string Read(string name)
    {
        using var stream = typeof(StandalonePagesApi).Assembly.GetManifestResourceStream("Emby.LibraryHub.Standalone." + name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    private static Dictionary<string, string> Headers() => new()
    {
        ["Cache-Control"] = "no-store",
        ["Referrer-Policy"] = "no-referrer",
        ["X-Content-Type-Options"] = "nosniff",
        ["Content-Security-Policy"] = "default-src 'none'; script-src 'self'; img-src 'self'; style-src 'unsafe-inline'; connect-src 'self'; frame-src 'self'; base-uri 'none'; form-action 'self'; frame-ancestors 'self'"
    };
}
