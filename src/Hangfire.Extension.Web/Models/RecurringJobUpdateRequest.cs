using System.ComponentModel.DataAnnotations;

namespace Hangfire.Extension.Web.Models;

public sealed class RecurringJobUpdateRequest
{
    [Required]
    public string CronExpression { get; set; } = string.Empty;
}
