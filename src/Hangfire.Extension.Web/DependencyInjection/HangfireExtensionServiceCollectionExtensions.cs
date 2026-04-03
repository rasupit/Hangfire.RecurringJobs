using Hangfire.Extension.Web.Hangfire;
using Hangfire.Extension.Web.Options;
using Hangfire.Extension.Web.Services;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hangfire.Extension.Web.DependencyInjection;

public static class HangfireExtensionServiceCollectionExtensions
{
    public static IServiceCollection AddHangfireExtension(this IServiceCollection services)
        => services.AddHangfireExtension(static _ => { });

    public static IServiceCollection AddHangfireExtension(this IServiceCollection services, string routePrefix)
        => services.AddHangfireExtension(options => options.RoutePrefix = routePrefix);

    public static IServiceCollection AddHangfireExtension(
        this IServiceCollection services,
        string routePrefix,
        string authorizationPolicy)
        => services.AddHangfireExtension(options =>
        {
            options.RoutePrefix = routePrefix;
            options.AuthorizationPolicy = authorizationPolicy;
        });

    public static IServiceCollection AddHangfireExtension(
        this IServiceCollection services,
        Action<HangfireExtensionOptions> configure)
    {
        services.TryAddSingleton<CronExpressionValidator>();
        services.TryAddSingleton<RecurringJobStorage>();
        services.TryAddSingleton<IRecurringJobManager>(serviceProvider =>
            new RecurringJobManager(serviceProvider.GetRequiredService<JobStorage>()));
        services.TryAddSingleton<RecurringJobAdminService>();
        services.AddRazorPages();
        services.AddOptions<HangfireExtensionOptions>()
            .Configure(configure)
            .PostConfigure(options => options.Validate());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions>,
                HangfireExtensionRazorPagesOptionsSetup>());

        return services;
    }
}
