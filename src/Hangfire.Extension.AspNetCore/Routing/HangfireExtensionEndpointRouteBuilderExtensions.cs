using Hangfire.Extension.AspNetCore.Models;
using Hangfire.Extension.AspNetCore.Options;
using Hangfire.Extension.Core.Abstractions;
using Hangfire.Extension.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hangfire.Extension.AspNetCore.Routing;

public static class HangfireExtensionEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapHangfireExtension(this IEndpointRouteBuilder endpoints)
        => endpoints.MapHangfireExtensionCore();

    public static IEndpointRouteBuilder MapHangfireExtension(this IEndpointRouteBuilder endpoints, string routePrefix)
    {
        var options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<HangfireExtensionOptions>>()
            .Value;

        var normalizedRoutePrefix = HangfireExtensionOptions.NormalizeRoutePrefix(routePrefix);
        if (!string.Equals(options.RoutePrefixNormalized, normalizedRoutePrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Hangfire.Extension was registered with route prefix '{options.RoutePrefixNormalized}', but MapHangfireExtension was called with '{normalizedRoutePrefix}'. Use the same prefix in both places.");
        }

        return endpoints.MapHangfireExtensionCore();
    }

    private static IEndpointRouteBuilder MapHangfireExtensionCore(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<HangfireExtensionOptions>>()
            .Value;

        var recurringJobs = endpoints.MapGroup(options.ApiRoutePrefix);
        if (options.RequireAuthorization)
        {
            recurringJobs.RequireAuthorization(options.AuthorizationPolicy);
        }

        recurringJobs.MapGet("/", async (
            [AsParameters] RecurringJobQuery query,
            IRecurringJobAdminService service,
            CancellationToken cancellationToken) =>
        {
            var jobs = await service.GetJobsAsync(query, cancellationToken);
            return Results.Ok(jobs);
        });

        recurringJobs.MapPost("/{id}/trigger", async Task<IResult> (
            string id,
            IRecurringJobAdminService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.TriggerAsync(id, cancellationToken);
            return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
        });

        recurringJobs.MapPost("/{id}/enable", async Task<IResult> (
            string id,
            IRecurringJobAdminService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.EnableAsync(id, cancellationToken);
            return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
        });

        recurringJobs.MapPost("/{id}/disable", async Task<IResult> (
            string id,
            IRecurringJobAdminService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.DisableAsync(id, cancellationToken);
            return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
        });

        recurringJobs.MapPut("/{id}", async Task<IResult> (
            string id,
            [FromBody] RecurringJobUpdateRequest request,
            IRecurringJobAdminService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateCronAsync(id, request.CronExpression, cancellationToken);
            return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
        });

        return endpoints;
    }
}
