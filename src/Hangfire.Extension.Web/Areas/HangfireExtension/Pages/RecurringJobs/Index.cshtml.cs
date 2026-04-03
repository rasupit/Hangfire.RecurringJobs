using Hangfire.Extension.Web.Models;
using Hangfire.Extension.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hangfire.Extension.Web.Areas.HangfireExtension.Pages.RecurringJobs;

public sealed class IndexModel(RecurringJobAdminService recurringJobAdminService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<RecurringJobSummary> Jobs { get; private set; } = [];

    public int TotalCount { get; private set; }

    public int PageSize { get; private set; } = 50;

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
    }

    public async Task<IActionResult> OnPostTriggerAsync(string id, CancellationToken cancellationToken)
        => await ExecuteActionAsync(() => recurringJobAdminService.TriggerAsync(id, cancellationToken));

    public async Task<IActionResult> OnPostDisableAsync(string id, CancellationToken cancellationToken)
        => await ExecuteActionAsync(() => recurringJobAdminService.DisableAsync(id, cancellationToken));

    public async Task<IActionResult> OnPostEnableAsync(string id, CancellationToken cancellationToken)
        => await ExecuteActionAsync(() => recurringJobAdminService.EnableAsync(id, cancellationToken));

    private async Task<IActionResult> ExecuteActionAsync(Func<Task<RecurringJobOperationResult>> action)
    {
        var result = await action();
        StatusMessage = result.Message;
        StatusSucceeded = result.Succeeded;

        return RedirectToPage(new { Search, PageNumber });
    }
}
