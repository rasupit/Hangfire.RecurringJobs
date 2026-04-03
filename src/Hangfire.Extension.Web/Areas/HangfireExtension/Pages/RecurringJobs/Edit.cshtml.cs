using System.ComponentModel.DataAnnotations;
using Hangfire.Extension.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hangfire.Extension.Web.Areas.HangfireExtension.Pages.RecurringJobs;

public sealed class EditModel(RecurringJobAdminService recurringJobAdminService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Id { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string CronExpression { get; set; } = string.Empty;

    public string? JobType { get; private set; }

    public string? MethodName { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var job = await recurringJobAdminService.GetJobAsync(Id, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        CronExpression = job.CronExpression ?? string.Empty;
        JobType = job.JobType;
        MethodName = job.MethodName;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await recurringJobAdminService.UpdateCronAsync(Id, CronExpression, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(CronExpression), result.Message);
            return Page();
        }

        return RedirectToPage("./Index");
    }
}
