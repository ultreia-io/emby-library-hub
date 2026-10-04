using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Querying;

namespace Emby.LibraryHub;

[Route("/LibraryHub/Community/Libraries", "GET")]
public sealed class GetBrowseLibraries : IReturn<BrowseLibrariesInfo> { }
[Route("/LibraryHub/Community/Browse", "GET")]
public sealed class GetBrowseItems : IReturn<BrowseItemsInfo>
{
    public string Mode { get; set; } = "";
    public string Group { get; set; } = "";
    public string ParentId { get; set; } = "";
    public string LibraryId { get; set; } = "";
    public string BoxSetId { get; set; } = "";
    public string Search { get; set; } = "";
    public string SearchField { get; set; } = "Title";
    public bool IncludeEpisodeMetadata { get; set; }
    public bool HideEmptyCollections { get; set; }
    public string Kind { get; set; } = "";
    public string Genre { get; set; } = "";
    public int? Year { get; set; }
    public string Played { get; set; } = "";
    public bool Favorites { get; set; }
    public string Sort { get; set; } = "SortName";
    public bool Descending { get; set; }
    public int Start { get; set; }
}
public sealed class BrowseLibrary
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsCollections { get; set; }
}
public sealed class BrowseLibrariesInfo
{
    public BrowseLibrary[] Libraries { get; set; } = Array.Empty<BrowseLibrary>();
    public string ServerId { get; set; } = "";
}
public sealed class BrowseItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Kind { get; set; } = "";
    public int? Year { get; set; }
    public float? Rating { get; set; }
    public long? Minutes { get; set; }
    public int? Number { get; set; }
    public string SeriesId { get; set; } = "";
    public string SeriesTitle { get; set; } = "";
    public string MatchField { get; set; } = "";
    public string MatchValue { get; set; } = "";
    public string SeasonId { get; set; } = "";
    public int? SeasonNumber { get; set; }
}
public sealed class BrowseItemsInfo
{
    public BrowseGroup[] Groups { get; set; } = Array.Empty<BrowseGroup>();
    public BrowseItem[] Items { get; set; } = Array.Empty<BrowseItem>();
    public int? Next { get; set; }
}

