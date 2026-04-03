using Hangfire;
using Hangfire.Extension.Web.Hangfire;
using Hangfire.Extension.Web.Models;

namespace Hangfire.Extension.Web.Services;

public sealed class RecurringJobAdminService
{
    private readonly RecurringJobStorage storage;
    private readonly CronExpressionValidator cronExpressionValidator;
    private readonly IRecurringJobManager recurringJobManager;
    private readonly IReadOnlyDictionary<string, RecurringJobDefinition> definitions;

    public RecurringJobAdminService(
        RecurringJobStorage storage,
        IEnumerable<RecurringJobDefinition> definitions,
        CronExpressionValidator cronExpressionValidator,
        IRecurringJobManager recurringJobManager)
    {
        this.storage = storage;
        this.cronExpressionValidator = cronExpressionValidator;
        this.recurringJobManager = recurringJobManager;
        this.definitions = definitions
            .GroupBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
    }

    public Task<RecurringJobPage> GetJobsAsync(RecurringJobQuery query, CancellationToken cancellationToken = default)
        => storage.GetJobsAsync(query, cancellationToken);

    public Task<RecurringJobSummary?> GetJobAsync(string recurringJobId, CancellationToken cancellationToken = default)
        => storage.GetJobAsync(recurringJobId, cancellationToken);

    public Task<RecurringJobOperationResult> TriggerAsync(string recurringJobId, CancellationToken cancellationToken = default)
        => ExecuteOperationAsync(recurringJobId, "triggered", () => recurringJobManager.Trigger(recurringJobId));

    public Task<RecurringJobOperationResult> DisableAsync(string recurringJobId, CancellationToken cancellationToken = default)
        => ExecuteOperationAsync(recurringJobId, "disabled", () => recurringJobManager.RemoveIfExists(recurringJobId));

    public Task<RecurringJobOperationResult> EnableAsync(string recurringJobId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!definitions.TryGetValue(recurringJobId, out var definition))
            {
                return Task.FromResult(RecurringJobOperationResult.Failure(
                    $"Recurring job '{recurringJobId}' cannot be enabled because no code-based definition is registered."));
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

            return Task.FromResult(RecurringJobOperationResult.Success($"Recurring job '{recurringJobId}' was enabled."));
        }
        catch (Exception)
        {
            return Task.FromResult(RecurringJobOperationResult.Failure(
                $"Failed to enable recurring job '{recurringJobId}'."));
        }
    }

    public Task<RecurringJobOperationResult> UpdateCronAsync(
        string recurringJobId,
        string cronExpression,
        CancellationToken cancellationToken = default)
    {
        if (!cronExpressionValidator.IsValid(cronExpression, out var validationError))
        {
            return Task.FromResult(RecurringJobOperationResult.Failure(validationError ?? "Invalid cron expression."));
        }

        try
        {
            if (!definitions.TryGetValue(recurringJobId, out var definition))
            {
                return Task.FromResult(RecurringJobOperationResult.Failure(
                    $"Recurring job '{recurringJobId}' cannot be updated because no code-based definition is registered."));
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

            return Task.FromResult(RecurringJobOperationResult.Success($"Cron for recurring job '{recurringJobId}' was updated."));
        }
        catch (Exception)
        {
            return Task.FromResult(RecurringJobOperationResult.Failure(
                $"Failed to update recurring job '{recurringJobId}'."));
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
        catch (Exception)
        {
            return Task.FromResult(
                RecurringJobOperationResult.Failure(
                    $"Failed to process recurring job '{recurringJobId}'."));
        }
    }
}
