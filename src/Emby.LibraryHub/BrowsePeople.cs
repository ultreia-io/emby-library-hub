using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace Emby.LibraryHub;

internal sealed partial class BrowseCatalog
{
    private static bool IsPeopleSort(string sort) => sort is "Director" or "Actor";

    private BrowseItemsInfo PeopleTree(GetBrowseItems request, InternalItemsQuery query)
    {
        var roles = request.Sort == "Director" ? new[] { PersonType.Director } : new[] { PersonType.Actor, PersonType.GuestStar };
        var parts = request.Group.Split('/');
        if (parts.Length == 2)
        {
            if (!long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var personId)) throw new ArgumentException("Invalid person.");
            query.PersonIds = new[] { personId };
            query.PersonTypes = roles;
        }
        var groups = new Dictionary<string, BrowseGroup>();
        var items = new List<BrowseItem>();
        var offset = request.Mode == "items" ? request.Start : 0;
        while (true)
        {
            query.StartIndex = offset; query.Limit = BatchSize;
            var batch = library.GetItemList(query, CancellationToken.None);
            var metadata = MetadataMatches(request, batch);
            // Read credits only for accessible items, once per batch, never once per title.
            var ids = batch.Where(i => metadata.ContainsKey(i.InternalId)).Select(i => i.InternalId).ToArray();
            var people = (ids.Length == 0 ? new List<PersonInfo>() : library.GetItemPeople(new InternalPeopleQuery
            { ItemIds = ids, PersonTypes = roles, EnableIds = true, EnableGroupByName = false }))
                .Where(p => p.Id > 0 && roles.Contains(p.Type) && !string.IsNullOrWhiteSpace(p.Name)).ToLookup(p => p.ItemId);
            foreach (var item in batch)
            {
                offset++;
                if (!metadata.TryGetValue(item.InternalId, out var match)) continue;
                var credits = people[item.InternalId].DistinctBy(p => p.Id).ToArray();
                if (request.Mode == "items")
                {
                    var matches = request.Group == "unknown" ? credits.Length == 0 :
                        credits.Any(p => PeopleLetter(p.Name) + "/" + p.Id.ToString(CultureInfo.InvariantCulture) == request.Group);
                    if (!matches) continue;
                    if (items.Count == 50) return new BrowseItemsInfo { Items = items.ToArray(), Next = offset - 1 };
                    items.Add(MapMatch(item, match));
                }
                else if (request.Group.Length == 0)
                {
                    if (credits.Length == 0) groups.TryAdd("unknown", new BrowseGroup { Key = "unknown", Label = "unknown", Kind = "unknown" });
                    foreach (var person in credits)
                    {
                        var letter = PeopleLetter(person.Name);
                        groups.TryAdd(letter, new BrowseGroup { Key = letter, Label = letter, Kind = letter == "other" ? "other" : "letter", HasGroups = true });
                    }
                }
                else
                {
                    foreach (var person in credits.Where(p => PeopleLetter(p.Name) == request.Group))
                    {
                        var key = request.Group + "/" + person.Id.ToString(CultureInfo.InvariantCulture);
                        groups.TryAdd(key, new BrowseGroup { Key = key, Label = person.Name, Kind = "person" });
                    }
                }
            }
            if (batch.Length < BatchSize) break;
        }
        var comparer = StringComparer.CurrentCultureIgnoreCase;
        var ordered = groups.Values.OrderBy(g => g.Key == "unknown" ? 2 : g.Key == "other" ? 1 : 0)
            .ThenBy(g => g.Label, request.Descending ? Comparer<string>.Create((a, b) => comparer.Compare(b, a)) : comparer)
            .ThenBy(g => g.Key, StringComparer.Ordinal).ToArray();
        return new BrowseItemsInfo { Groups = ordered.Skip(request.Start).Take(50).ToArray(), Items = items.ToArray(),
            Next = ordered.Length > request.Start + 50 ? request.Start + 50 : null };
    }

    private static string PeopleLetter(string name)
    {
        var first = name.Trim().Normalize(NormalizationForm.FormD).FirstOrDefault(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        var letter = char.ToUpperInvariant(first);
        return letter is >= 'A' and <= 'Z' ? letter.ToString() : letter is >= '0' and <= '9' ? "0-9" : "other";
    }
}
