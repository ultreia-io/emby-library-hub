using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace Emby.LibraryHub.Core;

public static class ReportHtml
{
    private static string E(string value) => WebUtility.HtmlEncode(value);

    public static string Day(DigestMessage message, string language, bool partial, string? indexUrl = null)
    {
        var fr = language == "fr";
        var body = new StringBuilder();
        body.Append("<nav><a target=\"_top\" href=\"").Append(E(indexUrl ?? "index.html")).Append("\">← ").Append(fr ? "Tous les rapports" : "All reports").Append("</a></nav>");
        body.Append("<header><p class=\"eyebrow\">LIBRARY HUB</p><h1>").Append(E(message.Subject)).Append("</h1>");
        body.Append("<p class=\"muted\">").Append(partial
            ? (fr ? "Journée en cours · changements enregistrés jusqu’à présent" : "Today so far · recorded changes")
            : (fr ? "Rapport quotidien · de 00:00 à 23:59" : "Daily report · 00:00 to 23:59")).Append("</p></header>");
        foreach (var section in message.Body.Replace("\r\n", "\n").Split("\n\n").Skip(1))
        {
            var lines = section.Split('\n');
            if (lines.Length < 2) continue;
            body.Append("<section class=\"library\"><h2>").Append(E(lines[0])).Append("</h2>");
            var listOpen = false;
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("• ", StringComparison.Ordinal))
                {
                    if (!listOpen) { body.Append("<ul>"); listOpen = true; }
                    var title = line.Substring(2);
                    var link = i + 1 < lines.Length && lines[i + 1].StartsWith("  ", StringComparison.Ordinal)
                        ? lines[i + 1].Trim() : "";
                    body.Append("<li>");
                    if (SafeLink(link))
                    {
                        body.Append("<a class=\"media\" target=\"_top\" rel=\"noreferrer\" href=\"")
                            .Append(E(link)).Append("\">").Append(E(title)).Append(" <span aria-hidden=\"true\">↗</span></a>");
                        i++;
                    }
                    else body.Append("<span class=\"media\">").Append(E(title)).Append("</span>");
                    body.Append("</li>");
                }
                else if (line == "Ajouts :" || line == "Added:" || line == "Retraits :" || line == "Removed:")
                {
                    if (listOpen) { body.Append("</ul>"); listOpen = false; }
                    body.Append("<h3 class=\"").Append(line == "Ajouts :" || line == "Added:" ? "added" : "removed")
                        .Append("\">").Append(E(line)).Append("</h3>");
                }
            }
            if (listOpen) body.Append("</ul>");
            body.Append("</section>");
        }
        body.Append("<footer>").Append(fr ? "La lecture des vidéos nécessite votre compte Emby." : "Watching videos requires your Emby account.")
            .Append("</footer>");
        return Page(message.Subject, language, body.ToString());
    }

    public static string Index(ReportManifest manifest, string? privatePageUrl = null)
    {
        var fr = manifest.Language == "fr";
        var title = fr ? "Les nouvelles de la médiathèque" : "Media library updates";
        var body = new StringBuilder("<header><p class=\"eyebrow\">LIBRARY HUB</p><h1>");
        body.Append(E(title)).Append("</h1><p class=\"muted\">")
            .Append(fr ? "Les ajouts et retraits, jour après jour." : "Additions and removals, day by day.").Append("</p></header>");
        body.Append("<p><a target=\"_top\" href=\"").Append(E(CommunityLinks.SubscriptionRelativeWebPath)).Append("\">")
            .Append(fr ? "Recevoir les nouvelles par email →" : "Subscribe to email updates →").Append("</a></p>");
        if (manifest.Reports.Count == 0)
            body.Append("<p class=\"empty\">").Append(fr ? "Aucun rapport publié pour le moment." : "No reports have been published yet.").Append("</p>");
        var culture = CultureInfo.GetCultureInfo(fr ? "fr-FR" : "en-GB");
        foreach (var month in manifest.Reports.OrderByDescending(r => r.Day, StringComparer.Ordinal).GroupBy(r => r.Day.Substring(0, 7)))
        {
            var first = DateTime.ParseExact(month.Key + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
            body.Append("<section><h2 class=\"month\">").Append(E(first.ToString("MMMM yyyy", culture))).Append("</h2><div class=\"days\">");
            foreach (var entry in month)
            {
                var day = DateTime.ParseExact(entry.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                body.Append("<a class=\"day\" target=\"_top\" href=\"").Append(E(privatePageUrl == null ? entry.Day + ".html" : privatePageUrl.TrimEnd('/') + "/" + entry.Day)).Append("\"><time datetime=\"")
                    .Append(entry.Day).Append("\">").Append(E(day.ToString("dddd d", culture))).Append("</time>")
                    .Append("<span class=\"muted\">").Append(entry.Count).Append(fr ? " entrées" : " entries")
                    .Append(entry.Partial ? (fr ? " · en cours" : " · in progress") : "").Append("</span><span class=\"arrow\">→</span></a>");
            }
            body.Append("</div></section>");
        }
        body.Append("<footer>").Append(fr ? "Les liens vers les médias s’ouvrent dans Emby." : "Media links open in Emby.").Append("</footer>");
        return Page(title, manifest.Language, body.ToString());
    }

    private static bool SafeLink(string link) => Uri.TryCreate(link, UriKind.Absolute, out var url) &&
        (url.Scheme == "https" || url.Scheme == "http") && string.IsNullOrEmpty(url.UserInfo);

    public static string Page(string title, string language, string body) =>
        "<!doctype html><html lang=\"" + (language == "fr" ? "fr" : "en") + "\"><head><meta charset=\"utf-8\">" +
        "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><meta name=\"robots\" content=\"noindex,nofollow,noarchive\">" +
        "<meta name=\"referrer\" content=\"no-referrer\"><title>" + E(title) + "</title><style>" + Css +
        "</style></head><body><main>" + body + BrandFooter + "</main></body></html>";

    public const string BrandFooter = """
        <footer class="hub-footer" style="margin-top:36px;padding:18px 0;border-top:1px solid #35443a;font-size:13px">© 2026 Tony Chemit · <a href="https://ultreia.io" target="_top" rel="noopener noreferrer">ultreia.io</a> · Library Hub</footer>
        """;

    private const string Css = """
        :root{color-scheme:light dark;--bg:#f6f8f5;--card:#fff;--ink:#17251c;--muted:#5b6a60;--line:#dce5dd;--accent:#20783b}
        *{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--ink);font:16px/1.6 system-ui,-apple-system,sans-serif}
        main{width:100%;max-width:920px;min-height:100vh;margin:auto;padding:36px 24px 48px;display:flex;flex-direction:column}main>*{flex-shrink:0}main>.hub-footer{margin-top:auto!important}a{color:var(--accent);text-decoration:none;overflow-wrap:anywhere}
        a:hover{text-decoration:underline}a:focus-visible{outline:3px solid var(--accent);outline-offset:4px;border-radius:4px}
        nav{margin-bottom:32px}header{margin-bottom:40px}.eyebrow{letter-spacing:.16em;font-size:12px;font-weight:750;color:var(--accent)}
        h1{font-size:clamp(27px,5vw,42px);line-height:1.16;letter-spacing:-.035em;max-width:760px;margin:12px 0 16px}
        h2{font-size:23px;margin:0 0 20px}h3{font-size:13px;letter-spacing:.05em;margin:20px 0 10px}
        .muted,footer{color:var(--muted)}.month{text-transform:capitalize;margin-top:34px}.days{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}
        .day{display:grid;grid-template-columns:1fr auto;background:var(--card);border:1px solid var(--line);border-radius:14px;padding:19px 22px;color:var(--ink)}
        .day time{text-transform:capitalize;font-weight:650}.day .muted{font-size:14px;grid-row:2}.arrow{grid-column:2;grid-row:1/3;align-self:center;color:var(--accent)}
        .library{padding:25px 28px;margin-bottom:20px;background:var(--card);border:1px solid var(--line);border-radius:16px}
        .added{color:var(--accent)}.removed{color:#ac5334}ul{list-style:none;padding:0;margin:0}li+li{border-top:1px solid var(--line)}
        .media{display:block;padding:11px 0;overflow-wrap:anywhere}.media span{font-size:13px;margin-left:5px}.empty{padding:28px;border:1px dashed var(--line);border-radius:16px}
        form{max-width:520px}label{display:block;font-weight:600;margin:18px 0 6px}input,select,button{font:inherit;border-radius:8px;padding:12px;border:1px solid var(--line)}
        input,select{width:100%;background:var(--card);color:var(--ink)}button{background:var(--accent);color:var(--bg);font-weight:700;cursor:pointer;margin-top:14px}
        footer{font-size:13px;margin-top:36px}@media(max-width:600px){main{padding:24px 16px}.days{grid-template-columns:1fr}.library{padding:20px}header{margin-bottom:28px}}
        @media(prefers-color-scheme:dark){:root{--bg:#111813;--card:#1b241e;--ink:#e8efe9;--muted:#a5b7aa;--line:#35443a;--accent:#86d19a}.removed{color:#efa685}}
        """;
}
