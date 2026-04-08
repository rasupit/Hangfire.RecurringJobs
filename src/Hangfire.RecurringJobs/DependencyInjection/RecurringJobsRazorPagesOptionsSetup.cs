using Hangfire.RecurringJobs;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Hangfire.RecurringJobs.DependencyInjection;

internal sealed class RecurringJobsRazorPagesOptionsSetup(IOptions<RecurringJobsOptions> options)
    : IConfigureOptions<RazorPagesOptions>
{
    public void Configure(RazorPagesOptions razorPagesOptions)
    {
        var settings = options.Value;
        if (settings.RequireAuthorization)
        {
            razorPagesOptions.Conventions.Add(new RecurringJobsAuthorizationConvention(settings.AuthorizationPolicy));
        }

        razorPagesOptions.Conventions.Add(new RecurringJobsPageRouteConvention(settings.RoutePrefixNormalized));
    }

    private sealed class RecurringJobsAuthorizationConvention(string policy) : IPageApplicationModelConvention
    {
        public void Apply(PageApplicationModel model)
        {
            if (!model.ViewEnginePath.StartsWith("/RecurringJobs", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            model.Filters.Add(new AuthorizeFilter(policy));
        }
    }

    private sealed class RecurringJobsPageRouteConvention(string routePrefix) : IPageRouteModelConvention
    {
        public void Apply(PageRouteModel model)
        {
            var template = model.ViewEnginePath switch
            {
                "/RecurringJobs/Index" => routePrefix,
                "/RecurringJobs/Edit" => $"{routePrefix}/{{id}}/edit",
                _ => null
            };

            if (template is null)
            {
                return;
            }

            model.Selectors.Clear();
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
