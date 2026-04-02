using Hangfire.Extension.Core.Models;

namespace Hangfire.Extension.Core.Abstractions;

public interface IRecurringJobAdminService
{
    Task<RecurringJobPage> GetJobsAsync(RecurringJobQuery query, CancellationToken cancellationToken = default);

    Task<RecurringJobSummary?> GetJobAsync(string recurringJobId, CancellationToken cancellationToken = default);

    Task<RecurringJobOperationResult> TriggerAsync(string recurringJobId, CancellationToken cancellationToken = default);

    Task<RecurringJobOperationResult> DisableAsync(string recurringJobId, CancellationToken cancellationToken = default);

    Task<RecurringJobOperationResult> EnableAsync(string recurringJobId, CancellationToken cancellationToken = default);

    Task<RecurringJobOperationResult> UpdateCronAsync(string recurringJobId, string cronExpression, CancellationToken cancellationToken = default);
}
