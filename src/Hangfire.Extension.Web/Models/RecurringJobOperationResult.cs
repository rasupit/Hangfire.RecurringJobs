namespace Hangfire.Extension.Web.Models;

public sealed record RecurringJobOperationResult(bool Succeeded, string Message)
{
    public static RecurringJobOperationResult Success(string message) => new(true, message);

    public static RecurringJobOperationResult Failure(string message) => new(false, message);
}
