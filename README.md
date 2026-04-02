# Hangfire.Extension

`Hangfire.Extension` is a small ASP.NET Core recurring-job admin surface for Hangfire.

It is designed to be added to an existing ASP.NET Core application that already uses Hangfire.

## What This Project Provides

- A recurring jobs page for viewing jobs
- Manual trigger support
- Disable support
- Cron edit support
- A reusable `SkipConcurrentExecutionAttribute`
- Thin service abstractions over public Hangfire APIs

## Expected Host Setup

This project assumes your existing application already:

- uses ASP.NET Core
- has Hangfire installed
- has a real Hangfire storage provider configured
- registers recurring jobs in code

This project does not provide a default Hangfire storage.

## Solution Layout

- `src/Hangfire.Extension.AspNetCore`
- `src/Hangfire.Extension.Web`
- `tests/Hangfire.Extension.Tests`

The runtime code is now compiled into a single library assembly:

- `Hangfire.Extension.AspNetCore.dll`

The internal layering is still separated by folders and namespaces, but not by multiple runtime DLLs.

For an existing app integration, you only need to reference:

- `Hangfire.Extension.AspNetCore`

The `Web` project is now only a sample host.

## How To Wire Into An Existing Project

## 1. Reference The ASP.NET Core Package

Add a reference to:

- `Hangfire.Extension.AspNetCore`

## 2. Keep Your Existing Hangfire Configuration

Your host must already configure Hangfire with a real storage provider.

Example:

```csharp
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(configuration =>
{
    configuration.UseSimpleAssemblyNameTypeSerializer();
    configuration.UseRecommendedSerializerSettings();

    // Keep your existing storage configuration here.
    // Example only:
    // configuration.UseSqlServerStorage(
    //     builder.Configuration.GetConnectionString("Hangfire"));
});
```

`Hangfire.Extension` depends on the app's configured `JobStorage`.

## 3. Register The Embedded UI And Services

After your Hangfire registration, add:

```csharp
using Hangfire.Extension.AspNetCore.DependencyInjection;

builder.Services.AddHangfireExtension("/recurring-jobs");
```

This registers:

- recurring job admin services
- Hangfire-backed storage reader
- cron validation
- embedded Razor Pages
- configurable recurring jobs route mapping

## 4. Replace The Placeholder Authorization Policy

The package defaults to requiring the `RecurringJobAdmin` authorization policy.

In a real host, define a proper internal policy.

Example:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RecurringJobAdmin", policy =>
        policy.RequireRole("Admin"));
```

If you use claims or custom policies, wire that instead.

If you want a different policy name, configure it when registering the package:

```csharp
builder.Services.AddHangfireExtension("/recurring-jobs", "OperationsAdmin");
```

For demos or local experiments only, you can explicitly disable authorization:

```csharp
builder.Services.AddHangfireExtension(options =>
{
    options.RoutePrefix = "/recurring-jobs";
    options.RequireAuthorization = false;
});
```

The default remains secure: authorization is required unless you explicitly turn it off.

## 5. Map The Embedded UI

In the request pipeline, add:

```csharp
using Hangfire.Extension.AspNetCore.Routing;

