using Hangfire.Extension.AspNetCore.DependencyInjection;
using Hangfire.Extension.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.Extension.Tests;

public sealed class RecurringJobDefinitionRegistrationTests
{
    [Fact]
    public async Task AddRecurringJobDefinition_RegistersDefinitionForLookup()
    {
        var services = new ServiceCollection();
        services.AddRecurringJobDefinition<SampleJob>(
            "job-1",
            job => job.Run(),
            "0 * * * *");

        await using var provider = services.BuildServiceProvider();
        var definitionProvider = provider.GetRequiredService<IRecurringJobDefinitionProvider>();

        var definition = await definitionProvider.GetDefinitionAsync("job-1");

        Assert.NotNull(definition);
        Assert.Equal("job-1", definition!.Id);
        Assert.Equal("0 * * * *", definition.CronExpression);
    }

    private sealed class SampleJob
    {
        public void Run()
        {
        }
    }
}
