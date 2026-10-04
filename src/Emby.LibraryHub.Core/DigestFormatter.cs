using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Emby.LibraryHub.Core;

public sealed class DigestFormatter
{
    public DigestMessage Format(DateTime day, IEnumerable<LibraryChange> changes, string language, string publicServerUrl = "", string serverId = "", ISet<string>? availableKeys = null)
    {
        if (language != "fr" && language != "en") throw new ArgumentException("Unsupported language.");
        var french = language == "fr";
        var culture = CultureInfo.GetCultureInfo(french ? "fr-FR" : "en-GB");
        var heading = (french ? "Médiathèque" : "Media library") + " — " + day.ToString("d MMMM yyyy", culture);
        var allChanges = changes.ToList();
        var removedKeys = new HashSet<string>(allChanges.GroupBy(c => c.Item.Key)
            .Where(g => availableKeys != null ? !availableKeys.Contains(g.Key) : !g.Last().Added).Select(g => g.Key));
        var body = new StringBuilder(heading);
        foreach (var library in allChanges.GroupBy(c => c.Item.LibraryId)
                     .OrderBy(g => g.Last().Item.LibraryName, StringComparer.Create(culture, true)))
        {
            body.Append("\n\n").Append(Clean(library.Last().Item.LibraryName));
            foreach (var added in new[] { true, false })
            {
                var items = library.Where(c => c.Added == added).Select(c => c.Item)
                    .GroupBy(i => i.Key).Select(g => g.Last()).ToList();
                if (items.Count == 0) continue;
                body.Append('\n').Append(french ? (added ? "Ajouts :" : "Retraits :") : (added ? "Added:" : "Removed:"));
                foreach (var line in Lines(items, french, added ? publicServerUrl : "", serverId, removedKeys).Distinct().OrderBy(x => x, StringComparer.Create(culture, true)))
                    body.Append("\n• ").Append(line);
            }
        }
        return new DigestMessage
        {
            Day = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Subject = heading,
            Body = body.ToString()
        };
    }

    private static IEnumerable<string> Lines(List<MediaEntry> items, bool french, string baseUrl, string serverId, HashSet<string> removedKeys)
    {
        var episodes = items.Where(i => i.Kind == "Episode" && !string.IsNullOrWhiteSpace(i.SeriesName)).ToList();
        foreach (var item in items.Except(episodes))
            yield return Clean(item.Title) + (item.Kind == "Movie" && item.Year.HasValue ? " (" + item.Year + ")" : "")
                + Link(item, baseUrl, serverId, removedKeys);
        foreach (var series in episodes.GroupBy(i => new { Series = SeriesId(i) > 0
                     ? "id:" + i.SeriesId : "name:" + Clean(i.SeriesName), i.Season }))
        {
            var item = series.Last();
            var line = Clean(item.SeriesName) + (item.Season.HasValue
                ? " — " + (french ? "Saison " : "Season ") + item.Season.Value.ToString(CultureInfo.InvariantCulture) : "");
            // A surviving added episode implies that its parent series is available.
            // Unknown series IDs and removals remain title-only; never link individual episodes.
            if (SeriesId(item) > 0 && series.Any(i => !removedKeys.Contains(i.Key)))
                line += ItemLink(SeriesId(item), baseUrl, serverId);
            yield return line;
        }
    }

    private static long SeriesId(MediaEntry item) =>
        long.TryParse(item.SeriesId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 ? id : 0;

    private static string Link(MediaEntry item, string baseUrl, string serverId, HashSet<string> removedKeys)
    {
        if (removedKeys.Contains(item.Key) ||
            !(item.Kind == "Movie" || item.Kind == "Video" || item.Kind == "MusicVideo")) return "";
        return ItemLink(item.ItemId, baseUrl, serverId);
    }

    private static string ItemLink(long itemId, string baseUrl, string serverId) => string.IsNullOrEmpty(baseUrl) ? ""
        : "\n  " + baseUrl.TrimEnd('/') + "/web/index.html#!/item?id=" + itemId.ToString(CultureInfo.InvariantCulture)
            + (string.IsNullOrEmpty(serverId) ? "" : "&serverId=" + Uri.EscapeDataString(serverId));

    // Titles are data: embedded newlines must not create fake headings or email headers.
    private static string Clean(string text) => Regex.Replace(text ?? "", @"[\s\p{Cc}]+", " ").Trim();
}
