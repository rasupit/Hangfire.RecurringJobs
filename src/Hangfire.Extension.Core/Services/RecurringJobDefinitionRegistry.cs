using Hangfire.Extension.Core.Models;

namespace Hangfire.Extension.Core.Services;

public sealed class RecurringJobDefinitionRegistry
{
    private readonly Dictionary<string, RecurringJobDefinition> definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock syncLock = new();

    public void Add(RecurringJobDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        lock (syncLock)
        {
            definitions[definition.Id] = definition;
        }
    }

    public RecurringJobDefinition? Get(string recurringJobId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recurringJobId);

        lock (syncLock)
        {
            definitions.TryGetValue(recurringJobId, out var definition);
            return definition;
        }
    }
}
