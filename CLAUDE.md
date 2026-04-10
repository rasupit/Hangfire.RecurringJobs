# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Restore
dotnet restore Hangfire.RecurringJobs.slnx

# Build
dotnet build Hangfire.RecurringJobs.slnx --configuration Release

# Run all tests
dotnet test tests/Hangfire.RecurringJobs.Tests/Hangfire.RecurringJobs.Tests.csproj --configuration Release

# Run a single test class
dotnet test tests/Hangfire.RecurringJobs.Tests/Hangfire.RecurringJobs.Tests.csproj --filter "FullyQualifiedName~RecurringJobAdminServiceTests"

# Pack the NuGet package
dotnet pack src/Hangfire.RecurringJobs/Hangfire.RecurringJobs.csproj --configuration Release
```

Target framework: `net10.0`. Package published as `Rasupit.Hangfire.RecurringJobs` on NuGet. Consumer namespace: `Hangfire.RecurringJobs`.

## Architecture

Single library project (`src/Hangfire.RecurringJobs`). No separate runtime assemblies. All layering is logical — folders and namespaces only.

**Key layers (all inside the one project):**

| Folder | Role |
|--------|------|
| `DependencyInjection/` | `AddHangfireRecurringJobs(...)` and `AddRecurringJobDefinition(...)` extension methods; Razor Pages conventions setup |
| `Options/` | `RecurringJobsOptions` — route prefix, auth policy, style list. One source of truth for all derived routes |
| `Hangfire/` | `RecurringJobStorage` — the only place that calls Hangfire storage APIs. Public Hangfire APIs only, no reflection or direct table access |
| `Services/` | `RecurringJobAdminService` (orchestrates all operations), `CronExpressionPreviewService`, `CronExpressionValidator`, style/script providers |
| `Models/` | Plain records: `RecurringJobSummary`, `RecurringJobPage`, `RecurringJobDefinition`, `RecurringJobQuery`, etc. |
| `Routing/` | `MapHangfireRecurringJobsApi()` — maps all JSON minimal API endpoints |
| `Pages/RecurringJobs/` | Razor Pages UI: `Index` (list + popup cron editor + toggle), `Edit` (full edit page fallback) |
| `Filters/` | `SkipConcurrentExecutionAttribute` — skip-on-overlap behavior using distributed lock |
| `wwwroot/hangfire-extension/` | Static assets served under `/hangfire-extension/...` to avoid host collisions |

**Data flow:** `RecurringJobAdminService` is the single orchestration point. It merges `IEnumerable<RecurringJobDefinition>` (registered in DI) with live data from `RecurringJobStorage`. Jobs without a code-based definition are treated as orphaned and are not shown. All Hangfire storage access is isolated in `RecurringJobStorage`.

**Route prefix:** Configured once in `AddHangfireRecurringJobs(routePrefix)`. Derived properties on `RecurringJobsOptions` (`RoutePrefixNormalized`, `ApiRoutePrefix`) produce all other routes automatically. Never require the consumer to repeat the same prefix.

**Styles:** When `RecurringJobsOptions.Styles` is empty, the library inlines its embedded CSS (Bootstrap + scoped styles) via `RecurringJobsStyleProvider`. When styles are provided, the host's stylesheets are used instead and embedded CSS is still applied as a base layer. JS is always inlined from `RecurringJobsScriptProvider`.

**Enable/update operations** recreate the recurring job from the registered `RecurringJobDefinition`. There is no fallback for missing definitions — failure is explicit.

## Conventions

See `conventions.md` and `AGENTS.md` for the full rule set. Key points:

- No unnecessary interfaces, abstractions, or additional projects
- Public Hangfire APIs only — no internal types, no reflection, no direct table queries
- Prefer simple, readable, incremental changes over rewrites
- The recurring jobs list is definition-driven — jobs not in `IEnumerable<RecurringJobDefinition>` are never shown
- `SkipConcurrentExecutionAttribute` must skip (cancel) concurrent executions, not queue behind a lock
- Route prefix is configured once; if `MapHangfireRecurringJobsApi()` is called with a different prefix than registration, it throws immediately

## Design Inspiration

The embedded UI targets Bootstrap (standard classes like `d-flex`, `btn`, `table`, `badge`, `card`, etc.). The visual design reference for any UI enhancements is documented in `DESIGN.md` (Notion-inspired design system with warm neutrals, whisper borders, and multi-layer shadows).

## Tests

Tests are in `tests/Hangfire.RecurringJobs.Tests/` using xUnit + NSubstitute. Integration tests use `Hangfire.Storage.SQLite` as the in-process storage and `Microsoft.AspNetCore.Mvc.Testing` for host integration.

Test files map to the area they cover:
- `RecurringJobAdminServiceTests` — service logic, definition-backed filtering
- `RecurringJobHostIntegrationTests` — full host integration via `WebApplicationFactory`
- `RecurringJobIntegrationTests` — storage integration
- `RecurringJobPageTests` — page model behavior
- `SkipConcurrentExecutionAttributeTests` — concurrency filter behavior
