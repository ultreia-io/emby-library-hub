using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;

namespace Emby.LibraryHub;

public sealed class BrowseGroup
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Kind { get; set; } = "";
    public bool HasGroups { get; set; }
}

internal sealed partial class BrowseCatalog
{
    private const int BatchSize = 256;
    private BrowseItemsInfo Tree(GetBrowseItems request)
    {
        if (request.Mode != "groups" && request.Mode != "items" && request.Mode != "children") throw new ArgumentException("Invalid tree operation.");
        if (request.Group.Length > 40 || request.Group.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '/')) throw new ArgumentException("Invalid group.");
        var pattern = request.Sort switch
        {
            "SortName" => "^(?:[A-Z]|0-9|other)$",
            "Director" or "Actor" => "^(?:(?:[A-Z]|0-9|other)(?:/[1-9][0-9]{0,18})?|unknown)$",
            "CommunityRating" => "^(?:[0-9]|unknown)$",
            "ProductionYear" => "^(?:[0-9]{1,4}(?:/[0-9]{1,4})?|unknown)$",
            _ => "^(?:[0-9]{4}(?:/(?:0[1-9]|1[0-2]))?|unknown)$"
        };
        if (request.Group.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(request.Group, pattern)) throw new ArgumentException("Invalid group.");
        var query = Query(request); // Recheck current account access on every expansion.
        if (request.Mode == "children") return Children(request);
        // Preserve Emby’s default fields: IDs are needed by permission checks and
        // names, dates and ratings by grouping and rendering.
        if (IsPeopleSort(request.Sort)) return PeopleTree(request, query);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(Plugin.Instance.Configuration.Digest.TimeZoneId);
        Narrow(query, request, zone);
        var groups = new Dictionary<string, BrowseGroup>();
        var items = new List<BrowseItem>();
        var offset = request.Mode == "items" ? request.Start : 0;
        while (true)
        {
            query.StartIndex = offset; query.Limit = BatchSize;
            var batch = library.GetItemList(query, CancellationToken.None);
            var matches = MetadataMatches(request, batch);
            foreach (var item in batch)
            {
                offset++;
                if (!matches.TryGetValue(item.InternalId, out var match)) continue;
                var path = GroupPath(item, request.Sort, zone);
                if (request.Mode == "groups")
                {
                    var prefix = request.Group.Length == 0 ? "" : request.Group + "/";
                    if (!path.StartsWith(prefix, StringComparison.Ordinal) || path == request.Group) continue;
                    var key = prefix + path.Substring(prefix.Length).Split('/')[0];
                    groups.TryAdd(key, Group(key, path != key, request.Sort));
                }
                else if (path == request.Group)
                {
                    // Stop at the first item of the next page without consuming it.
                    if (items.Count == 50) return new BrowseItemsInfo { Items = items.ToArray(), Next = offset - 1 };
                    items.Add(MapMatch(item, match));
                }
            }
            if (batch.Length < BatchSize) break;
        }
        var ordered = groups.Values.OrderBy(g => g.Key == "unknown" || g.Key == "other" ? 1 : 0)
            .ThenBy(g => request.Descending ? -GroupOrder(g.Key) : GroupOrder(g.Key)).ToArray();
        return new BrowseItemsInfo { Groups = ordered.Skip(request.Start).Take(50).ToArray(), Items = items.ToArray(),
            Next = ordered.Length > request.Start + 50 ? request.Start + 50 : null };
    }
    private static int GroupOrder(string key)
    {
        var value = key.Split('/').Last();
        if (int.TryParse(value, out var number)) return number;
        if (value == "0-9") return 0;
        return value.Length == 1 ? value[0] : int.MaxValue;
    }
    private static BrowseGroup Group(string key, bool children, string sort)
    {
        var value = key.Split('/').Last();
        var kind = key == "unknown" || key == "other" ? key : sort switch
        { "SortName" => "letter", "CommunityRating" => "rating", "ProductionYear" => children ? "decade" : "year", _ => children ? "year" : "month" };
        return new BrowseGroup { Key = key, Label = value, Kind = kind, HasGroups = children };
    }
    private static string GroupPath(BaseItem item, string sort, TimeZoneInfo zone)
    {
        if (sort == "SortName")
        {
            var name = item.SortName ?? item.Name ?? "";
            if (name.Length == 0) return "other";
            var c = char.ToUpperInvariant(name[0]);
            return c >= 'A' && c <= 'Z' ? c.ToString() : c >= '0' && c <= '9' ? "0-9" : "other";
        }
        if (sort == "ProductionYear")
        {
            var year = item.ProductionYear;
            return year is > 0 and <= 9999 ? (year.Value / 10 * 10) + "/" + year : "unknown";
        }
        if (sort == "CommunityRating") return item.CommunityRating is >= 0 and <= 10 ? Math.Min(9, (int)item.CommunityRating.Value).ToString(CultureInfo.InvariantCulture) : "unknown";
        DateTimeOffset? date = sort == "DateCreated" ? item.DateCreated : item.PremiereDate;
        if (!date.HasValue || date.Value == DateTimeOffset.MinValue) return "unknown";
        // Release dates are calendar dates; only added timestamps need the server's configured timezone.
        var local = sort == "DateCreated" ? TimeZoneInfo.ConvertTime(date.Value, zone) : date.Value;
        return local.ToString("yyyy/MM", CultureInfo.InvariantCulture);
    }
    private static void Narrow(InternalItemsQuery query, GetBrowseItems request, TimeZoneInfo zone)
    {
        var parts = request.Group.Split('/');
        if (request.Sort == "SortName" && parts.Length == 1 && parts[0].Length == 1 && parts[0][0] is >= 'A' and <= 'Z') query.NameStartsWith = parts[0];
        if (request.Sort == "ProductionYear")
        {
            if (int.TryParse(parts.Last(), out var year) && year >= 0 && year <= 9999)
            {
                var years = parts.Length == 1 ? Enumerable.Range(Math.Max(1, year), Math.Min(10, 10000 - year)).ToArray() : new[] { year };
                query.Years = request.Year.HasValue ? years.Where(y => y == request.Year.Value).DefaultIfEmpty(-1).ToArray() : years;
            }
        }
        if (request.Sort == "CommunityRating" && int.TryParse(request.Group, out var rating) && rating is >= 0 and <= 9) query.MinCommunityRating = rating;
        if ((request.Sort == "DateCreated" || request.Sort == "PremiereDate") && int.TryParse(parts[0], out var dateYear) && dateYear is > 0 and < 9999)
        {
            var month = parts.Length == 2 && int.TryParse(parts[1], out var m) ? m : 1;
            if (month < 1 || month > 12) throw new ArgumentException("Invalid month.");
            var start = new DateTime(dateYear, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var end = parts.Length == 2 ? start.AddMonths(1) : start.AddYears(1);
            if (request.Sort == "DateCreated") { query.MinDateCreated = TimeZoneInfo.ConvertTimeToUtc(start, zone); query.MaxDateCreated = TimeZoneInfo.ConvertTimeToUtc(end, zone); }
            else { query.MinPremiereDate = new DateTimeOffset(start, TimeSpan.Zero); query.MaxPremiereDate = new DateTimeOffset(end.AddTicks(-1), TimeSpan.Zero); }
        }
    }
    private BrowseItemsInfo Children(GetBrowseItems request)
    {
        if (!long.TryParse(request.ParentId, out var id) || id <= 0) throw new ArgumentException("Invalid series or season.");
        var parent = library.GetItemById(id);
        if (!(parent is Series || parent is Season) || !Visible(parent)) throw new UnauthorizedAccessException();
        var seriesId = parent is Series ? id : parent.SeriesId;
        var membership = Query(new GetBrowseItems { LibraryId = request.LibraryId, BoxSetId = request.BoxSetId });
        membership.ItemIds = new[] { seriesId }; membership.IncludeItemTypes = new[] { "Series" }; membership.Limit = 1;
        if (!library.GetItemList(membership, CancellationToken.None).Any(i => i.InternalId == seriesId && Visible(i))) throw new UnauthorizedAccessException();
        var series = parent as Series ?? library.GetItemById(seriesId) as Series;
        if (series == null) throw new UnauthorizedAccessException();
        // A logical series may span several folders. Resolve only records inside
        // the selected library/collection and accessible to this member.
        var related = Query(new GetBrowseItems { LibraryId = request.LibraryId, BoxSetId = request.BoxSetId });
        related.IncludeItemTypes = new[] { "Series" };
        related.GroupByPresentationUniqueKey = false;
        if (string.IsNullOrEmpty(series.PresentationUniqueKey)) related.ItemIds = new[] { seriesId };
        else related.PresentationUniqueKey = series.PresentationUniqueKey;
        var seriesIds = new HashSet<long>();
        for (var offset = 0; ; offset += BatchSize)
        {
            related.StartIndex = offset; related.Limit = BatchSize;
            var batch = library.GetItemList(related, CancellationToken.None);
            foreach (var allowedId in library.FilterItemsToIdsForUser(batch, user, null, CancellationToken.None))
                seriesIds.Add(allowedId);
            if (batch.Length < BatchSize) break;
        }
        if (!seriesIds.Contains(seriesId)) throw new UnauthorizedAccessException();
        var query = new InternalItemsQuery(user)
        {
            SeriesIds = seriesIds.ToArray(), Recursive = true,
            IncludeItemTypes = new[] { parent is Series ? "Season" : "Episode" },
            IsVirtualItem = false, EnableTotalRecordCount = false, GroupByPresentationUniqueKey = true,
            StartIndex = request.Start, Limit = 51, OrderBy = new[] { ("IndexNumber", SortOrder.Ascending), ("SortName", SortOrder.Ascending) }
        };
        if (parent is Season season)
        {
            // Episode files can be nested in release folders or stored directly
            // under the series. Their season metadata is the reliable relationship.
            if (season.IndexNumber.HasValue) query.ParentIndexNumber = season.IndexNumber;
            else if (!string.IsNullOrEmpty(season.PresentationUniqueKey)) query.AlbumWithPresentationUniqueKey = season.PresentationUniqueKey;
            else query.ParentIds = new[] { id };
        }
        var rows = library.GetItemList(query, CancellationToken.None);
        var page = rows.Take(50).ToArray();
        var allowed = library.FilterItemsToIdsForUser(page, user, null, CancellationToken.None).ToHashSet();
        return new BrowseItemsInfo { Items = page.Where(i => allowed.Contains(i.InternalId)).Select(Map).ToArray(), Next = rows.Length > 50 ? request.Start + 50 : null };
    }
}
