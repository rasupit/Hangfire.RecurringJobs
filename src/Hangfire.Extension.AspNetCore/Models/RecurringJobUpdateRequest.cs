using System.ComponentModel.DataAnnotations;

namespace Hangfire.Extension.AspNetCore.Models;

public sealed class RecurringJobUpdateRequest
{
    [Required]
    public string CronExpression { get; set; } = string.Empty;
}
