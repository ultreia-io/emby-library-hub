using System;
using System.Collections.Generic;
using System.Linq;

namespace Emby.LibraryHub.Core;

// Calls are serialized by the Emby adapter. Every operation loads a committed state;
// a failed save must never leave an in-memory state ahead of durable storage.
public sealed partial class DigestEngine
{
    private readonly IDigestStore store;
    private readonly DigestFormatter formatter = new DigestFormatter();
    private readonly string serverId;
    private readonly IDigestReportPublisher? reports;
    public DigestEngine(IDigestStore store, string serverId = "", IDigestReportPublisher? reports = null)
    { this.store = store; this.serverId = serverId; this.reports = reports; }

    public void Reconcile(IEnumerable<LibraryInventory> libraries, DateTimeOffset observedAt, TimeZoneInfo? zone = null)
    {
        zone ??= TimeZoneInfo.Utc;
        var today = TimeZoneInfo.ConvertTime(observedAt, zone).Date;
        var state = Load();
        foreach (var library in libraries)
        {
            var old = state.Inventory.Where(i => i.LibraryId == library.Id).ToDictionary(i => i.Key);
            var current = library.Items.GroupBy(i => i.Key).ToDictionary(g => g.Key, g => g.Last());
            if (state.Libraries.Contains(library.Id))
            {
                foreach (var item in current.Values.Where(i => !old.ContainsKey(i.Key))) AddChange(state, item, true, observedAt);
                foreach (var item in old.Values.Where(i => !current.ContainsKey(i.Key))) AddChange(state, item, false, observedAt);
            }
            else state.Libraries.Add(library.Id);
            // Once per library (including upgrades), recover additions from earlier
            // in this calendar day. Older media remain the silent baseline.
            if (!state.AdditionHistoryInitialized.Contains(library.Id))
            {
                var pendingAdditions = new HashSet<string>(state.Changes.Where(c => c.Added).Select(c => c.Item.Key));
                foreach (var item in current.Values)
                    if (item.DateCreated is { } created && created <= observedAt &&
                        TimeZoneInfo.ConvertTime(created, zone).Date == today && pendingAdditions.Add(item.Key))
                        AddChange(state, item, true, created);
                state.AdditionHistoryInitialized.Add(library.Id);
            }
            foreach (var change in state.Changes.Where(c => c.Added && c.Item.LibraryId == library.Id))
                if (current.TryGetValue(change.Item.Key, out var refreshed)) change.Item = refreshed;
            state.Inventory.RemoveAll(i => i.LibraryId == library.Id);
            state.Inventory.AddRange(current.Values);
        }
        store.Save(state);
    }

    public void ItemAdded(MediaEntry item, DateTimeOffset observedAt)
    {
        var state = Load();
        if (!state.Libraries.Contains(item.LibraryId)) return;
        if (!state.Inventory.Any(i => i.Key == item.Key)) AddChange(state, item, true, observedAt);
        state.Inventory.RemoveAll(i => i.Key == item.Key);
        state.Inventory.Add(item);
        store.Save(state);
    }

    public void ItemRemoved(long itemId, string path, IEnumerable<string> availableLibraries, DateTimeOffset observedAt)
    {
        var state = Load();
        var allowed = new HashSet<string>(availableLibraries);
        var removed = state.Inventory.Where(i => allowed.Contains(i.LibraryId) &&
            (i.ItemId == itemId || Below(i.Path, path))).ToList();
        if (removed.Count == 0) return;
        foreach (var item in removed) AddChange(state, item, false, observedAt);
        var keys = new HashSet<string>(removed.Select(i => i.Key));
        state.Inventory.RemoveAll(i => keys.Contains(i.Key));
        store.Save(state);
    }

    public void ArchiveCompletedDays(DateTimeOffset now, DigestOptions options)
    {
        var zone = options.Validate();
        var today = TimeZoneInfo.ConvertTime(now, zone).Date;
        var state = Load();
        var completed = state.Changes.Where(c => TimeZoneInfo.ConvertTime(c.ObservedAt, zone).Date < today).ToList();
        if (completed.Count == 0) return;
        state.History.AddRange(completed);
        state.Changes.RemoveAll(c => completed.Contains(c));
        store.Save(state);
    }

    public DigestMessage Preview(DateTimeOffset now, DigestOptions options)
    {
        var zone = options.Validate();
        var state = Load();
        var day = state.Changes.Count > 0
            ? state.Changes.Min(c => TimeZoneInfo.ConvertTime(c.ObservedAt, zone).Date)
            : TimeZoneInfo.ConvertTime(now, zone).Date;
        var changes = state.Changes.Where(c => TimeZoneInfo.ConvertTime(c.ObservedAt, zone).Date == day).ToList();
        var message = formatter.Format(day, changes, options.Language, options.IncludeVideoLinks ? options.PublicServerUrl : "", serverId, new HashSet<string>(state.Inventory.Select(i => i.Key)));
        if (changes.Count == 0) message.Body = options.Language == "fr" ? "Aucun changement suivi à envoyer." : "No tracked changes to send.";
        return message;
    }

    private DigestState Load()
    {
        var state = store.Load();
        if (state.Version != 1) throw new InvalidOperationException("Unsupported digest state version; state was not changed.");
        return state;
    }

    private static void AddChange(DigestState state, MediaEntry item, bool added, DateTimeOffset at) =>
        state.Changes.Add(new LibraryChange { Item = item, Added = added, ObservedAt = at });

    private static bool Below(string candidate, string parent) => !string.IsNullOrEmpty(parent) &&
        (string.Equals(candidate, parent, StringComparison.Ordinal) ||
         candidate.StartsWith(parent.TrimEnd('/', '\\') + "/", StringComparison.Ordinal) ||
         candidate.StartsWith(parent.TrimEnd('/', '\\') + "\\", StringComparison.Ordinal));
}
