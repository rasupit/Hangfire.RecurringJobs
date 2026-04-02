using Hangfire.Extension.Core.Models;

namespace Hangfire.Extension.Core.Abstractions;

public interface IRecurringJobStorage
{
    Task<RecurringJobPage> GetJobsAsync(RecurringJobQuery query, CancellationToken cancellationToken = default);

    Task<RecurringJobSummary?> GetJobAsync(string recurringJobId, CancellationToken cancellationToken = default);
}
