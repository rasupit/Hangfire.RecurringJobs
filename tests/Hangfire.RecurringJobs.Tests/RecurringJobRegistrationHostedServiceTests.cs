using Hangfire;
using Hangfire.Common;
using Hangfire.RecurringJobs.Models;
using Hangfire.RecurringJobs.Services;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hangfire.RecurringJobs.Tests;

public sealed class RecurringJobRegistrationHostedServiceTests
{
    private readonly IRecurringJobManager recurringJobManager = Substitute.For<IRecurringJobManager>();

    [Fact]
    public async Task StartAsync_DoesNotRegisterJobs_WhenAutoRegisterOnStartupIsFalse()
    {
        var definition = MakeDefinition("job-1");
        var service = CreateSubject([definition], autoRegisterOnStartup: false);

        await service.StartAsync(CancellationToken.None);

#pragma warning disable CS0618
        recurringJobManager.DidNotReceive().AddOrUpdate(
            Arg.Any<string>(), Arg.Any<Job>(), Arg.Any<string>(), Arg.Any<RecurringJobOptions>());
#pragma warning restore CS0618
    }

    [Fact]
    public async Task StartAsync_RegistersAllDefinitions_WhenAutoRegisterOnStartupIsTrue()
    {
        var d1 = MakeDefinition("job-1", "0 * * * *");
        var d2 = MakeDefinition("job-2", "15 * * * *");
        var service = CreateSubject([d1, d2], autoRegisterOnStartup: true);

        await service.StartAsync(CancellationToken.None);

#pragma warning disable CS0618
        recurringJobManager.Received(1).AddOrUpdate(
            d1.Id, d1.Job, d1.CronExpression, Arg.Any<RecurringJobOptions>());
        recurringJobManager.Received(1).AddOrUpdate(
            d2.Id, d2.Job, d2.CronExpression, Arg.Any<RecurringJobOptions>());
#pragma warning restore CS0618
    }

    [Fact]
    public async Task StartAsync_UsesDefaultTimeZone_WhenDefinitionTimeZoneIsNull()
    {
        var defaultTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore");
        var definition = MakeDefinition("job-1", timeZone: null);
        var service = CreateSubject([definition], autoRegisterOnStartup: true, defaultTimeZone: defaultTz);

        await service.StartAsync(CancellationToken.None);

#pragma warning disable CS0618
        recurringJobManager.Received(1).AddOrUpdate(
            definition.Id,
            definition.Job,
            definition.CronExpression,
            Arg.Is<RecurringJobOptions>(o => o.TimeZone == defaultTz));
#pragma warning restore CS0618
    }

    [Fact]
    public async Task StartAsync_UsesDefinitionTimeZone_WhenExplicitlySet()
    {
        var definitionTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore");
        var defaultTz = TimeZoneInfo.Utc;
        var definition = MakeDefinition("job-1", timeZone: definitionTz);
        var service = CreateSubject([definition], autoRegisterOnStartup: true, defaultTimeZone: defaultTz);

        await service.StartAsync(CancellationToken.None);

#pragma warning disable CS0618
        recurringJobManager.Received(1).AddOrUpdate(
            definition.Id,
            definition.Job,
            definition.CronExpression,
            Arg.Is<RecurringJobOptions>(o => o.TimeZone == definitionTz));
#pragma warning restore CS0618
    }

    private RecurringJobRegistrationHostedService CreateSubject(
        IEnumerable<RecurringJobDefinition> definitions,
        bool autoRegisterOnStartup,
        TimeZoneInfo? defaultTimeZone = null)
    {
        var options = Options.Create(new RecurringJobsOptions
        {
            AutoRegisterOnStartup = autoRegisterOnStartup,
            DefaultTimeZone = defaultTimeZone ?? TimeZoneInfo.Utc
        });

        return new RecurringJobRegistrationHostedService(definitions, recurringJobManager, options);
    }

    private static RecurringJobDefinition MakeDefinition(
        string id,
        string cronExpression = "0 * * * *",
        TimeZoneInfo? timeZone = null)
        => new(id, Job.FromExpression(() => SampleJob.Run()), cronExpression, timeZone, "default");

    private static class SampleJob
    {
        public static void Run() { }
    }
}
