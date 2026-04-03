using System.Net.Http.Json;
using Hangfire;
using Hangfire.Common;
using Hangfire.Extension.Models;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hangfire.Extension.Tests;

public sealed class RecurringJobHostIntegrationTests
{
    [Fact]
    public async Task RecurringJobsPage_RendersSeededJob()
    {
        await using var factory = new RecurringJobsWebAppFactory();
        await factory.SeedRecurringJobAsync(
            "host-job-alpha",
            Job.FromExpression(() => SampleRecurringJobs.RunAlpha()),
            "0 * * * *");

        using var client = factory.CreateHttpsClient();
        var response = await client.GetAsync("/recurring-jobs");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Recurring Jobs", content);
        Assert.Contains("host-job-alpha", content);
    }

    [Fact]
    public async Task RecurringJobsPage_UsesCanonicalEditUrl()
    {
        await using var factory = new RecurringJobsWebAppFactory();
        await factory.SeedRecurringJobAsync(
            "host-job-edit",
            Job.FromExpression(() => SampleRecurringJobs.RunAlpha()),
            "0 * * * *");

        using var client = factory.CreateHttpsClient();
        var content = await client.GetStringAsync("/recurring-jobs");

        Assert.Contains("/recurring-jobs/host-job-edit/edit", content);
        Assert.DoesNotContain("/RecurringJobs/Edit/host-job-edit", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InternalAreaRoute_IsNotExposed()
    {
        await using var factory = new RecurringJobsWebAppFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/RecurringJobs");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RecurringJobsApi_ReturnsPagedResponse()
    {
        await using var factory = new RecurringJobsWebAppFactory();
        await factory.SeedRecurringJobAsync(
            "host-job-beta",
            Job.FromExpression(() => SampleRecurringJobs.RunBeta()),
            "15 * * * *");

        using var client = factory.CreateHttpsClient();
        var page = await client.GetFromJsonAsync<RecurringJobPage>(
            "/recurring-jobs/api/jobs/recurring?page=1&pageSize=10&search=beta");

        Assert.NotNull(page);
        Assert.Equal(1, page!.Page);
        Assert.Equal(10, page.PageSize);
        Assert.True(page.TotalCount >= 1);
        Assert.Equal("beta", page.Search);
        Assert.False(page.HasPreviousPage);
        Assert.Contains(page.Items, item => item.Id == "host-job-beta");
    }

    private sealed class RecurringJobsWebAppFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly SQLiteStorage storage;

        public RecurringJobsWebAppFactory()
        {
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                "hangfire-extension-host-tests",
                $"{Guid.NewGuid():N}.db");

            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
            storage = new SQLiteStorage(DatabasePath);
        }

        public string DatabasePath { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<JobStorage>();
                services.RemoveAll<IRecurringJobManager>();

                services.AddSingleton<JobStorage>(storage);
                services.AddSingleton<IRecurringJobManager>(_ => new RecurringJobManager(storage));
            });
        }

        public HttpClient CreateHttpsClient()
            => CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        public async Task SeedRecurringJobAsync(string recurringJobId, Job job, string cronExpression)
        {
            using var scope = Services.CreateScope();
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

            recurringJobManager.AddOrUpdate(recurringJobId, job, cronExpression);
            await Task.CompletedTask;
        }

        public new async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            storage.Dispose();

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
