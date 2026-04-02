using Hangfire;
using Hangfire.Extension.Core.Abstractions;
using Hangfire.Extension.Core.Models;
using Hangfire.Extension.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hangfire.Extension.Infrastructure.Hangfire.DependencyInjection;

public static class HangfireExtensionServiceCollectionExtensions
{
    public static IServiceCollection AddHangfireExtensionAdmin(this IServiceCollection services)
    {
        services.TryAddSingleton<ICronExpressionValidator, CronExpressionValidator>();
        services.TryAddSingleton<RecurringJobDefinitionRegistry>();
        services.TryAddSingleton<IRecurringJobDefinitionProvider, RegistryBackedRecurringJobDefinitionProvider>();
        services.TryAddSingleton<IRecurringJobStorage, RecurringJobStorage>();
        services.TryAddSingleton<IRecurringJobManager>(serviceProvider =>
            new RecurringJobManager(serviceProvider.GetRequiredService<JobStorage>()));
        services.TryAddSingleton<IRecurringJobAdminService, RecurringJobAdminService>();

        return services;
    }

    public static IServiceCollection AddRecurringJobDefinition(
        this IServiceCollection services,
        RecurringJobDefinition definition)
    {
        services.AddHangfireExtensionAdmin();
        services.AddSingleton<IRecurringJobDefinitionRegistration>(new RecurringJobDefinitionRegistration(definition));

        return services;
    }
}
