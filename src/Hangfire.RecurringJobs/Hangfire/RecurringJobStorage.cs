using Hangfire;
using Hangfire.States;
using Hangfire.Storage;

using Hangfire.RecurringJobs.Models;

namespace Hangfire.RecurringJobs.Hangfire;

public sealed class RecurringJobStorage(JobStorage jobStorage)
{
    public const string StorageUnavailableMessage = "Recurring job data is temporarily unavailable.";

    public Task<IReadOnlyList<RecurringJobSummary>> GetJobsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = jobStorage.GetConnection();
            var items = connection.GetRecurringJobs()
                .OrderBy(job => job.Id, StringComparer.OrdinalIgnoreCase)
                .Select(Map)
                .ToArray();

            return Task.FromResult<IReadOnlyList<RecurringJobSummary>>(items);
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<RecurringJobSummary>>([CreateStorageUnavailableSummary()]);
        }
    }

    public Task<RecurringJobSummary?> GetJobAsync(string recurringJobId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = jobStorage.GetConnection();
            var job = connection.GetRecurringJobs()
                .FirstOrDefault(candidate => string.Equals(candidate.Id, recurringJobId, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(job is null ? null : Map(job));
        }
        catch
        {
            return Task.FromResult<RecurringJobSummary?>(CreateStorageUnavailableSummary(recurringJobId));
        }
    }

    private static RecurringJobSummary Map(RecurringJobDto job)
        => new(
            Id: job.Id,
            CronExpression: job.Cron,
            TimeZoneId: job.TimeZoneId,
            Queue: string.IsNullOrWhiteSpace(job.Queue) ? EnqueuedState.DefaultQueue : job.Queue,
            JobType: job.Job?.Type.FullName,
            MethodName: job.Job?.Method.Name,
            NextExecution: job.NextExecution,
            LastExecution: job.LastExecution,
            LastJobId: job.LastJobId,
            Error: job.Error,
            IsDisabled: job.Removed);

    private static RecurringJobSummary CreateStorageUnavailableSummary(string id = "storage-unavailable")
        => new(
            Id: id,
            CronExpression: null,
            TimeZoneId: null,
            Queue: null,
            JobType: null,
            MethodName: null,
            NextExecution: null,
            LastExecution: null,
            LastJobId: null,
            Error: StorageUnavailableMessage,
            IsDisabled: true,
            IsSystemError: true,
            IsStorageUnavailable: true);
}
