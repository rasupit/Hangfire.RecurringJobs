using Cronos;

namespace Hangfire.RecurringJobs.Services;

public sealed class CronExpressionValidator
{
    public bool IsValid(string cronExpression, out string? validationError)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            validationError = "Cron expression is required.";
            return false;
        }

        try
        {
            _ = CronExpression.Parse(cronExpression, CronFormat.Standard);
            validationError = null;
            return true;
        }
        catch (CronFormatException exception)
        {
            validationError = exception.Message;
            return false;
        }
    }
}
