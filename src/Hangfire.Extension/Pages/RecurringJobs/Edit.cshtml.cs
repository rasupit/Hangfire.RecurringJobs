using System.ComponentModel.DataAnnotations;
using Hangfire.Extension.Models;
using Hangfire.Extension.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hangfire.Extension.Pages.RecurringJobs;

public sealed class EditModel(
    RecurringJobAdminService recurringJobAdminService,
    CronExpressionPreviewService cronExpressionPreviewService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Id { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string CronExpression { get; set; } = string.Empty;

    public string? JobType { get; private set; }

    public string? MethodName { get; private set; }

    public bool IsDisabled { get; private set; }

    public RecurringJobCronPreview CronPreview { get; private set; }
        = new(false, "Cron expression is required.", null, []);

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool? StatusSucceeded { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LoadJobDetailsAsync(cancellationToken))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadJobDetailsAsync(cancellationToken);
            return Page();
        }

        var result = await recurringJobAdminService.UpdateCronAsync(Id, CronExpression, cancellationToken);
        if (!result.Succeeded)
        {
            await LoadJobDetailsAsync(cancellationToken);
            ModelState.AddModelError(nameof(CronExpression), result.Message);
            return Page();
        }

        StatusMessage = result.Message;
        StatusSucceeded = true;
        return RedirectToPage("./Index");
    }

    private async Task<bool> LoadJobDetailsAsync(CancellationToken cancellationToken)
    {
        var job = await recurringJobAdminService.GetJobAsync(Id, cancellationToken);
        if (job is null)
        {
            return false;
        }

        CronExpression = job.CronExpression ?? string.Empty;
        JobType = job.JobType;
        MethodName = job.MethodName;
        IsDisabled = job.IsDisabled;
        CronPreview = cronExpressionPreviewService.CreatePreview(CronExpression);

        return true;
    }
}
