using Hangfire.RecurringJobs;
using Hangfire.RecurringJobs.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.RecurringJobs.Tests;

public sealed class RecurringJobDefinitionRegistrationTests
{
    [Fact]
    public void AddRecurringJobDefinition_RegistersDefinition()
    {
        var services = new ServiceCollection();
        services.AddRecurringJobDefinition<SampleJob>(
            "job-1",
            job => job.Run(),
            "0 * * * *");

        using var provider = services.BuildServiceProvider();
        var definition = Assert.Single(provider.GetServices<RecurringJobDefinition>());

        Assert.Equal("job-1", definition.Id);
        Assert.Equal("0 * * * *", definition.CronExpression);
    }

    [Fact]
    public void AddRecurringJobDefinition_LeavesTimeZoneNull_WhenNotSpecified()
    {
        var services = new ServiceCollection();
        services.AddRecurringJobDefinition<SampleJob>(
            "job-1",
            job => job.Run(),
            "0 * * * *");

        using var provider = services.BuildServiceProvider();
        var definition = Assert.Single(provider.GetServices<RecurringJobDefinition>());

        Assert.Null(definition.TimeZone);
    }

    [Fact]
    public void AddRecurringJobDefinition_StoresExplicitTimeZone_WhenProvided()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore");
        var services = new ServiceCollection();
        services.AddRecurringJobDefinition<SampleJob>(
            "job-1",
            job => job.Run(),
            "0 * * * *",
            tz);

        using var provider = services.BuildServiceProvider();
        var definition = Assert.Single(provider.GetServices<RecurringJobDefinition>());

        Assert.Equal(tz, definition.TimeZone);
    }

    private sealed class SampleJob
    {
        public void Run()
        {
        }
    }
}
