using Hangfire;
using Hangfire.Common;
using Hangfire.Extension.Web.Hangfire;
using Hangfire.Extension.Web.Models;
using Hangfire.Extension.Web.Services;
using Hangfire.Storage.SQLite;

namespace Hangfire.Extension.Tests;

public sealed class RecurringJobIntegrationTests
{
    [Fact]
    public async Task GetJobsAsync_ReturnsPagedResults_FromSQLiteStorage()
    {
        using var fixture = new SQLiteStorageFixture();
        var recurringJobManager = new RecurringJobManager(fixture.Storage);

        recurringJobManager.AddOrUpdate(
            "job-alpha",
            Job.FromExpression(() => SampleRecurringJobs.RunAlpha()),
            "0 * * * *");

        recurringJobManager.AddOrUpdate(
            "job-beta",
            Job.FromExpression(() => SampleRecurringJobs.RunBeta()),
            "0 12 * * *");

        var storage = new RecurringJobStorage(fixture.Storage);

        var page = await storage.GetJobsAsync(new RecurringJobQuery(Page: 1, PageSize: 1, Search: "job"));

        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal("job", page.Search);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public async Task DisableAndEnable_WorkAgainstSQLiteStorage()
    {
        using var fixture = new SQLiteStorageFixture();
        var recurringJobManager = new RecurringJobManager(fixture.Storage);
        var definition = new RecurringJobDefinition(
            "job-enable-disable",
            Job.FromExpression(() => SampleRecurringJobs.RunAlpha()),
            "0 * * * *",
            TimeZoneInfo.Utc,
            "default");

        recurringJobManager.AddOrUpdate(
            definition.Id,
            definition.Job,
            definition.CronExpression);

        var service = new RecurringJobAdminService(
            new RecurringJobStorage(fixture.Storage),
            [definition],
            new CronExpressionValidator(),
            recurringJobManager);

        var disableResult = await service.DisableAsync(definition.Id);
        var disabledJob = await service.GetJobAsync(definition.Id);

        Assert.True(disableResult.Succeeded);
        Assert.Null(disabledJob);

        var enableResult = await service.EnableAsync(definition.Id);
        var enabledJob = await service.GetJobAsync(definition.Id);

        Assert.True(enableResult.Succeeded);
        Assert.NotNull(enabledJob);
        Assert.Equal(definition.CronExpression, enabledJob!.CronExpression);
    }

    private sealed class SQLiteStorageFixture : IDisposable
    {
        public SQLiteStorageFixture()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                "hangfire-extension-tests",
                $"{Guid.NewGuid():N}.db");

            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
            Storage = new SQLiteStorage(DatabasePath);
        }

        public string DatabasePath { get; }

        public SQLiteStorage Storage { get; }

        public void Dispose()
        {
            Storage.Dispose();

            if (File.Exists(DatabasePath))
            {
                File.Delete(DatabasePath);
            }
        }
    }

    private static class SampleRecurringJobs
    {
        public static void RunAlpha()
        {
        }

        public static void RunBeta()
        {
        }
    }
}
