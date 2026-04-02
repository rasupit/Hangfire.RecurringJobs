using Hangfire.Extension.AspNetCore.Options;
using Hangfire.Extension.Infrastructure.Hangfire.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hangfire.Extension.AspNetCore.DependencyInjection;

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
        services.AddHangfireExtensionAdmin();
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
