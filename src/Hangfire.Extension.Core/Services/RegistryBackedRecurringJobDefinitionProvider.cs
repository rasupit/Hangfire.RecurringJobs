using Hangfire.Extension.Core.Abstractions;
using Hangfire.Extension.Core.Models;

namespace Hangfire.Extension.Core.Services;

public sealed class RegistryBackedRecurringJobDefinitionProvider : IRecurringJobDefinitionProvider
{
    private readonly RecurringJobDefinitionRegistry registry;

    public RegistryBackedRecurringJobDefinitionProvider(
        RecurringJobDefinitionRegistry registry,
        IEnumerable<IRecurringJobDefinitionRegistration> registrations)
    {
        this.registry = registry;

        foreach (var registration in registrations)
        {
            registration.Register(registry);
        }
    }

    public ValueTask<RecurringJobDefinition?> GetDefinitionAsync(string recurringJobId, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(registry.Get(recurringJobId));
}
