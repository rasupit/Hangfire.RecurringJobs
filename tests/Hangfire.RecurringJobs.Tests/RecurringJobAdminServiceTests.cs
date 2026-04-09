using Hangfire;
using Hangfire.Common;
using Hangfire.RecurringJobs.Hangfire;
using Hangfire.RecurringJobs.Models;
using Hangfire.RecurringJobs.Services;
using Hangfire.Storage.SQLite;
using NSubstitute;

namespace Hangfire.RecurringJobs.Tests;

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

    [Fact]
    public async Task GetJobsAsync_ReturnsUnavailableRows_WhenStorageCannotBeRead()
    {
        var definition = new RecurringJobDefinition(
            "job-1",
            Job.FromExpression(() => SampleJob.Run()),
            "0 * * * *",
            TimeZoneInfo.Utc,
            "default");
        var jobStorage = Substitute.For<JobStorage>();
        jobStorage.GetConnection().Returns(_ => throw new InvalidOperationException("Storage unavailable."));
        var service = CreateSubject(new RecurringJobStorage(jobStorage), definition);

        var page = await service.GetJobsAsync(new RecurringJobQuery(Page: 1, PageSize: 10));

        Assert.True(page.IsStorageUnavailable);
        Assert.Equal(RecurringJobStorage.StorageUnavailableMessage, page.StorageErrorMessage);
        Assert.Single(page.Items);
        Assert.Equal(definition.Id, page.Items[0].Id);
        Assert.True(page.Items[0].IsSystemError);
        Assert.True(page.Items[0].IsStorageUnavailable);
    }

    [Fact]
    public async Task GetJobAsync_ReturnsUnavailableSummary_WhenStorageCannotBeRead()
    {
        var definition = new RecurringJobDefinition(
            "job-1",
            Job.FromExpression(() => SampleJob.Run()),
            "0 * * * *",
            TimeZoneInfo.Utc,
            "default");
        var jobStorage = Substitute.For<JobStorage>();
        jobStorage.GetConnection().Returns(_ => throw new InvalidOperationException("Storage unavailable."));
        var service = CreateSubject(new RecurringJobStorage(jobStorage), definition);

        var job = await service.GetJobAsync(definition.Id);

        Assert.NotNull(job);
        Assert.Equal(definition.Id, job!.Id);
        Assert.True(job.IsSystemError);
        Assert.True(job.IsStorageUnavailable);
        Assert.Equal(RecurringJobStorage.StorageUnavailableMessage, job.Error);
    }

    private RecurringJobAdminService CreateSubject(params RecurringJobDefinition[] definitions)
        => CreateSubject(new RecurringJobStorage(storage), definitions);

    private RecurringJobAdminService CreateSubject(RecurringJobStorage recurringJobStorage, params RecurringJobDefinition[] definitions)
        => new(recurringJobStorage, definitions, new CronExpressionValidator(), recurringJobManager);

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
