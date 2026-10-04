using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Emby.LibraryHub;

internal sealed partial class BrowseCatalog
{
    private static readonly string[] SearchFields = { "Title", "All", "Overview", "Actor", "Director", "Genres", "Tags", "Studios", "Year" };

    private static void SearchOptions(InternalItemsQuery query, GetBrowseItems request)
    {
        if (string.IsNullOrWhiteSpace(request.Search) || request.Mode == "collections") return;
        var fields = new List<ItemFields>();
        if (request.SearchField is "Title" or "All") fields.Add(ItemFields.OriginalTitle);
        if (request.SearchField is "Overview" or "All") fields.Add(ItemFields.Overview);
        if (request.SearchField is "Genres" or "All") fields.Add(ItemFields.Genres);
        if (request.SearchField is "Tags" or "All") fields.Add(ItemFields.Tags);
        if (request.SearchField is "Studios" or "All") fields.Add(ItemFields.Studios);
        if (request.SearchField is "Year" or "All") fields.Add(ItemFields.ProductionYear);
        // Extend Emby's default fields, including the IDs used by permission checks.
        query.DtoOptions.Fields = query.DtoOptions.Fields.Concat(fields).Distinct().ToArray();
    }

    private Dictionary<long, (string Field, string Value)> MetadataMatches(GetBrowseItems request, BaseItem[] batch)
    {
        var visible = library.FilterItemsToIdsForUser(batch, user, null, CancellationToken.None).ToHashSet();
        var result = new Dictionary<long, (string Field, string Value)>();
        var text = request.Search.Trim();
        if (text.Length == 0)
        {
            foreach (var item in batch.Where(i => visible.Contains(i.InternalId))) result[item.InternalId] = ("", "");
            return result;
        }
        var roles = request.SearchField == "Director" ? new[] { PersonType.Director } :
            request.SearchField == "Actor" ? new[] { PersonType.Actor, PersonType.GuestStar } :
            new[] { PersonType.Actor, PersonType.GuestStar, PersonType.Director };
        var people = (visible.Count > 0 && request.SearchField is "Actor" or "Director" or "All"
            ? library.GetItemPeople(new InternalPeopleQuery { ItemIds = visible.ToArray(), PersonTypes = roles, EnableIds = true, EnableGroupByName = false })
            : new List<PersonInfo>()).ToLookup(person => person.ItemId);
        var compare = CultureInfo.InvariantCulture.CompareInfo;
        bool Selected(string field) => request.SearchField == "All" || request.SearchField == field;
        int Index(string? value) => string.IsNullOrEmpty(value) ? -1 : compare.IndexOf(value, text, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
        foreach (var item in batch)
        {
            if (!visible.Contains(item.InternalId)) continue;
            var candidates = new List<(string Field, string Value)>();
            if (Selected("Title")) { candidates.Add(("Title", item.Name)); candidates.Add(("OriginalTitle", item.OriginalTitle)); }
            if (Selected("Overview")) candidates.Add(("Overview", item.Overview));
            if (Selected("Actor") || Selected("Director")) candidates.AddRange(people[item.InternalId]
                .Where(person => roles.Contains(person.Type))
                .Select(person => (person.Type == PersonType.Director ? "Director" : "Actor", person.Name)));
            if (Selected("Genres")) candidates.AddRange((item.Genres ?? Array.Empty<string>()).Select(value => ("Genres", value)));
            if (Selected("Tags")) candidates.AddRange((item.Tags ?? Array.Empty<string>()).Select(value => ("Tags", value)));
            if (Selected("Studios")) candidates.AddRange((item.Studios ?? Array.Empty<string>()).Select(value => ("Studios", value)));
            if (Selected("Year") && item.ProductionYear.HasValue) candidates.Add(("Year", item.ProductionYear.Value.ToString(CultureInfo.InvariantCulture)));
            foreach (var candidate in candidates)
            {
                var index = Index(candidate.Value);
                if (index < 0) continue;
                var value = candidate.Value;
                if (candidate.Field == "Overview")
                {
                    var start = Math.Max(0, index - 35); var length = Math.Min(value.Length - start, Math.Max(120, text.Length + 35));
                    value = (start > 0 ? "…" : "") + value.Substring(start, length) + (start + length < value.Length ? "…" : "");
                }
                result[item.InternalId] = (candidate.Field, value); break;
            }
        }
        return result;
    }

    private static BrowseItem MapMatch(BaseItem item, (string Field, string Value) match)
    {
        var result = Map(item);
        if (match.Field != "Title") { result.MatchField = match.Field; result.MatchValue = match.Value; }
        return result;
    }

    private BrowseItemsInfo MatchingItems(GetBrowseItems request, InternalItemsQuery query)
    {
        var items = new List<BrowseItem>();
        var offset = request.Start;
        while (true)
        {
            query.StartIndex = offset; query.Limit = BatchSize;
            var batch = library.GetItemList(query, CancellationToken.None);
            var matches = MetadataMatches(request, batch);
            foreach (var item in batch)
            {
                offset++;
                if (!matches.TryGetValue(item.InternalId, out var match)) continue;
                if (items.Count == 50) return new BrowseItemsInfo { Items = items.ToArray(), Next = offset - 1 };
                items.Add(MapMatch(item, match));
            }
            if (batch.Length < BatchSize) return new BrowseItemsInfo { Items = items.ToArray() };
        }
    }
}
