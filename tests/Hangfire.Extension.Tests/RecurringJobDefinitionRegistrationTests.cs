using Hangfire.Extension;
using Hangfire.Extension.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.Extension.Tests;

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

    private sealed class SampleJob
    {
        public void Run()
        {
        }
    }
}
