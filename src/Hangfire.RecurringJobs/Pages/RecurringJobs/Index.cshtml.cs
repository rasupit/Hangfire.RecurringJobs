using Hangfire.RecurringJobs.Models;
using Hangfire.RecurringJobs.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Hangfire.RecurringJobs.Pages.RecurringJobs;

public sealed class IndexModel(RecurringJobAdminService recurringJobAdminService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<RecurringJobSummary> Jobs { get; private set; } = [];

    public int TotalCount { get; private set; }

    public int PageSize { get; private set; } = 50;

    public bool IsStorageUnavailable { get; private set; }

    public string? StorageErrorMessage { get; private set; }

    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool CanGoToPreviousPage => PageNumber > 1;

    public bool CanGoToNextPage => PageNumber < TotalPages;

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool? StatusSucceeded { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var page = await recurringJobAdminService.GetJobsAsync(
            new RecurringJobQuery(PageNumber, 50, Search),
            cancellationToken);

        Jobs = page.Items;
        TotalCount = page.TotalCount;
        PageSize = page.PageSize;
        PageNumber = page.Page;
        IsStorageUnavailable = page.IsStorageUnavailable;
        StorageErrorMessage = page.StorageErrorMessage;
    }

    public async Task<IActionResult> OnPostTriggerAsync(string id, CancellationToken cancellationToken)
        => await ExecuteActionAsync(() => recurringJobAdminService.TriggerAsync(id, cancellationToken));

    public async Task<IActionResult> OnPostDisableAsync(string id, CancellationToken cancellationToken)
        => await ExecuteActionAsync(() => recurringJobAdminService.DisableAsync(id, cancellationToken));

    public async Task<IActionResult> OnPostEnableAsync(string id, CancellationToken cancellationToken)
        => await ExecuteActionAsync(() => recurringJobAdminService.EnableAsync(id, cancellationToken));

    public async Task<IActionResult> OnPostSetEnabledAsync(string id, bool enabled, CancellationToken cancellationToken)
        => await ExecuteActionAsync(() => enabled
            ? recurringJobAdminService.EnableAsync(id, cancellationToken)
            : recurringJobAdminService.DisableAsync(id, cancellationToken));

    public async Task<IActionResult> OnPostUpdateCronAsync(string id, string cronExpression, CancellationToken cancellationToken)
        => await ExecuteActionAsync(() => recurringJobAdminService.UpdateCronAsync(id, cronExpression, cancellationToken));

    private async Task<IActionResult> ExecuteActionAsync(Func<Task<RecurringJobOperationResult>> action)
    {
        var result = await action();
        StatusMessage = result.Message;
        StatusSucceeded = result.Succeeded;

        return RedirectToPage(routeValues: BuildCanonicalRouteValues());
    }

    private RouteValueDictionary? BuildCanonicalRouteValues()
    {
        RouteValueDictionary? routeValues = null;

        if (!string.IsNullOrWhiteSpace(Search))
        {
            routeValues = [];
            routeValues["search"] = Search;
        }

        if (PageNumber > 1)
        {
            routeValues ??= [];
            routeValues["pageNumber"] = PageNumber;
        }

        return routeValues;
    }
}
