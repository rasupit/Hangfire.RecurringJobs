using System.Net.Http.Json;
using Hangfire;
using Hangfire.Common;
using Hangfire.RecurringJobs.Hangfire;
using Hangfire.RecurringJobs.Models;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.DataProtection;
using NSubstitute;

namespace Hangfire.RecurringJobs.Tests;

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
        var content = await GetStringEnsuringSuccessAsync(client, "/recurring-jobs");

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
        var content = await GetStringEnsuringSuccessAsync(client, "/recurring-jobs");

        Assert.Contains("/recurring-jobs/host-job-edit/edit", content);
        Assert.DoesNotContain("/RecurringJobs/Edit/host-job-edit", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecurringJobsPage_UsesEmbeddedFallbackThemeByDefault()
    {
        await using var factory = new RecurringJobsWebAppFactory();
        using var client = factory.CreateHttpsClient();

        var content = await GetStringEnsuringSuccessAsync(client, "/recurring-jobs");

        Assert.Contains("<style>", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".navbar", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bootstrap.min.css", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hangfire-extension.css", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecurringJobsPage_RendersLibraryLayout()
    {
        await using var factory = new RecurringJobsWebAppFactory();
        using var client = factory.CreateHttpsClient();

        var content = await GetStringEnsuringSuccessAsync(client, "/recurring-jobs");

        Assert.Contains("class=\"navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3\"", content);
        Assert.Contains("<div class=\"container\">", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<title>Recurring Jobs - Hangfire.RecurringJobs</title>", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecurringJobsPage_UsesConfiguredThemePath_WhenStylesAreOverridden()
    {
        await using var factory = new RecurringJobsWebAppFactory("/lib/bootstrap/dist/css/bootstrap.min.css", "/css/site.css");
        using var client = factory.CreateHttpsClient();

        var content = await GetStringEnsuringSuccessAsync(client, "/recurring-jobs");

        Assert.DoesNotContain("<style>", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/lib/bootstrap/dist/css/bootstrap.min.css", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/css/site.css", content, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public async Task RecurringJobsPage_ShowsUnavailableBanner_WhenStorageIsUnavailable()
    {
        await using var factory = new RecurringJobsWebAppFactory(storageUnavailable: true);
        using var client = factory.CreateHttpsClient();

        var content = await GetStringEnsuringSuccessAsync(client, "/recurring-jobs");

        Assert.Contains(RecurringJobStorage.StorageUnavailableMessage, content);
        Assert.Contains("Editing and operational actions are temporarily unavailable", content);
        Assert.Contains("host-job-alpha", content);
        Assert.Contains("Unavailable", content);
    }

    [Fact]
    public async Task RecurringJobsApi_ReturnsServiceUnavailable_WhenStorageIsUnavailable()
    {
        await using var factory = new RecurringJobsWebAppFactory(storageUnavailable: true);
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/recurring-jobs/api/jobs/recurring");

        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task CronPreviewEndpoint_ReturnsHumanReadablePreview()
    {
        await using var factory = new RecurringJobsWebAppFactory();
        using var client = factory.CreateHttpsClient();

        var preview = await client.GetFromJsonAsync<RecurringJobCronPreview>(
            "/recurring-jobs/api/jobs/recurring/preview?cronExpression=0%202%20*%20*%20*");

        Assert.NotNull(preview);
        Assert.True(preview!.IsValid);
        Assert.False(string.IsNullOrWhiteSpace(preview.Description));
        Assert.NotEmpty(preview.UpcomingOccurrences);
    }

    private sealed class RecurringJobsWebAppFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly JobStorage storage;
        private readonly IRecurringJobManager recurringJobManager;
        private readonly SQLiteStorage? ownedSqliteStorage;
        private readonly string dataProtectionKeysDirectory;
        private readonly string[] stylesPaths;
        private readonly bool storageUnavailable;

        public RecurringJobsWebAppFactory(params string[] stylesPaths)
            : this(false, stylesPaths)
        {
        }

        public RecurringJobsWebAppFactory(bool storageUnavailable, params string[] stylesPaths)
        {
            this.storageUnavailable = storageUnavailable;
            this.stylesPaths = stylesPaths;
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                "hangfire-extension-host-tests",
                $"{Guid.NewGuid():N}.db");

            if (storageUnavailable)
            {
                storage = Substitute.For<JobStorage>();
                storage.GetConnection().Returns(_ => throw new InvalidOperationException("Storage unavailable."));
                recurringJobManager = Substitute.For<IRecurringJobManager>();
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
                ownedSqliteStorage = new SQLiteStorage(DatabasePath);
                storage = ownedSqliteStorage;
                recurringJobManager = new RecurringJobManager(storage);
            }

            dataProtectionKeysDirectory = Path.Combine(
                Path.GetTempPath(),
                "hangfire-extension-test-keys",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(dataProtectionKeysDirectory);
        }

        public string DatabasePath { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<RecurringJobsOptions>(options =>
                {
                    options.Styles.Clear();
                    foreach (var stylePath in stylesPaths)
                    {
                        options.Styles.Add(stylePath);
                    }
                });
            });
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysDirectory));

                services.RemoveAll<JobStorage>();
                services.RemoveAll<IRecurringJobManager>();

                services.AddSingleton<JobStorage>(storage);
                services.AddSingleton<IRecurringJobManager>(_ => recurringJobManager);
                services.AddSingleton(new RecurringJobDefinition(
                    "host-job-alpha",
                    Job.FromExpression(() => SampleRecurringJobs.RunAlpha()),
                    "0 * * * *",
                    TimeZoneInfo.Utc,
                    "default"));
                services.AddSingleton(new RecurringJobDefinition(
                    "host-job-beta",
                    Job.FromExpression(() => SampleRecurringJobs.RunBeta()),
                    "15 * * * *",
                    TimeZoneInfo.Utc,
                    "default"));
                services.AddSingleton(new RecurringJobDefinition(
                    "host-job-edit",
                    Job.FromExpression(() => SampleRecurringJobs.RunAlpha()),
                    "0 * * * *",
                    TimeZoneInfo.Utc,
                    "default"));
            });
        }

        public HttpClient CreateHttpsClient()
            => CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        public async Task SeedRecurringJobAsync(string recurringJobId, Job job, string cronExpression)
        {
            if (storageUnavailable)
            {
                throw new InvalidOperationException("Cannot seed recurring jobs when storage is unavailable.");
            }

            using var scope = Services.CreateScope();
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

            recurringJobManager.AddOrUpdate(recurringJobId, job, cronExpression);
            await Task.CompletedTask;
        }

        public new async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            ownedSqliteStorage?.Dispose();

            if (ownedSqliteStorage is not null && File.Exists(DatabasePath))
            {
                File.Delete(DatabasePath);
            }

            if (Directory.Exists(dataProtectionKeysDirectory))
            {
                Directory.Delete(dataProtectionKeysDirectory, recursive: true);
            }
        }
    }

    private static async Task<string> GetStringEnsuringSuccessAsync(HttpClient client, string requestUri)
    {
        var response = await client.GetAsync(requestUri);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Xunit.Sdk.XunitException(
                $"GET {requestUri} returned {(int)response.StatusCode} {response.StatusCode}.{Environment.NewLine}{content}");
        }

        return content;
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
