using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;

namespace Emby.LibraryHub;

public sealed class DigestTask : IScheduledTask
{
    public string Name => "Library Hub";
    public string Key => "LibraryHub";
    public string Category => "Library Hub";
    public string Description => "Track library changes and send completed daily reports after the configured delivery time.";
    public Task Execute(CancellationToken cancellationToken, IProgress<double> progress) => Run(cancellationToken, progress);
    private static async Task Run(CancellationToken token, IProgress<double> progress)
    {
        progress.Report(0);
        await DigestCoordinator.Instance.RunScheduledAsync(token).ConfigureAwait(false);
        progress.Report(100);
    }
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromMinutes(5).Ticks };
    }
}
