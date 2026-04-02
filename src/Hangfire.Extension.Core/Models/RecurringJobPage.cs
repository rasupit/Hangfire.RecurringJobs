namespace Hangfire.Extension.Core.Models;

public sealed record RecurringJobPage(
    IReadOnlyList<RecurringJobSummary> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
