using Hangfire.RecurringJobs.Models;
using Hangfire.RecurringJobs.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hangfire.RecurringJobs;

public static class RecurringJobsEndpointRouteBuilderExtensions
{
    private const string UnexpectedErrorMessage = "The recurring job request could not be completed.";

    public static IEndpointRouteBuilder MapHangfireRecurringJobsApi(this IEndpointRouteBuilder endpoints)
        => endpoints.MapRecurringJobsApiCore();

    private static IEndpointRouteBuilder MapRecurringJobsApiCore(this IEndpointRouteBuilder endpoints, string? routePrefix = null)
    {
        var options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<RecurringJobsOptions>>()
            .Value;

        if (!string.IsNullOrWhiteSpace(routePrefix))
        {
            var normalizedRoutePrefix = RecurringJobsOptions.NormalizeRoutePrefix(routePrefix);
            if (!string.Equals(options.RoutePrefixNormalized, normalizedRoutePrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Hangfire.RecurringJobs was registered with route prefix '{options.RoutePrefixNormalized}', but API mapping was called with '{normalizedRoutePrefix}'. Configure the route in AddHangfireRecurringJobs and use the same value everywhere.");
            }
        }

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

        recurringJobs.MapGet("/{id}", async (
            string id,
            RecurringJobAdminService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var job = await service.GetJobAsync(id, cancellationToken);
                return job is null
                    ? Results.NotFound()
                    : Results.Ok(job);
            }
            catch
            {
                return Results.Problem(
                    title: "Recurring job unavailable",
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

        recurringJobs.MapGet("/preview", (
            [FromQuery] string cronExpression,
            CronExpressionPreviewService previewService) =>
        {
            try
            {
                return Results.Ok(previewService.CreatePreview(cronExpression));
            }
            catch
            {
                return Results.Problem(
                    title: "Cron preview unavailable",
                    detail: UnexpectedErrorMessage,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
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
