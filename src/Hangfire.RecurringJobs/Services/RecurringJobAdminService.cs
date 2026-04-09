using Hangfire;
using Hangfire.RecurringJobs.Hangfire;
using Hangfire.RecurringJobs.Models;

namespace Hangfire.RecurringJobs.Services;

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
        => GetDefinitionBackedJobsAsync(query, cancellationToken);

    public Task<RecurringJobSummary?> GetJobAsync(string recurringJobId, CancellationToken cancellationToken = default)
        => GetDefinitionBackedJobAsync(recurringJobId, cancellationToken);

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

    private async Task<RecurringJobPage> GetDefinitionBackedJobsAsync(
        RecurringJobQuery query,
        CancellationToken cancellationToken)
    {
        var storageJobs = await storage.GetJobsAsync(cancellationToken);
        if (storageJobs.Count == 1 && storageJobs[0].IsStorageUnavailable)
        {
            return CreateStorageUnavailablePage(query, storageJobs[0].Error);
        }

        var storageJobsById = storageJobs
            .ToDictionary(job => job.Id, StringComparer.OrdinalIgnoreCase);

        var filteredJobs = definitions.Values
            .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
            .Select(definition => storageJobsById.TryGetValue(definition.Id, out var storageJob)
                ? storageJob
                : CreateDisabledSummary(definition))
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

    private async Task<RecurringJobSummary?> GetDefinitionBackedJobAsync(
        string recurringJobId,
        CancellationToken cancellationToken)
    {
        if (!definitions.TryGetValue(recurringJobId, out var definition))
        {
            return null;
        }

        var job = await storage.GetJobAsync(recurringJobId, cancellationToken);
        if (job?.IsStorageUnavailable == true)
        {
            return CreateStorageUnavailableSummary(definition, job.Error);
        }

        return job ?? CreateDisabledSummary(definition);
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

    private static RecurringJobSummary CreateDisabledSummary(RecurringJobDefinition definition)
        => new(
            Id: definition.Id,
            CronExpression: definition.CronExpression,
            Queue: definition.Queue,
            JobType: definition.Job.Type.FullName,
            MethodName: definition.Job.Method.Name,
            NextExecution: null,
            LastExecution: null,
            LastJobId: null,
            Error: null,
            IsDisabled: true);

    private RecurringJobPage CreateStorageUnavailablePage(RecurringJobQuery query, string? errorMessage)
    {
        var unavailableJobs = definitions.Values
            .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
            .Select(definition => CreateStorageUnavailableSummary(definition, errorMessage))
            .Where(job => MatchesSearch(job, query.Search))
            .ToArray();

        var items = unavailableJobs
            .Skip((query.SafePage - 1) * query.SafePageSize)
            .Take(query.SafePageSize)
            .ToArray();

        return new RecurringJobPage(
            Items: items,
            Page: query.SafePage,
            PageSize: query.SafePageSize,
            TotalCount: unavailableJobs.Length,
            Search: query.Search,
            IsStorageUnavailable: true,
            StorageErrorMessage: errorMessage ?? RecurringJobStorage.StorageUnavailableMessage);
    }

    private static RecurringJobSummary CreateStorageUnavailableSummary(RecurringJobDefinition definition, string? errorMessage)
        => new(
            Id: definition.Id,
            CronExpression: definition.CronExpression,
            Queue: definition.Queue,
            JobType: definition.Job.Type.FullName,
            MethodName: definition.Job.Method.Name,
            NextExecution: null,
            LastExecution: null,
            LastJobId: null,
            Error: errorMessage ?? RecurringJobStorage.StorageUnavailableMessage,
            IsDisabled: true,
            IsSystemError: true,
            IsStorageUnavailable: true);
}
