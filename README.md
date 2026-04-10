# Hangfire.RecurringJobs

`Rasupit.Hangfire.RecurringJobs` is a small ASP.NET Core admin surface for Hangfire recurring jobs.

[NuGet package](https://www.nuget.org/packages/Rasupit.Hangfire.RecurringJobs)

It is meant to plug into an existing ASP.NET Core application that already uses Hangfire and already owns its real storage configuration.

## What It Provides

- A recurring jobs page with search and pagination
- Inline trigger and active or disabled toggle actions
- Cron editing from an index-page popup, with a full edit page fallback
- Human-readable cron descriptions and upcoming-run previews
- Support for valid schedules with no future occurrence for manual-trigger-only jobs
- A small JSON API for recurring job operations
- `SkipConcurrentExecutionAttribute` for skip-on-overlap behavior

## Design Goals

- Admin-focused UI for Hangfire recurring jobs
- Intended for operational or back-office use, not end-user-facing screens
- Simple host integration
- Public Hangfire APIs only
- One consumer namespace: `Hangfire.RecurringJobs`

## Package And Namespace

Current package identity:

- `Rasupit.Hangfire.RecurringJobs`

Consumer namespace:

```csharp
using Hangfire.RecurringJobs;
```

## Expected Host Setup

Your host application should already:

- use ASP.NET Core
- use Hangfire
- configure a real Hangfire storage provider
- register its recurring jobs in code

This library does not configure storage for you.

## Integration

### 1. Keep Your Existing Hangfire Setup

```csharp
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(configuration =>
{
    configuration.UseSimpleAssemblyNameTypeSerializer();
    configuration.UseRecommendedSerializerSettings();

    // Keep your existing storage configuration here.
});
```

### 2. Register The Recurring Jobs UI

```csharp
using Hangfire.RecurringJobs;

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RecurringJobAdmin", policy => policy.RequireRole("Admin"));

builder.Services.AddHangfireRecurringJobs("/recurring-jobs");
```

This registers:

- recurring job services
- Razor Pages conventions for the embedded UI
- cron preview services
- the recurring jobs API options

### 3. Map Razor Pages And The API

```csharp
var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHangfireRecurringJobsApi();
app.MapRazorPages();

app.Run();
```

`MapHangfireRecurringJobsApi()` only maps the JSON API endpoints.

The UI route itself is configured through `AddHangfireRecurringJobs(...)` and served by normal Razor Pages endpoint mapping.

`MapStaticAssets()` is still recommended for the library's packaged static assets, but the default recurring jobs page no longer depends on separate runtime requests for Bootstrap, `hangfire-extension.css`, or the recurring jobs JavaScript.

Package-owned static files are served from `/hangfire-extension/...`, so the library does not compete with a host application's own `wwwroot/lib/...`, `wwwroot/css/...`, or root files such as `favicon.ico`.

## Routes

With the example above, the canonical UI routes are:

- `/recurring-jobs`
- `/recurring-jobs/{id}/edit`

The API routes are:

- `GET /recurring-jobs/api/jobs/recurring`
- `GET /recurring-jobs/api/jobs/recurring/{id}`
- `POST /recurring-jobs/api/jobs/recurring/{id}/trigger`
- `POST /recurring-jobs/api/jobs/recurring/{id}/enable`
- `POST /recurring-jobs/api/jobs/recurring/{id}/disable`
- `GET /recurring-jobs/api/jobs/recurring/preview?cronExpression=...`
- `PUT /recurring-jobs/api/jobs/recurring/{id}`

If you want a different base path, configure it once in `AddHangfireRecurringJobs(...)`.

## Authorization

The default policy name is:

- `RecurringJobAdmin`

You can change it during registration:

```csharp
builder.Services.AddHangfireRecurringJobs("/operations/jobs", "OperationsAdmin");
```

For demos or local-only usage, you can disable authorization explicitly:

```csharp
builder.Services.AddHangfireRecurringJobs(options =>
{
    options.RoutePrefix = "/recurring-jobs";
    options.RequireAuthorization = false;
    options.Styles.Add("/lib/bootstrap/dist/css/bootstrap.min.css");
    options.Styles.Add("/css/site.css");
});
```

If `Styles` is not configured, the library inlines its embedded Bootstrap-compatible theme automatically.

If `Styles` contains entries, the library always injects a small `<style>` block for its custom components (the toggle switch, cron editor dialog, and toast notifications — none of which have a Bootstrap equivalent) and then renders each configured stylesheet link in order. This means the configured stylesheets own all the standard Bootstrap utility and layout styling, while the library keeps only the component rules it cannot delegate to Bootstrap.

## Registered Definitions

Enable and cron-update operations need a code-based definition source so the library can safely recreate the recurring job.

The recurring jobs list is definition-driven. Jobs that exist only in Hangfire storage and no longer have a matching code registration are treated as orphaned entries and are not shown in the admin UI.

```csharp
builder.Services.AddRecurringJobDefinition<ReportJobs>(
    "nightly-report",
    jobs => jobs.RunNightlyReport(),
    "0 2 * * *",
    TimeZoneInfo.Utc,
    "default");
```

You can also register with an explicit Hangfire `Job`:

```csharp
using Hangfire.Common;

builder.Services.AddRecurringJobDefinition(
    "nightly-report",
    Job.FromExpression<ReportJobs>(jobs => jobs.RunNightlyReport()),
    "0 2 * * *",
    TimeZoneInfo.Utc,
    "default");
```

## Full Example

```csharp
using Hangfire;
using Hangfire.RecurringJobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RecurringJobAdmin", policy => policy.RequireRole("Admin"));

builder.Services.AddHangfire(configuration =>
{
    configuration.UseSimpleAssemblyNameTypeSerializer();
    configuration.UseRecommendedSerializerSettings();

    // Reuse your existing Hangfire storage here.
});

builder.Services.AddHangfireRecurringJobs("/recurring-jobs");
builder.Services.AddRecurringJobDefinition<ReportJobs>(
    "nightly-report",
    jobs => jobs.RunNightlyReport(),
    "0 2 * * *",
    TimeZoneInfo.Utc,
    "default");

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHangfireRecurringJobsApi();
app.MapRazorPages();

app.Run();
```

## UI Behavior

- The `Status` column owns the active or disabled lifecycle state and uses a toggle switch.
- `Execution` shows the last known run details, including last failure information.
- `Trigger`, toggle, and popup cron save update the affected row inline instead of refreshing the whole page.
- `Trigger` and cron save show toast feedback. Successful enable or disable relies on the visible row-state change instead.
- The index page uses a popup cron editor on larger screens, and the full edit page remains available as the fallback route.

## Cron Editing

Cron expressions are previewed as you type.

- Parseable expressions show a plain-English description and the next few run times.
- Expressions that are valid but have no future occurrence are allowed and shown as manual-trigger-only schedules.
- Saving a disabled job applies the new schedule and enables it again.

## SkipConcurrentExecutionAttribute

Use `SkipConcurrentExecutionAttribute` when overlapping executions should be skipped instead of waiting.

```csharp
using Hangfire.RecurringJobs;

public sealed class ReportJobs
{
    [SkipConcurrentExecution(timeoutInSeconds: 30)]
    public Task RunNightlyReport()
        => Task.CompletedTask;
}
```

Behavior:

- the first execution acquires a distributed lock
- a concurrent execution that cannot acquire the lock is canceled
- the second run is skipped instead of queued

## Verification

Current automated coverage includes:

- service tests
- storage integration tests
- host integration tests
- canonical route tests for the embedded UI

## Notes

- The embedded UI is implemented with Razor Pages inside the library project.
- The library includes static web assets for the embedded UI under `/hangfire-extension/...`.
- `MapRazorPages()` is still required in the host, because the UI is delivered as Razor Pages.
- `MapHangfireRecurringJobsApi()` maps the JSON endpoints only; the UI still comes from Razor Pages under the configured route prefix.
