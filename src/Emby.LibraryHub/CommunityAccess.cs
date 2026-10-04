using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Emby.LibraryHub.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Emby.LibraryHub;

// Permission results live for one request only; never reuse them across users or requests.
public sealed class CommunityAccess
{
    private readonly IUserManager users;
    private readonly ILibraryManager library;
    public CommunityAccess(IUserManager users, ILibraryManager library) { this.users = users; this.library = library; }
    public bool AccountAllowed(long id)
    {
        var user = id > 0 ? users.GetUserById(id) : null;
        return user != null && !user.Policy.IsDisabled && !user.IsLockedOut && user.IsParentalScheduleAllowed();
    }
    public long Require(long id)
    {
        if (!AccountAllowed(id)) throw new UnauthorizedAccessException("Sign in with an active Emby account.");
        return id;
    }
    public Func<MediaEntry, bool> Filter(long id) => FilterWithCancellation(id, CancellationToken.None);

    public Func<MediaEntry, bool> FilterWithCancellation(long id, CancellationToken cancellationToken)
    {
        Require(id);
        var user = users.GetUserById(id);
        var checkedEntries = new Dictionary<string, bool>(StringComparer.Ordinal);
        var folders = new Dictionary<string, LibraryAccess?>(StringComparer.Ordinal);
        return entry =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = entry.Key + "\n" + entry.ItemId;
            if (!checkedEntries.TryGetValue(key, out var visible))
            {
                if (!folders.TryGetValue(entry.LibraryId, out var access))
                {
                    access = ReadLibrary(user, entry.LibraryId, cancellationToken);
                    folders[entry.LibraryId] = access;
                }
                visible = access != null && CanRead(user, entry, access);
                checkedEntries[key] = visible;
            }
            return visible;
        };
    }

    private sealed class LibraryAccess
    {
        public Dictionary<long, BaseItem> Items { get; } = new Dictionary<long, BaseItem>();
        public HashSet<long> Visible { get; } = new HashSet<long>();
    }

    private LibraryAccess? ReadLibrary(User user, string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var libraryId = library.GetInternalId(id);
        var policy = user.Policy;
        if (!policy.EnableAllFolders && !(policy.EnabledFolders ?? Array.Empty<string>()).Any(value => library.GetInternalId(value) == libraryId)) return null;
        var folder = library.GetItemById(libraryId);
        if (folder == null || !library.FilterItemsToIdsForUser(new[] { folder }, user, null, cancellationToken).Contains(libraryId)) return null;
        var access = new LibraryAccess();
        // One inventory query per library; Emby's permission queries run in bounded batches,
        // instead of two database queries for every item in a multi-year archive.
        var items = library.GetItemList(new InternalItemsQuery
        {
            Recursive = true,
            ParentIds = new[] { libraryId },
            IncludeItemTypes = LibraryReader.Kinds,
            IsVirtualItem = false,
            EnableTotalRecordCount = false,
            GroupByPresentationUniqueKey = false
        }, cancellationToken);
        foreach (var batch in items.Chunk(256))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var item in batch) access.Items[item.InternalId] = item;
            access.Visible.UnionWith(library.FilterItemsToIdsForUser(batch, user, null, cancellationToken));
        }
        return access;
    }

    private bool CanRead(User user, MediaEntry entry, LibraryAccess access)
    {
        if (access.Items.TryGetValue(entry.ItemId, out var item) && string.Equals(item.Path, entry.Path, StringComparison.Ordinal))
            return access.Visible.Contains(entry.ItemId);
        // Items that still exist at this path but moved out of the library cannot be disclosed.
        item = library.GetItemById(entry.ItemId);
        if (item != null && string.Equals(item.Path, entry.Path, StringComparison.Ordinal)) return false;
        // A deleted item cannot be checked against item-level restrictions anymore.
        var policy = user.Policy;
        return !policy.MaxParentalRating.HasValue && (policy.BlockedTags?.Length ?? 0) == 0 &&
            (policy.IncludeTags?.Length ?? 0) == 0 && !policy.IsTagBlockingModeInclusive &&
            (policy.BlockUnratedItems?.Length ?? 0) == 0 && (policy.ExcludedSubFolders?.Length ?? 0) == 0;
    }
}
