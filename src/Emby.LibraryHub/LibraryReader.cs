using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Emby.LibraryHub.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace Emby.LibraryHub;

public sealed class LibraryReader
{
    private readonly ILibraryManager library;
    private readonly IFileSystem files;
    internal static readonly string[] Kinds = { "Movie", "Episode", "Audio", "MusicVideo", "Video", "Photo", "AudioBook" };

    public LibraryReader(ILibraryManager library, IFileSystem files)
    {
        this.library = library;
        this.files = files;
    }

    public List<VirtualFolderInfo> AvailableLibraries() => library.GetVirtualFolders().Where(f =>
        f.Locations != null && f.Locations.Length > 0 && f.Locations.All(files.DirectoryExists)).ToList();

    public List<LibraryInventory> Read(CancellationToken cancellationToken)
    {
        var result = new List<LibraryInventory>();
        foreach (var folder in AvailableLibraries())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var inventory = new LibraryInventory { Id = folder.ItemId, Name = folder.Name };
            var items = library.GetItemList(new InternalItemsQuery
            {
                Recursive = true,
                ParentIds = new[] { library.GetInternalId(folder.ItemId) },
                IncludeItemTypes = Kinds,
                IsVirtualItem = false,
                EnableTotalRecordCount = false,
                GroupByPresentationUniqueKey = false
            }, cancellationToken);
            inventory.Items.AddRange(items.Where(IsMedia).Select(i => Map(i, folder)));
            // A mount disappearing during the read must not turn into mass removals.
            if (folder.Locations.All(files.DirectoryExists)) result.Add(inventory);
        }
        return result;
    }

    public IEnumerable<MediaEntry> ReadEvent(ItemChangeEventArgs args)
    {
        if (!IsMedia(args.Item)) return Enumerable.Empty<MediaEntry>();
        var parentIds = new HashSet<long>((args.CollectionFolders ?? library.GetCollectionFolders(args.Item))
            .Select(f => f.InternalId));
        return AvailableLibraries().Where(f => parentIds.Contains(library.GetInternalId(f.ItemId)))
            .Select(f => Map(args.Item, f)).ToList();
    }

    private static bool IsMedia(BaseItem item) => item != null && !item.IsVirtualItem && !item.ExtraType.HasValue
        && !string.IsNullOrEmpty(item.Path) && Kinds.Contains(item.GetType().Name);

    private static MediaEntry Map(BaseItem item, VirtualFolderInfo folder)
    {
        var episode = item as Episode;
        return new MediaEntry
        {
            LibraryId = folder.ItemId,
            LibraryName = folder.Name,
            ItemId = item.InternalId,
            Path = item.Path,
            DateCreated = item.DateCreated == default ? null : item.DateCreated,
            Title = item.Name,
            Kind = item.GetType().Name,
            Year = item.ProductionYear,
            SeriesId = episode == null ? "" : episode.FindSeriesId().ToString(CultureInfo.InvariantCulture),
            SeriesName = episode?.SeriesName ?? "",
            Season = episode?.ParentIndexNumber,
            Episode = episode?.IndexNumber,
            EpisodeEnd = episode?.IndexNumberEnd
        };
    }
}
