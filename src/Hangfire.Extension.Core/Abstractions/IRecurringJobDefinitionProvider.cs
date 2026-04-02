using Hangfire.Extension.Core.Models;

namespace Hangfire.Extension.Core.Abstractions;

public interface IRecurringJobDefinitionProvider
{
    ValueTask<RecurringJobDefinition?> GetDefinitionAsync(string recurringJobId, CancellationToken cancellationToken = default);
}