// All queries are scoped to the authenticated Emby user. No archive, scan, or email operation is involved.
internal sealed partial class BrowseCatalog
{
    private readonly ILibraryManager library;
    private readonly User user;
    private readonly CancellationToken cancellationToken;
    public BrowseCatalog(ILibraryManager library, User user) : this(library, user, CancellationToken.None) { }
    public BrowseCatalog(ILibraryManager library, User user, CancellationToken cancellationToken)
    { this.library = library; this.user = user; this.cancellationToken = cancellationToken; }
    private bool Visible(BaseItem item) => library.FilterItemsToIdsForUser(new[] { item }, user, null, cancellationToken).Contains(item.InternalId);
    private static string Id(BaseItem item) => item.InternalId.ToString(CultureInfo.InvariantCulture);
    private bool Offered(long id) => Plugin.Instance.Configuration.Digest.BrowseLibraryIds is not { } selected ||
        selected.Contains(id.ToString(CultureInfo.InvariantCulture));
    private long[] LibraryIds() => library.GetVirtualFolders()
        .Select(f => library.GetInternalId(f.ItemId)).Distinct().ToArray();
    private bool IsCollectionsLibrary(long id) => library.GetVirtualFolders().Any(f =>
        library.GetInternalId(f.ItemId) == id && string.Equals(f.CollectionType, "boxsets", StringComparison.OrdinalIgnoreCase));
    public BrowseLibrary[] Libraries()
    {
        var folders = library.GetVirtualFolders();
        var collectionLibraries = folders.Where(f => string.Equals(f.CollectionType, "boxsets", StringComparison.OrdinalIgnoreCase))
            .Select(f => library.GetInternalId(f.ItemId)).ToHashSet();
        var roots = folders.Select(f => library.GetItemById(library.GetInternalId(f.ItemId))).Where(i => i is CollectionFolder).DistinctBy(i => i.InternalId);
        var result = new List<BrowseLibrary>();
        foreach (var batch in roots.Chunk(256))
        {
            var allowed = library.FilterItemsToIdsForUser(batch, user, null, cancellationToken).ToHashSet();
            result.AddRange(batch.Where(i => Offered(i.InternalId) && allowed.Contains(i.InternalId))
                .Select(i => new BrowseLibrary { Id = Id(i), Name = i.Name, IsCollections = collectionLibraries.Contains(i.InternalId) }));
        }
        return result.OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }
    public BrowseItemsInfo Items(GetBrowseItems request)
    {
        if (request.Mode.Length > 0 && request.Mode != "collections" && request.Mode != "contents") return Tree(request);
        var query = Query(request);
        if (request.Mode == "collections" && request.HideEmptyCollections) return MatchingCollections(request, query);
        if (request.Mode != "collections") return MatchingItems(request, query);
        var rows = library.GetItemList(query, cancellationToken);
        var page = rows.Take(50).ToArray();
        var visible = library.FilterItemsToIdsForUser(page, user, null, cancellationToken).ToHashSet();
        return new BrowseItemsInfo { Next = rows.Length > 50 ? request.Start + 50 : null,
            Items = page.Where(i => visible.Contains(i.InternalId)).Select(Map).ToArray() };
    }
    private BrowseItemsInfo MatchingCollections(GetBrowseItems request, InternalItemsQuery query)
    {
        // Bound each request to one page of collection headings. Search their
        // combined contents once, rather than querying each collection separately.
        var rows = library.GetItemList(query, cancellationToken);
        var page = rows.Take(50).ToArray();
        var visible = library.FilterItemsToIdsForUser(page, user, null, cancellationToken).ToHashSet();
        var boxes = page.Where(box => visible.Contains(box.InternalId)).ToArray();
        var found = new HashSet<long>();
        if (boxes.Length > 0)
        {
            var contents = Query(new GetBrowseItems
            {
                Mode = "contents", LibraryId = request.LibraryId, BoxSetId = Id(boxes[0]),
                Search = request.Search, SearchField = request.SearchField, IncludeEpisodeMetadata = request.IncludeEpisodeMetadata,
                Kind = request.Kind, Genre = request.Genre, Year = request.Year,
                Played = request.Played, Favorites = request.Favorites, Sort = request.Sort
            });
            contents.CollectionIds = boxes.Select(box => box.InternalId).ToArray();
            contents.GroupByPresentationUniqueKey = false;
            contents.DtoOptions.Fields = contents.DtoOptions.Fields.Append(ItemFields.Collections).Distinct().ToArray();
            contents.Limit = BatchSize;
            for (var offset = 0; ; offset += BatchSize)
            {
                contents.StartIndex = offset;
                var batch = library.GetItemList(contents, cancellationToken);
                var matches = MetadataMatches(request, batch);
                foreach (var item in batch.Where(item => matches.ContainsKey(item.InternalId)))
                    foreach (var collection in item.Collections)
                        if (visible.Contains(collection.Id)) found.Add(collection.Id);
                if (batch.Length < BatchSize || found.Count == boxes.Length) break;
            }
        }
        return new BrowseItemsInfo
        {
            Items = boxes.Where(box => found.Contains(box.InternalId)).Select(Map).ToArray(),
            Next = rows.Length > 50 ? request.Start + 50 : null
        };
    }
    private InternalItemsQuery Query(GetBrowseItems request)
    {
        if (!long.TryParse(request.LibraryId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            throw new ArgumentException("Invalid library.");
        var root = library.GetItemById(id);
        if (!Offered(id) || !(root is CollectionFolder) || !LibraryIds().Contains(id) || !Visible(root))
            throw new UnauthorizedAccessException("This library is unavailable.");
        var kinds = new[] { "Movie", "Series", "Episode", "Audio", "MusicVideo", "Video", "AudioBook", "Photo" };
        var sorts = new[] { "SortName", "DateCreated", "PremiereDate", "ProductionYear", "CommunityRating", "Director", "Actor" };
        if ((!string.IsNullOrEmpty(request.Kind) && !kinds.Contains(request.Kind)) || !sorts.Contains(request.Sort) || !SearchFields.Contains(request.SearchField) ||
            (request.Played != "" && request.Played != "true" && request.Played != "false") || request.Start < 0 ||
            request.Start > int.MaxValue - 51 || request.Search.Trim().Length == 1 || request.Search.Length > 200 || request.Genre.Length > 100 ||
            (request.Year.HasValue && (request.Year < 1 || request.Year > 9999))) throw new ArgumentException("Invalid search filters.");
        var collectionLibrary = IsCollectionsLibrary(id);
        long boxId = 0;
        if (request.BoxSetId.Length > 0)
        {
            if (!collectionLibrary || !long.TryParse(request.BoxSetId, NumberStyles.None, CultureInfo.InvariantCulture, out boxId) || boxId <= 0)
                throw new ArgumentException("Invalid collection within this library.");
            var box = library.GetItemById(boxId);
            if (!(box is BoxSet) || !Visible(box)) throw new UnauthorizedAccessException("This collection is unavailable.");
        }
        var listingCollections = collectionLibrary && boxId == 0;
        if (request.Mode == "collections" && !listingCollections || listingCollections && request.Mode != "collections")
            throw new ArgumentException("Expand the Collections library before selecting a collection.");
        if (request.Mode == "contents" && boxId == 0) throw new ArgumentException("Choose a collection first.");
        // Emby's Collections library is a virtual view: box sets have no physical library ancestor.
        // List its headings lazily; search and media filters apply when a collection is expanded.
        var query = new InternalItemsQuery(user)
        {
            Recursive = boxId == 0,
            ParentIds = collectionLibrary ? Array.Empty<long>() : new[] { id },
            CollectionIds = boxId > 0 ? new[] { boxId } : Array.Empty<long>(),
            IncludeItemTypes = listingCollections ? new[] { "BoxSet" } : request.Kind.Length > 0 ? new[] { request.Kind } : kinds.Where(k => k != "Episode" || (request.IncludeEpisodeMetadata && request.Search.Length > 0)).ToArray(),
            IsVirtualItem = false,
            Genres = listingCollections || string.IsNullOrWhiteSpace(request.Genre) ? Array.Empty<string>() : new[] { request.Genre.Trim() },
            Years = !listingCollections && request.Year.HasValue ? new[] { request.Year.Value } : Array.Empty<int>(),
            IsPlayed = !listingCollections && request.Played.Length > 0 ? bool.Parse(request.Played) : null,
            IsFavorite = !listingCollections && request.Favorites ? true : null,
            StartIndex = request.Start, Limit = 51, EnableTotalRecordCount = false,
            GroupByPresentationUniqueKey = true,
            OrderBy = new[] { (listingCollections || IsPeopleSort(request.Sort) ? "SortName" : request.Sort, request.Descending ? SortOrder.Descending : SortOrder.Ascending), ("SortName", SortOrder.Ascending) }
        };
        SearchOptions(query, request);
        return query;
    }
    private static BrowseItem Map(BaseItem i) => new BrowseItem
    {
        Id = Id(i), Title = i.Name, Kind = i.GetType().Name, Year = i.ProductionYear, Rating = i.CommunityRating,
        Minutes = i.RunTimeTicks.HasValue ? i.RunTimeTicks.Value / TimeSpan.TicksPerMinute : null, Number = i.IndexNumber,
        SeriesId = i is Episode ? i.SeriesId.ToString(CultureInfo.InvariantCulture) : "",
        SeriesTitle = i is Episode ep ? ep.SeriesName : "",
        SeasonId = i is Episode ? i.ParentId.ToString(CultureInfo.InvariantCulture) : "",
        SeasonNumber = i.ParentIndexNumber
    };

}

[Authenticated]
public sealed class BrowseApi : IService, IRequiresRequest
{
    private readonly IAuthorizationContext authorization;
    private readonly ILibraryManager library;
    private readonly IUserManager users;
    private readonly IServerApplicationHost host;
    public BrowseApi(IAuthorizationContext authorization, ILibraryManager library, IUserManager users, IServerApplicationHost host)
    { this.authorization = authorization; this.library = library; this.users = users; this.host = host; }
    public IRequest Request { get; set; } = null!;
    private User User()
    {
        Request.Response.AddHeader("Cache-Control", "no-store");
        var id = new CommunityAccess(users, library).Require(authorization.GetAuthorizationInfo(Request).UserId);
        return users.GetUserById(id);
    }
    public BrowseLibrariesInfo Get(GetBrowseLibraries request)
    {
        var user = User();
        return new BrowseLibrariesInfo { Libraries = new BrowseCatalog(library, user, Request.CancellationToken).Libraries(), ServerId = host.SystemId };
    }
    public BrowseItemsInfo Get(GetBrowseItems request)
    {
        var user = User();
        try { return new BrowseCatalog(library, user, Request.CancellationToken).Items(request); }
        catch (UnauthorizedAccessException) { Request.Response.StatusCode = 404; return new BrowseItemsInfo(); }
    }
}

[Route("/LibraryHub/Browse/Libraries", "GET")]
public sealed class GetAdminBrowseLibraries : IReturn<BrowseLibrariesInfo> { }

[Authenticated(Roles = "Admin")]
public sealed class BrowseAdminApi : IService
{
    private readonly ILibraryManager library;
    public BrowseAdminApi(ILibraryManager library) { this.library = library; }
    public BrowseLibrariesInfo Get(GetAdminBrowseLibraries request) => new BrowseLibrariesInfo
    {
        Libraries = library.GetVirtualFolders()
            .Select(f => library.GetItemById(library.GetInternalId(f.ItemId)))
            .OfType<CollectionFolder>().DistinctBy(i => i.InternalId)
            .Select(i => new BrowseLibrary { Id = i.InternalId.ToString(CultureInfo.InvariantCulture), Name = i.Name })
            .OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase).ToArray()
    };
}
