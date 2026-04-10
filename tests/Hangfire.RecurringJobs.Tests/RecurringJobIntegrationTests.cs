using Hangfire;
using Hangfire.Common;
using Hangfire.RecurringJobs.Hangfire;
using Hangfire.RecurringJobs.Models;
using Hangfire.RecurringJobs.Services;
using Hangfire.Storage.SQLite;

namespace Hangfire.RecurringJobs.Tests;

public sealed class RecurringJobIntegrationTests
{
    [Fact]
    public async Task GetJobsAsync_ReturnsPaginatedResults()
    {
        using var fixture = new SQLiteStorageFixture();
        var recurringJobManager = new RecurringJobManager(fixture.Storage);
        var alphaDefinition = new RecurringJobDefinition(
            "job-alpha",
            Job.FromExpression(() => SampleRecurringJobs.RunAlpha()),
            "0 * * * *", TimeZoneInfo.Utc, "default");
        var betaDefinition = new RecurringJobDefinition(
            "job-beta",
            Job.FromExpression(() => SampleRecurringJobs.RunBeta()),
            "0 12 * * *", TimeZoneInfo.Utc, "default");

        recurringJobManager.AddOrUpdate("job-alpha", alphaDefinition.Job, "0 * * * *");
        recurringJobManager.AddOrUpdate("job-beta", betaDefinition.Job, "0 12 * * *");

        var service = new RecurringJobAdminService(
            new RecurringJobStorage(fixture.Storage),
            [alphaDefinition, betaDefinition],
            new CronExpressionValidator(),
            recurringJobManager);

        var page = await service.GetJobsAsync(new RecurringJobQuery(Page: 1, PageSize: 1, Search: "job"));

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
        Assert.NotNull(disabledJob);
        Assert.True(disabledJob!.IsDisabled);

        var enableResult = await service.EnableAsync(definition.Id);
        var enabledJob = await service.GetJobAsync(definition.Id);

        Assert.True(enableResult.Succeeded);
        Assert.NotNull(enabledJob);
        Assert.Equal(definition.CronExpression, enabledJob!.CronExpression);
    }

    [Fact]
    public async Task GetJobsAsync_OnlyReturnsCodeDefinedJobs_AndKeepsDisabledDefinitionsVisible()
    {
        using var fixture = new SQLiteStorageFixture();
        var recurringJobManager = new RecurringJobManager(fixture.Storage);
        var activeDefinition = new RecurringJobDefinition(
            "job-active",
            Job.FromExpression(() => SampleRecurringJobs.RunAlpha()),
            "0 * * * *",
            TimeZoneInfo.Utc,
            "default");
        var disabledDefinition = new RecurringJobDefinition(
            "job-disabled",
            Job.FromExpression(() => SampleRecurringJobs.RunBeta()),
            "0 12 * * *",
            TimeZoneInfo.Utc,
            "default");

        recurringJobManager.AddOrUpdate(
            activeDefinition.Id,
            activeDefinition.Job,
            activeDefinition.CronExpression);

        recurringJobManager.AddOrUpdate(
            "job-orphaned",
            Job.FromExpression(() => SampleRecurringJobs.RunBeta()),
            "15 * * * *");

        var service = new RecurringJobAdminService(
            new RecurringJobStorage(fixture.Storage),
            [activeDefinition, disabledDefinition],
            new CronExpressionValidator(),
            recurringJobManager);

        var page = await service.GetJobsAsync(new RecurringJobQuery(Page: 1, PageSize: 20));

        Assert.Equal(2, page.TotalCount);
        Assert.DoesNotContain(page.Items, item => item.Id == "job-orphaned");
        Assert.Contains(page.Items, item => item.Id == "job-active" && !item.IsDisabled);
        Assert.Contains(page.Items, item => item.Id == "job-disabled" && item.IsDisabled);
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
