namespace Hangfire.RecurringJobs.Models;

public sealed record RecurringJobCronPreview(
    bool IsValid,
    string Summary,
    string? Description,
    IReadOnlyList<string> UpcomingOccurrences);
