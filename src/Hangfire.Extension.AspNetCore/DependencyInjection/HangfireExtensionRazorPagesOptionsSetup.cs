using Hangfire.Extension.AspNetCore.Options;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Hangfire.Extension.AspNetCore.DependencyInjection;

internal sealed class HangfireExtensionRazorPagesOptionsSetup(IOptions<HangfireExtensionOptions> options)
    : IConfigureOptions<RazorPagesOptions>
{
    public void Configure(RazorPagesOptions razorPagesOptions)
    {
        var settings = options.Value;
        if (settings.RequireAuthorization)
        {
            razorPagesOptions.Conventions.Add(new HangfireExtensionAuthorizationConvention(settings.AuthorizationPolicy));
        }

        razorPagesOptions.Conventions.Add(new HangfireExtensionPageRouteConvention(settings.RoutePrefixNormalized));
    }

    private sealed class HangfireExtensionAuthorizationConvention(string policy) : IPageApplicationModelConvention
    {
        public void Apply(PageApplicationModel model)
        {
            if (!string.Equals(model.AreaName, "HangfireExtension", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!model.ViewEnginePath.StartsWith("/RecurringJobs", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            model.Filters.Add(new AuthorizeFilter(policy));
        }
    }

    private sealed class HangfireExtensionPageRouteConvention(string routePrefix) : IPageRouteModelConvention
    {
        public void Apply(PageRouteModel model)
        {
            if (!string.Equals(model.AreaName, "HangfireExtension", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var template = model.ViewEnginePath switch
            {
                "/RecurringJobs/Index" => routePrefix,
                "/RecurringJobs/Edit" => $"{routePrefix}/{{id}}",
                _ => null
            };

            if (template is null)
            {
                return;
            }

            model.Selectors.Add(new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel
                {
                    Template = template
                }
            });
        }
    }
}
