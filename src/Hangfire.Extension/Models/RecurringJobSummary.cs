namespace Hangfire.Extension.Models;

public sealed record RecurringJobSummary(
    string Id,
    string? CronExpression,
    string? Queue,
    string? JobType,
    string? MethodName,
    DateTimeOffset? NextExecution,
    DateTimeOffset? LastExecution,
    string? LastJobId,
    string? Error,
    bool IsDisabled,
    bool IsSystemError = false);
