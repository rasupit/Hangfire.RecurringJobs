using Hangfire.Extension.Core.Abstractions;
using Hangfire.Extension.Core.Models;

namespace Hangfire.Extension.Core.Services;

public sealed class NullRecurringJobDefinitionProvider : IRecurringJobDefinitionProvider
{
    public ValueTask<RecurringJobDefinition?> GetDefinitionAsync(string recurringJobId, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<RecurringJobDefinition?>(null);
}
