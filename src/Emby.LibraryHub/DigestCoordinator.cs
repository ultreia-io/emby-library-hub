using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.LibraryHub.Core;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller;
using System.Collections.Concurrent;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace Emby.LibraryHub;

public sealed class DigestCoordinator : IServerEntryPoint
{
    public static DigestCoordinator Instance { get; private set; } = null!;
    private readonly ILibraryManager library;
    private readonly IServerApplicationHost host;
    private readonly LibraryReader reader;
    private readonly ILogger logger;
    internal void LogSubscriptionFailure(Exception error) =>
        logger.ErrorException("Library Hub: confirmation email request failed.", error);

    private readonly DigestEngine engine;
    public ReportArchive Reports { get; }
    public SubscriptionService Subscriptions { get; }
    public string SubscriptionPath => CommunityLinks.SubscriptionWebPath;
    public CommunityAccess Access { get; }
    private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
    private readonly CancellationTokenSource stop = new CancellationTokenSource();
    private long revision;
    private volatile bool initialized;
    private readonly ConcurrentQueue<Action> events = new ConcurrentQueue<Action>();
    private int workerRunning;

    public DigestCoordinator(ILibraryManager library, IFileSystem files, IJsonSerializer json, ILogger logger, IServerApplicationHost host, IUserManager users)
    {
        Instance = this;
        this.library = library;
        this.host = host;
        this.logger = logger;
        Access = new CommunityAccess(users, library);
        reader = new LibraryReader(library, files);
        Reports = new ReportArchive(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Plugin.Instance.StatePath)!, "reports"),
            text => json.DeserializeFromString<ReportManifest>(text), manifest => json.SerializeToString(manifest));
        Subscriptions = new SubscriptionService(new SubscriptionStore(
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Plugin.Instance.StatePath)!, "subscribers.json"),
            text => json.DeserializeFromString<SubscriptionState>(text), state => json.SerializeToString(state)));
        engine = new DigestEngine(new AtomicStateStore(Plugin.Instance.StatePath,
            text => json.DeserializeFromString<DigestState>(text), state => json.SerializeToString(state)), host.SystemId, Reports);
    }

    public void Run()
    {
        library.ItemAdded += OnAdded;
        library.ItemRemoved += OnRemoved;
        library.ItemUpdated += OnUpdated;
        ObserveBackground(() => ReconcileAsync(stop.Token));
    }

    public async Task RunScheduledAsync(CancellationToken cancellationToken)
    {
        using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stop.Token))
        {
            await gate.WaitAsync(linked.Token).ConfigureAwait(false);
            try
            {
                if (!Reconcile(linked.Token)) return;

            }
            finally { gate.Release(); }
            // Bound work per scheduled run and release locks between recipients.
            for (var n = 0; n < 100; n++)
            {
                await gate.WaitAsync(linked.Token).ConfigureAwait(false);
                try
                {
                    var now = DateTimeOffset.UtcNow;
                    if (!await Subscriptions.DeliverNextAsync(now, Plugin.Instance.Configuration.Digest, SubscriptionPath,
                        (userId, first, options) => engine.SubscriptionMessages(first, now, options, Access.FilterWithCancellation(userId, linked.Token)),
                        new SmtpDigestSender(), linked.Token, Access.AccountAllowed).ConfigureAwait(false)) break;
                }
                finally { gate.Release(); }
            }
        }
    }

    public Task<string?> ReadPrivateReportAsync(long userId, string? day) =>
        ReadStandaloneReportAsync(userId, day, CommunityLinks.RelativeWebPath, CommunityLinks.SubscriptionRelativeWebPath);

    public Task<string?> ReadStandaloneReportAsync(long userId, string? day, string archivePath, string subscriptionPath)
    {
        Access.Require(userId);
        return Task.FromResult(Reports.ReadPublished(day, archivePath, subscriptionPath));
    }

    public async Task<DigestMessage> PreviewAsync()
    {
        await gate.WaitAsync(stop.Token).ConfigureAwait(false);
        try
        {
            if (!Reconcile(stop.Token))
                throw new InvalidOperationException("Emby is starting or scanning. Try the preview again when it finishes.");
            return engine.Preview(DateTimeOffset.UtcNow, Plugin.Instance.Configuration.Digest);
        }
        finally { gate.Release(); }
    }

    public async Task<int> ResetReportsAsync()
    {
        await gate.WaitAsync(stop.Token).ConfigureAwait(false);
        try { return Reports.Reset(DateTimeOffset.UtcNow, Plugin.Instance.Configuration.Digest); }
        finally { gate.Release(); }
    }

    public async Task<int> GenerateReportsAsync(string from, string to)
    {
        await gate.WaitAsync(stop.Token).ConfigureAwait(false);
        try
        {
            var options = Plugin.Instance.Configuration.Digest;
            DigestEngine.ValidateHistoricalRange(from, to, DateTimeOffset.UtcNow, options);
            if (!Reconcile(stop.Token))
                throw new InvalidOperationException("Emby is starting or scanning. Try generation again when it finishes.");
            return engine.GenerateReports(from, to, DateTimeOffset.UtcNow, options);
        }
        finally { gate.Release(); }
    }

    private async Task ReconcileAsync(CancellationToken token)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try { Reconcile(token); }
        finally { gate.Release(); }
    }

    private bool Reconcile(CancellationToken token)
    {
        DrainEvents();
        if (!host.IsStartupComplete || library.IsScanRunning) return false;
        var before = Interlocked.Read(ref revision);
        var inventories = reader.Read(token);
        // Never commit a mixed snapshot while Emby is changing the library.
        if (library.IsScanRunning || before != Interlocked.Read(ref revision)) return false;
        engine.Reconcile(inventories, DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById(Plugin.Instance.Configuration.Digest.TimeZoneId));
        initialized = true;
        engine.ArchiveCompletedDays(DateTimeOffset.UtcNow, Plugin.Instance.Configuration.Digest);
        engine.PublishTrackedReports(DateTimeOffset.UtcNow, Plugin.Instance.Configuration.Digest);
        return true;
    }

    private void OnAdded(object? sender, ItemChangeEventArgs args)
    {
        Interlocked.Increment(ref revision);
        if (!initialized || library.IsScanRunning) return;
        var at = DateTimeOffset.UtcNow;
        try
        {
            var items = reader.ReadEvent(args).ToList();
            QueueEvent(() => { foreach (var item in items) engine.ItemAdded(item, at); });
        }
        catch (Exception exception) { logger.ErrorException("Library Hub: item capture failed; next scan will reconcile.", exception); }
    }

    private void OnRemoved(object? sender, ItemChangeEventArgs args)
    {
        Interlocked.Increment(ref revision);
        if (!initialized || library.IsScanRunning) return;
        var at = DateTimeOffset.UtcNow;
        var id = args.Item.InternalId;
        var path = args.Item.Path;
        QueueEvent(() => engine.ItemRemoved(id, path, reader.AvailableLibraries().Select(f => f.ItemId), at));
    }

    private void QueueEvent(Action action)
    {
        events.Enqueue(action);
        StartWorker();
    }

    private void StartWorker()
    {
        if (Interlocked.CompareExchange(ref workerRunning, 1, 0) != 0) return;
        ObserveBackground(async () =>
        {
            var succeeded = false;
            try
            {
                await gate.WaitAsync(stop.Token).ConfigureAwait(false);
                try { DrainEvents(); succeeded = true; }
                finally { gate.Release(); }
            }
            finally
            {
                Interlocked.Exchange(ref workerRunning, 0);
                if (succeeded && !events.IsEmpty && !stop.IsCancellationRequested) StartWorker();
            }
        });
    }

    private void DrainEvents()
    {
        while (events.TryPeek(out var action))
        {
            stop.Token.ThrowIfCancellationRequested();
            action();
            events.TryDequeue(out _);
        }
    }

    private void OnUpdated(object? sender, ItemChangeEventArgs args) => Interlocked.Increment(ref revision);

    private void ObserveBackground(Func<Task> action)
    {
        _ = Task.Run(async () =>
        {
            try { await action().ConfigureAwait(false); }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            catch (Exception exception) { logger.ErrorException("Library Hub: tracking failed; pending reports retained.", exception); }
        });
    }

    public void Dispose()
    {
        library.ItemAdded -= OnAdded;
        library.ItemRemoved -= OnRemoved;
        library.ItemUpdated -= OnUpdated;
        stop.Cancel();
        // In-flight operations release the gate themselves; do not dispose it underneath them.
    }
}
