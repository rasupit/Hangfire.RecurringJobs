using System.Linq.Expressions;
using Hangfire.Common;
using Hangfire.Extension.Core.Models;
using Hangfire.Extension.Infrastructure.Hangfire.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.Extension.AspNetCore.DependencyInjection;

public static class RecurringJobDefinitionServiceCollectionExtensions
{
    public static IServiceCollection AddRecurringJobDefinition(
        this IServiceCollection services,
        string recurringJobId,
        Job job,
        string cronExpression,
        TimeZoneInfo? timeZone = null,
        string queue = "default")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recurringJobId);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);

        return services.AddRecurringJobDefinition(
            new RecurringJobDefinition(
                recurringJobId,
                job,
                cronExpression,
                timeZone ?? TimeZoneInfo.Utc,
                queue));
    }

    public static IServiceCollection AddRecurringJobDefinition<T>(
        this IServiceCollection services,
        string recurringJobId,
        Expression<Action<T>> methodCall,
        string cronExpression,
        TimeZoneInfo? timeZone = null,
        string queue = "default")
    {
        ArgumentNullException.ThrowIfNull(methodCall);

        return services.AddRecurringJobDefinition(
            recurringJobId,
            Job.FromExpression(methodCall),
            cronExpression,
            timeZone,
            queue);
    }

    public static IServiceCollection AddRecurringJobDefinition<T>(
        this IServiceCollection services,
        string recurringJobId,
        Expression<Func<T, Task>> methodCall,
        string cronExpression,
        TimeZoneInfo? timeZone = null,
        string queue = "default")
    {
        ArgumentNullException.ThrowIfNull(methodCall);

        return services.AddRecurringJobDefinition(
            recurringJobId,
            Job.FromExpression(methodCall),
            cronExpression,
            timeZone,
            queue);
    }
}
