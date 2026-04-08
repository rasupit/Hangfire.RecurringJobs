using System.ComponentModel.DataAnnotations;

namespace Hangfire.RecurringJobs.Models;

public sealed class RecurringJobUpdateRequest
{
    [Required]
    public string CronExpression { get; set; } = string.Empty;
}
