using Hangfire.Extension.Core.Services;

namespace Hangfire.Extension.Core.Abstractions;

public interface IRecurringJobDefinitionRegistration
{
    void Register(RecurringJobDefinitionRegistry registry);
}
