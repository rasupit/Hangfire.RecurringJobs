using Hangfire;
using Hangfire.Extension.Core.Abstractions;
using Hangfire.Extension.Core.Models;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Extension.Infrastructure.Hangfire;

public sealed class RecurringJobStorage(JobStorage jobStorage) : IRecurringJobStorage
{
    public Task<RecurringJobPage> GetJobsAsync(RecurringJobQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = jobStorage.GetConnection();
            var jobs = connection.GetRecurringJobs();
            var filteredJobs = jobs
                .Where(job => MatchesSearch(job, query.Search))
                .OrderBy(job => job.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var items = filteredJobs
                .Skip((query.SafePage - 1) * query.SafePageSize)
                .Take(query.SafePageSize)
                .Select(Map)
                .ToArray();

            return Task.FromResult(new RecurringJobPage(
                Items: items,
                Page: query.SafePage,
                PageSize: query.SafePageSize,
                TotalCount: filteredJobs.Length));
        }
        catch (Exception exception)
        {
            var failedItems = new[]
            {
                new RecurringJobSummary(
                    Id: "storage-error",
                    CronExpression: null,
                    Queue: null,
                    JobType: null,
                    MethodName: null,
                    NextExecution: null,
                    LastExecution: null,
                    LastJobId: null,
                    Error: exception.Message,
                    IsDisabled: true)
            };

            return Task.FromResult(new RecurringJobPage(
                Items: failedItems,
                Page: query.SafePage,
                PageSize: query.SafePageSize,
                TotalCount: failedItems.Length));
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
}
