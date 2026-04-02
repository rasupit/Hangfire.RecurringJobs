using Hangfire;
using Hangfire.Common;
using Hangfire.Extension.Core.Abstractions;
using Hangfire.Extension.Core.Models;
using Hangfire.Extension.Core.Services;
using NSubstitute;

namespace Hangfire.Extension.Tests;

public sealed class RecurringJobAdminServiceTests
{
    private readonly IRecurringJobStorage storage = Substitute.For<IRecurringJobStorage>();
    private readonly IRecurringJobDefinitionProvider definitionProvider = Substitute.For<IRecurringJobDefinitionProvider>();
    private readonly ICronExpressionValidator cronValidator = new CronExpressionValidator();
    private readonly IRecurringJobManager recurringJobManager = Substitute.For<IRecurringJobManager>();

    [Fact]
    public async Task UpdateCronAsync_ReturnsFailure_WhenCronIsInvalid()
    {
        var service = CreateSubject();

        var result = await service.UpdateCronAsync("job-1", "invalid");

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
        await definitionProvider.DidNotReceive().GetDefinitionAsync("job-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnableAsync_RecreatesKnownDefinition()
    {
        var definition = new RecurringJobDefinition(
            "job-1",
            Job.FromExpression(() => SampleJob.Run()),
            "0 * * * *",
            TimeZoneInfo.Utc,
            "default");

        definitionProvider.GetDefinitionAsync("job-1", Arg.Any<CancellationToken>())
            .Returns(definition);

        var service = CreateSubject();

        var result = await service.EnableAsync("job-1");

        Assert.True(result.Succeeded);
#pragma warning disable CS0618
        recurringJobManager.Received(1).AddOrUpdate(
            definition.Id,
            definition.Job,
            definition.CronExpression,
            Arg.Is<RecurringJobOptions>(options =>
                options.TimeZone == definition.TimeZone &&
                options.QueueName == definition.Queue));
#pragma warning restore CS0618
    }

    private RecurringJobAdminService CreateSubject()
        => new(storage, definitionProvider, cronValidator, recurringJobManager);

    private static class SampleJob
    {
        public static void Run()
        {
        }
    }
}
