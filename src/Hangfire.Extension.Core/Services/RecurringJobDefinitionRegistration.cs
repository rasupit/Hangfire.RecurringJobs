using Hangfire.Extension.Core.Abstractions;
using Hangfire.Extension.Core.Models;

namespace Hangfire.Extension.Core.Services;

public sealed class RecurringJobDefinitionRegistration(RecurringJobDefinition definition) : IRecurringJobDefinitionRegistration
{
    public void Register(RecurringJobDefinitionRegistry registry)
        => registry.Add(definition);
}
