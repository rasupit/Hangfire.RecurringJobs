using Hangfire;
using Hangfire.Extension.Core.Abstractions;
using Hangfire.Extension.Core.Models;

namespace Hangfire.Extension.Core.Services;

public sealed class RecurringJobAdminService(
    IRecurringJobStorage storage,
    IRecurringJobDefinitionProvider definitionProvider,
    ICronExpressionValidator cronExpressionValidator,
    IRecurringJobManager recurringJobManager) : IRecurringJobAdminService
{
    public Task<RecurringJobPage> GetJobsAsync(RecurringJobQuery query, CancellationToken cancellationToken = default)
        => storage.GetJobsAsync(query, cancellationToken);

    public Task<RecurringJobSummary?> GetJobAsync(string recurringJobId, CancellationToken cancellationToken = default)
        => storage.GetJobAsync(recurringJobId, cancellationToken);

    public Task<RecurringJobOperationResult> TriggerAsync(string recurringJobId, CancellationToken cancellationToken = default)
        => ExecuteOperationAsync(recurringJobId, "triggered", () => recurringJobManager.Trigger(recurringJobId));

    public Task<RecurringJobOperationResult> DisableAsync(string recurringJobId, CancellationToken cancellationToken = default)
        => ExecuteOperationAsync(recurringJobId, "disabled", () => recurringJobManager.RemoveIfExists(recurringJobId));

    public async Task<RecurringJobOperationResult> EnableAsync(string recurringJobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var definition = await definitionProvider.GetDefinitionAsync(recurringJobId, cancellationToken);
            if (definition is null)
            {
                return RecurringJobOperationResult.Failure(
                    $"Recurring job '{recurringJobId}' cannot be enabled because no code-based definition is registered.");
            }

            // Hangfire's job-based recurring API still exposes queue via the obsolete QueueName property.
#pragma warning disable CS0618
            recurringJobManager.AddOrUpdate(
                definition.Id,
                definition.Job,
                definition.CronExpression,
                new RecurringJobOptions
                {
                    TimeZone = definition.TimeZone,
                    QueueName = definition.Queue
                });
#pragma warning restore CS0618

            return RecurringJobOperationResult.Success($"Recurring job '{recurringJobId}' was enabled.");
        }
        catch (Exception exception)
        {
            return RecurringJobOperationResult.Failure(
                $"Failed to enable recurring job '{recurringJobId}': {exception.Message}");
        }
    }

    public async Task<RecurringJobOperationResult> UpdateCronAsync(
        string recurringJobId,
        string cronExpression,
        CancellationToken cancellationToken = default)
    {
        if (!cronExpressionValidator.IsValid(cronExpression, out var validationError))
        {
            return RecurringJobOperationResult.Failure(validationError ?? "Invalid cron expression.");
        }

        try
        {
            var definition = await definitionProvider.GetDefinitionAsync(recurringJobId, cancellationToken);
            if (definition is null)
            {
                return RecurringJobOperationResult.Failure(
                    $"Recurring job '{recurringJobId}' cannot be updated because no code-based definition is registered.");
            }

            // Hangfire's job-based recurring API still exposes queue via the obsolete QueueName property.
#pragma warning disable CS0618
            recurringJobManager.AddOrUpdate(
                definition.Id,
                definition.Job,
                cronExpression,
                new RecurringJobOptions
                {
                    TimeZone = definition.TimeZone,
                    QueueName = definition.Queue
                });
#pragma warning restore CS0618

            return RecurringJobOperationResult.Success($"Cron for recurring job '{recurringJobId}' was updated.");
        }
        catch (Exception exception)
        {
            return RecurringJobOperationResult.Failure(
                $"Failed to update recurring job '{recurringJobId}': {exception.Message}");
        }
    }

    private static Task<RecurringJobOperationResult> ExecuteOperationAsync(
        string recurringJobId,
        string pastTenseAction,
        Action action)
    {
        try
        {
            action();
            return Task.FromResult(
                RecurringJobOperationResult.Success($"Recurring job '{recurringJobId}' was {pastTenseAction}."));
        }
        catch (Exception exception)
        {
            return Task.FromResult(
                RecurringJobOperationResult.Failure(
                    $"Failed to process recurring job '{recurringJobId}': {exception.Message}"));
        }
    }
}
