using Hangfire.RecurringJobs.Hangfire;
using Hangfire.RecurringJobs.Services;
using Hangfire;
using Hangfire.RecurringJobs.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hangfire.RecurringJobs;

public static class RecurringJobsServiceCollectionExtensions
{
    public static IServiceCollection AddHangfireRecurringJobs(this IServiceCollection services)
        => services.AddHangfireRecurringJobs(static _ => { });

    public static IServiceCollection AddHangfireRecurringJobs(this IServiceCollection services, string routePrefix)
        => services.AddHangfireRecurringJobs(options => options.RoutePrefix = routePrefix);

    public static IServiceCollection AddHangfireRecurringJobs(
        this IServiceCollection services,
        string routePrefix,
        string authorizationPolicy)
        => services.AddHangfireRecurringJobs(options =>
        {
            options.RoutePrefix = routePrefix;
            options.AuthorizationPolicy = authorizationPolicy;
        });

    public static IServiceCollection AddHangfireRecurringJobs(
        this IServiceCollection services,
        Action<RecurringJobsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddSingleton<CronExpressionValidator>();
        services.TryAddSingleton<CronExpressionPreviewService>();
        services.TryAddSingleton<RecurringJobStorage>();
        services.TryAddSingleton<IRecurringJobManager>(serviceProvider =>
            new RecurringJobManager(serviceProvider.GetRequiredService<JobStorage>()));
        services.TryAddSingleton<RecurringJobAdminService>();
        services.AddRazorPages();
        services.AddOptions<RecurringJobsOptions>()
            .Configure(configure)
            .PostConfigure(options => options.Validate());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions>,
                RecurringJobsRazorPagesOptionsSetup>());

        return services;
    }

}
