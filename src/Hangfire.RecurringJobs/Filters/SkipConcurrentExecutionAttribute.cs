using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;

namespace Hangfire.RecurringJobs;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SkipConcurrentExecutionAttribute(int timeoutInSeconds = 60) : JobFilterAttribute, IServerFilter
{
    private const string LockHandleKey = "Hangfire.RecurringJobs.SkipConcurrentExecution.LockHandle";
    private readonly TimeSpan timeout = timeoutInSeconds > 0
        ? TimeSpan.FromSeconds(timeoutInSeconds)
        : throw new ArgumentOutOfRangeException(nameof(timeoutInSeconds), "Timeout must be greater than zero.");

    public void OnPerforming(PerformingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var resource = $"{context.BackgroundJob.Job.Type.FullName ?? context.BackgroundJob.Job.Type.Name}.{context.BackgroundJob.Job.Method.Name}";

        try
        {
            var lockHandle = context.Connection.AcquireDistributedLock(resource, timeout);
            context.Items[LockHandleKey] = lockHandle;
        }
        catch (DistributedLockTimeoutException)
        {
            context.Canceled = true;
        }
    }

    public void OnPerformed(PerformedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Items.TryGetValue(LockHandleKey, out var lockHandle) && lockHandle is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
