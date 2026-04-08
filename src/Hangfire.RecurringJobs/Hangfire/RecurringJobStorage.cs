using Hangfire;
using Hangfire.States;
using Hangfire.Storage;

using Hangfire.RecurringJobs.Models;

namespace Hangfire.RecurringJobs.Hangfire;

public sealed class RecurringJobStorage(JobStorage jobStorage)
{
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
            return Task.FromResult<IReadOnlyList<RecurringJobSummary>>([CreateStorageErrorSummary()]);
        }
    }

    public async Task<RecurringJobPage> GetJobsAsync(RecurringJobQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var jobs = await GetJobsAsync(cancellationToken);
            var filteredJobs = jobs
                .Where(job => MatchesSearch(job, query.Search))
                .ToArray();

            var items = filteredJobs
                .Skip((query.SafePage - 1) * query.SafePageSize)
                .Take(query.SafePageSize)
                .ToArray();

            return new RecurringJobPage(
                Items: items,
                Page: query.SafePage,
                PageSize: query.SafePageSize,
                TotalCount: filteredJobs.Length,
                Search: query.Search);
        }
        catch
        {
            var failedItems = new[] { CreateStorageErrorSummary() };

            return new RecurringJobPage(
                Items: failedItems,
                Page: query.SafePage,
                PageSize: query.SafePageSize,
                TotalCount: failedItems.Length,
                Search: query.Search);
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
            return Task.FromResult<RecurringJobSummary?>(null);
        }
    }

    private static bool MatchesSearch(RecurringJobDto job, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return Contains(job.Id, search)
               || Contains(job.Job?.Type.Name, search)
               || Contains(job.Job?.Method.Name, search)
               || Contains(job.Queue, search);
    }

    private static bool MatchesSearch(RecurringJobSummary job, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return Contains(job.Id, search)
               || Contains(job.JobType, search)
               || Contains(job.MethodName, search)
               || Contains(job.Queue, search);
    }

    private static bool Contains(string? value, string search)
        => value?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false;

    private static RecurringJobSummary Map(RecurringJobDto job)
        => new(
            Id: job.Id,
            CronExpression: job.Cron,
            Queue: string.IsNullOrWhiteSpace(job.Queue) ? EnqueuedState.DefaultQueue : job.Queue,
            JobType: job.Job?.Type.FullName,
            MethodName: job.Job?.Method.Name,
            NextExecution: job.NextExecution,
            LastExecution: job.LastExecution,
            LastJobId: job.LastJobId,
            Error: job.Error,
            IsDisabled: job.Removed);

    private static RecurringJobSummary CreateStorageErrorSummary()
        => new(
            Id: "storage-error",
            CronExpression: null,
            Queue: null,
            JobType: null,
            MethodName: null,
            NextExecution: null,
            LastExecution: null,
            LastJobId: null,
            Error: "Recurring job data is temporarily unavailable.",
            IsDisabled: true,
            IsSystemError: true);
}
