# Project: Hangfire Recurring Job Admin Replacement

## Background

The current implementation uses `Hangfire.RecurringJobAdmin`, which has proven unstable and can crash the entire ASP.NET application. While it provides a convenient UI for managing recurring jobs, its architectural and runtime issues make it unsuitable for production use.

We will replace this dependency with a custom-built, lightweight internal solution.

---

## Objectives

Recreate (not necessarily fully replicate) the core capabilities of RecurringJobAdmin with a focus on:

- Stability (must never impact host application reliability)
- Simplicity (minimal UI and logic)
- Control (full ownership of behavior and extensibility)

---

## Core Features (MVP)

### 1. View Recurring Jobs
- List all recurring jobs
- Display:
  - Job ID
  - Method / Type
  - Cron expression
  - Next execution
  - Last execution
  - Queue

### 2. Trigger Job Manually
- Trigger execution on demand

### 3. Enable / Disable Job
- Remove job (disable)
- Recreate job (enable)

### 4. Update Cron Expression
- Edit schedule
- Validate cron input before saving

---

## Nice-to-Have Features (Phase 2)

- Search / filter jobs
- Grouping (by domain, tenant, or prefix)
- Job metadata display (custom tags)
- Execution history (basic)
- Bulk operations

---

## Non-Goals

- Full-featured job orchestration UI
- Complex workflow editing
- Replacing Hangfire Dashboard entirely

---

## Architecture

### Backend

Expose a minimal API layer:

```
GET    /api/jobs/recurring
POST   /api/jobs/recurring/{id}/trigger
POST   /api/jobs/recurring/{id}/enable
POST   /api/jobs/recurring/{id}/disable
PUT    /api/jobs/recurring/{id}
```

Use Hangfire APIs:

- `JobStorage.Current.GetConnection()`
- `RecurringJobManager`
- `RecurringJob`


### Data Handling

- Read directly from Hangfire storage
- Avoid large unbounded queries
- Add paging if job count grows

---

## Safety Requirements

- All endpoints must be wrapped in try/catch
- No unbounded queries
- Timeouts for storage calls where possible
- Never block request threads unnecessarily
- Fail gracefully (partial data > crash)

---

## UI Approach

### Option A (Recommended MVP)
- Simple Razor Pages or minimal SPA
- Table view + modal edit

### Option B
- API-only (Postman / internal tooling)

---

## Security

- Restrict access (internal network / auth)
- No public exposure
- Audit logging (optional future)

---

## Integration Notes

- Continue using Hangfire built-in dashboard for monitoring
- Replace `Hangfire.RecurringJobAdmin` entirely
- Use built-in `[DisableConcurrentExecution]` attribute for concurrency control

---

## Risks

| Risk | Mitigation |
|------|-----------|
| Breaking job definitions | Keep job registration in code as source of truth |
| Large dataset performance | Add pagination and filtering |
| Cron misconfiguration | Add validation layer |

---

## Future Extensions

- Tenant-aware job management
- Dynamic job registration UI
- Observability hooks (logs, metrics)
- Custom job tags and grouping

---

## Phase 1: Concurrency Control (Skip-if-Running)

### Goal

Implement a concurrency control mechanism that:

> Prevents multiple instances of the same job from running concurrently by **skipping new executions instead of queueing them**.

### Behavior Definition

- If a job is already running:
  - New execution attempts are **canceled immediately**
  - No waiting
  - No queue buildup
- Only one instance of a job runs at any given time

### Implementation Approach

Use Hangfire's filter extensibility system (`IServerFilter`) to intercept execution.

Hangfire supports filters via a chain-of-responsibility pipeline, allowing execution to be intercepted before a job runs ([docs.hangfire.io](https://docs.hangfire.io/en/latest/extensibility/using-job-filters.html?utm_source=chatgpt.com)).

Core mechanism:
- Attempt to acquire a distributed lock
- If lock fails → cancel execution
- If lock succeeds → proceed and release after execution

Distributed locks ensure only one worker/process holds the lock at a time across servers ([deepwiki.com](https://deepwiki.com/raisedapp/Hangfire.Storage.SQLite/4.1-distributed-locking?utm_source=chatgpt.com)).

### Core Component

- `SkipConcurrentExecutionAttribute`
  - Implements `JobFilterAttribute`, `IServerFilter`
  - Uses `context.Connection.AcquireDistributedLock(...)`
  - Sets `context.Canceled = true` when lock cannot be acquired

### Key Design Decisions

- Prefer **skip over queue** to avoid backlog and resource contention
- Use distributed locks (not in-memory) for multi-server safety
- Timeout must be greater than worst-case job execution time

### Lock Scope (Current)

- Based on: `Type + Method`

### Future Extension (Important)

Allow customizable lock keys for:
- Per-tenant locking
- Per-entity (e.g., order, invoice)

Suggested abstraction:

```
IJobLockKeyProvider
```

### Validation Checklist

- Multiple rapid triggers → only one executes
- Recurring job faster than execution → no backlog
- Multi-instance deployment → still no overlap
- Failure scenarios → lock released correctly

### Non-Goals (for this phase)

- UI / dashboard integration
- Job editing
- Monitoring visualization

---

## Extension & Compatibility Requirement

This module must be implemented as a **loosely coupled extension** to Hangfire:

### Goals
- Survive upgrades of `Hangfire.Core` and `Hangfire.AspNetCore` with minimal or no changes
- Avoid reliance on internal or undocumented APIs
- Keep integration surface small and stable

### Approach
- Use only **public Hangfire APIs**:
  - `RecurringJobManager`
  - `RecurringJob`
  - `JobStorage.Current`
  - `IStorageConnection`
- Avoid direct SQL queries into Hangfire tables
- Avoid reflection or internal type access

### Plugin / Extension Strategy
- Leverage Hangfire filter interfaces where applicable (e.g., `IServerFilter`, `IClientFilter`)
- Keep all logic in a separate project/module (e.g., `YourApp.HangfireExtensions`)
- Use dependency injection instead of static coupling where possible

### Isolation Principles
- No modification of Hangfire source
- No monkey patching
- No UI middleware that can interfere with Hangfire pipeline

### Version Tolerance
- Wrap Hangfire calls in thin adapter/services
- Centralize all Hangfire interactions in one layer
- This allows future changes to be handled in one place

---

## Guiding Principle

> Keep it simple, safe, and under full control.

This tool is an internal admin utility, not a product. Stability and predictability are more important than feature completeness.

