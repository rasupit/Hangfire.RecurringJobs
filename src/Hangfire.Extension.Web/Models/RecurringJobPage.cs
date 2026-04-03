namespace Hangfire.Extension.Web.Models;

public sealed record RecurringJobPage(
    IReadOnlyList<RecurringJobSummary> Items,
    int Page,
    int PageSize,
    int TotalCount,
    string? Search = null)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