app.MapHangfireExtension("/recurring-jobs");
```

This maps the packaged JSON endpoints and keeps the embedded page prefix aligned with registration.

## 6. Map Razor Pages

In the request pipeline:

```csharp
app.MapRazorPages();
```

If your app already maps Razor Pages, keep your existing mapping.

## 7. Embedded UI Route

With the configuration above, the UI is available at:

- `/recurring-jobs`

You can change the route prefix by passing the same value into both:

- `AddHangfireExtension("/your-prefix")`
- `MapHangfireExtension("/your-prefix")`

## 8. Embedded API Endpoints

The package exposes these endpoints under your configured route prefix:

- `GET /recurring-jobs/api/jobs/recurring`
- `POST /recurring-jobs/api/jobs/recurring/{id}/trigger`
- `POST /recurring-jobs/api/jobs/recurring/{id}/enable`
- `POST /recurring-jobs/api/jobs/recurring/{id}/disable`
- `PUT /recurring-jobs/api/jobs/recurring/{id}`

If you choose a different route prefix, the API moves with it.

The recurring jobs `GET` endpoint returns a paged response:

```json
{
  "items": [
    {
      "id": "nightly-report",
      "cronExpression": "0 2 * * *",
      "queue": "default",
      "jobType": "MyApp.Jobs.ReportJobs",
      "methodName": "RunNightlyReport",
      "nextExecution": "2026-04-04T02:00:00+00:00",
      "lastExecution": "2026-04-03T02:00:01+00:00",
      "lastJobId": "12345",
      "error": null,
      "isDisabled": false
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalCount": 1,
  "totalPages": 1,
  "search": "nightly",
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

## Important: Enable And Update Require A Definition Source

Viewing, triggering, and disabling can work from Hangfire storage alone.

Enabling and updating cron expressions require a code-based source of truth so the app knows how to recreate the recurring job safely.

The simplest option is to register definitions directly with the built-in registry.

## Preferred: Register Definitions Directly

Example:

```csharp
using Hangfire.Extension.AspNetCore.DependencyInjection;

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

## Advanced: Implement IRecurringJobDefinitionProvider Yourself

If you need a custom lookup source, dynamic registry, or definitions stored elsewhere, you can still implement:

- `IRecurringJobDefinitionProvider`

and register your own implementation to replace the built-in registry-backed one.

## Recommended Integration Shape

In an existing app, your `Program.cs` will usually end up looking roughly like this:

```csharp
using Hangfire;
using Hangfire.Extension.AspNetCore.DependencyInjection;
using Hangfire.Extension.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RecurringJobAdmin", policy => policy.RequireRole("Admin"));

builder.Services.AddHangfire(configuration =>
{
    configuration.UseSimpleAssemblyNameTypeSerializer();
    configuration.UseRecommendedSerializerSettings();

    // Reuse your existing Hangfire storage here.
});

builder.Services.AddHangfireExtension("/recurring-jobs");
builder.Services.AddRecurringJobDefinition<ReportJobs>(
    "nightly-report",
    jobs => jobs.RunNightlyReport(),
    "0 2 * * *",
    TimeZoneInfo.Utc,
    "default");

var app = builder.Build();

app.UseAuthorization();

app.MapHangfireExtension("/recurring-jobs");
app.MapRazorPages();

app.Run();
```

## Using The Admin UI

Once wired into your host:

1. Run the application.
2. Open `/recurring-jobs`.
3. Search or inspect recurring jobs.
4. Use `Trigger` to run a job immediately.
5. Use `Disable` to remove a recurring job from Hangfire storage.
6. Use `Enable` after a definition provider is in place.
7. Use `Edit cron` after a definition provider is in place.

## Notes And Limitations

- The current sample host intentionally uses a placeholder authorization policy.
- The recurring job pages are embedded from `Hangfire.Extension.AspNetCore`.
- `Enable` and `Update cron` are intentionally code-first.
- The extension uses public Hangfire APIs only.

## SkipConcurrentExecutionAttribute

Use `SkipConcurrentExecutionAttribute` on a job method when a second overlapping execution should be skipped instead of waiting for the first one to finish.

Example:

```csharp
using Hangfire.Extension.Core.Filters;

public sealed class ReportJobs
{
    [SkipConcurrentExecution(timeoutInSeconds: 30)]
    public Task RunNightlyReport()
    {
        // Job work here.
        return Task.CompletedTask;
    }
}
```

Behavior:

- the first execution acquires a distributed lock
- a concurrent execution that cannot acquire the lock within the timeout is canceled
- the lock is released after the job completes

This relies on your Hangfire storage provider supporting distributed locks correctly.

## Current Verification

At the time this README was added:

- the solution builds successfully
- the test project passes
- the sample UI runs

## NuGet Packaging

The packageable library is:

- `src/Hangfire.Extension.AspNetCore/Hangfire.Extension.AspNetCore.csproj`

To create a local NuGet package:

```powershell
dotnet pack .\src\Hangfire.Extension.AspNetCore\Hangfire.Extension.AspNetCore.csproj -c Release
```

The generated files will be written under:

- `src/Hangfire.Extension.AspNetCore\bin\Release`

This package contains:

- the embedded Razor Pages UI
- the route-mapping extensions
- the recurring-job services
- the concurrency-control attribute

The sample host in `src/Hangfire.Extension.Web` is not intended to be published as a NuGet package.

## Next Good Improvements

- add richer filtering and sorting options to the API and UI
- add packaging/publishing guidance for NuGet distribution
