using Hangfire;
using Hangfire.Common;
using Hangfire.Extension.Hangfire;
using Hangfire.Extension.Models;
using Hangfire.Extension.Services;
using Hangfire.Storage.SQLite;
using NSubstitute;

namespace Hangfire.Extension.Tests;

public sealed class RecurringJobAdminServiceTests : IDisposable
{
    private readonly SQLiteStorage storage;
    private readonly IRecurringJobManager recurringJobManager = Substitute.For<IRecurringJobManager>();

    public RecurringJobAdminServiceTests()
    {
        DatabasePath = Path.Combine(
            Path.GetTempPath(),
            "hangfire-extension-admin-service-tests",
            $"{Guid.NewGuid():N}.db");

        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        storage = new SQLiteStorage(DatabasePath);
    }

    public string DatabasePath { get; }

    [Fact]
    public async Task UpdateCronAsync_ReturnsFailure_WhenCronIsInvalid()
    {
        var service = CreateSubject();

        var result = await service.UpdateCronAsync("job-1", "invalid");

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
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

        var service = CreateSubject(definition);

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

    private RecurringJobAdminService CreateSubject(params RecurringJobDefinition[] definitions)
        => new(new RecurringJobStorage(storage), definitions, new CronExpressionValidator(), recurringJobManager);

    public void Dispose()
    {
        storage.Dispose();

        if (File.Exists(DatabasePath))
        {
            File.Delete(DatabasePath);
        }
    }

    private static class SampleJob
    {
        public static void Run()
        {
        }
    }
}
