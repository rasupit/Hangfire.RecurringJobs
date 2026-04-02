using Cronos;
using Hangfire.Extension.Core.Abstractions;

namespace Hangfire.Extension.Core.Services;

public sealed class CronExpressionValidator : ICronExpressionValidator
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
