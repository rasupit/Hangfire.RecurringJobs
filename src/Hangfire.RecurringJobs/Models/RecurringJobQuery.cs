namespace Hangfire.RecurringJobs.Models;

public sealed record RecurringJobQuery(int Page = 1, int PageSize = 50, string? Search = null)
{
    public int SafePage => Page < 1 ? 1 : Page;

    public int SafePageSize => PageSize switch
    {
        < 1 => 25,
        > 200 => 200,
        _ => PageSize
    };
}
