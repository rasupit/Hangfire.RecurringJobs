using Hangfire.RecurringJobs.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hangfire.RecurringJobs.Services;

internal sealed class RecurringJobRegistrationHostedService : IHostedService
{
    private readonly IEnumerable<RecurringJobDefinition> definitions;
    private readonly IRecurringJobManager recurringJobManager;
    private readonly RecurringJobsOptions options;

    public RecurringJobRegistrationHostedService(
        IEnumerable<RecurringJobDefinition> definitions,
        IRecurringJobManager recurringJobManager,
        IOptions<RecurringJobsOptions> options)
    {
        this.definitions = definitions;
        this.recurringJobManager = recurringJobManager;
        this.options = options.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.AutoRegisterOnStartup)
        {
            return Task.CompletedTask;
        }

        foreach (var definition in definitions)
        {
#pragma warning disable CS0618
            recurringJobManager.AddOrUpdate(
                definition.Id,
                definition.Job,
                definition.CronExpression,
                new RecurringJobOptions
                {
                    TimeZone = definition.TimeZone ?? options.DefaultTimeZone,
                    QueueName = definition.Queue
                });
#pragma warning restore CS0618
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
