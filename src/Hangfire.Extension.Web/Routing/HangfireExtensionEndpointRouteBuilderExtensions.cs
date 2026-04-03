using Hangfire.Extension.Web.Models;
using Hangfire.Extension.Web.Options;
using Hangfire.Extension.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hangfire.Extension.Web.Routing;

public static class HangfireExtensionEndpointRouteBuilderExtensions
{
    private const string UnexpectedErrorMessage = "The recurring job request could not be completed.";

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
            RecurringJobAdminService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var jobs = await service.GetJobsAsync(query, cancellationToken);
                return Results.Ok(jobs);
            }
            catch
            {
                return Results.Problem(
                    title: "Recurring jobs unavailable",
                    detail: UnexpectedErrorMessage,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        recurringJobs.MapPost("/{id}/trigger", async Task<IResult> (
            string id,
            RecurringJobAdminService service,
            CancellationToken cancellationToken) =>
        {
            return await ExecuteOperationAsync(
                () => service.TriggerAsync(id, cancellationToken));
        });

        recurringJobs.MapPost("/{id}/enable", async Task<IResult> (
            string id,
            RecurringJobAdminService service,
            CancellationToken cancellationToken) =>
        {
            return await ExecuteOperationAsync(
                () => service.EnableAsync(id, cancellationToken));
        });

        recurringJobs.MapPost("/{id}/disable", async Task<IResult> (
            string id,
            RecurringJobAdminService service,
            CancellationToken cancellationToken) =>
        {
            return await ExecuteOperationAsync(
                () => service.DisableAsync(id, cancellationToken));
        });

        recurringJobs.MapPut("/{id}", async Task<IResult> (
            string id,
            [FromBody] RecurringJobUpdateRequest request,
            RecurringJobAdminService service,
            CancellationToken cancellationToken) =>
        {
            return await ExecuteOperationAsync(
                () => service.UpdateCronAsync(id, request.CronExpression, cancellationToken));
        });

        return endpoints;
    }

    private static async Task<IResult> ExecuteOperationAsync(
        Func<Task<RecurringJobOperationResult>> action)
    {
        try
        {
            var result = await action();
            return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
        }
        catch
        {
            return Results.Problem(
                title: "Recurring job request failed",
                detail: UnexpectedErrorMessage,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
