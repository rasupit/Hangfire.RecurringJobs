namespace Hangfire.Extension.Core.Abstractions;

public interface ICronExpressionValidator
{
    bool IsValid(string cronExpression, out string? validationError);
}
