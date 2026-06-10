# Hangfire.RecurringJobs — Issue / Investigation List

Captured 2026-06-10. Source inspected at HEAD `e9af234` — which **is** the `v0.2.0`
release commit, i.e. the same version currently consumed by Nd6Automation
(`Rasupit.Hangfire.RecurringJobs` 0.2.0). So these notes describe the released code,
not an unreleased branch.

---

## Issue 1 — A job that starts **disabled** never shows in the list (can't be enabled from the UI)

**Observed:** When a recurring job is initially disabled, it does not appear in the
`/{prefix}` list, so it can't be enabled from the UI. Only a job that started *enabled*
can be toggled enable ↔ disable.

**Verified during triage (so we don't re-investigate dead ends):**

- The JSON API `GET {prefix}/api/jobs/recurring` and the Razor page use the **same**
  service path: `RecurringJobAdminService.GetJobsAsync` → `GetDefinitionBackedJobsAsync`.
  That method enumerates the registered **code definitions** (`definitions.Values`) and,
  for any definition not present in Hangfire storage, emits `CreateDisabledSummary(...)`
  with `IsDisabled = true`. A *definition-backed* disabled job is therefore expected to be
  returned. (`Services/RecurringJobAdminService.cs`)
- **Confirmed empirically.** A host that registers two definitions with
  `AutoRegisterOnStartup = false` returns, from the live API, **both jobs with
  `isDisabled: true, nextExecution: null`**. The server side returns disabled,
  definition-backed jobs correctly — the bug is **not** in the API or the service.
- The client script `wwwroot/hangfire-extension/js/recurring-jobs.js` does **not build the
  table from the API**. It only *enhances* server-rendered rows: `refreshRow(id)` fetches
  `…/{id}` and calls `updateRow` **only if a matching `[data-hfext-job-row]` already
  exists** (`if (row) { … }`, ~line 260). There is no code path that creates or removes
  `<tr>` rows, and no init-time list fetch — toggle/trigger handlers also end in
  `refreshRow`, never row insertion. So the rendered list is entirely whatever the server
  emitted on page load.

**Leading hypotheses (ranked, to confirm):**

1. **No code definition in the tested environment (most likely).** The list is strictly
   *definition-backed*: a job with no `AddRecurringJobDefinition` is invisible whether
   enabled or disabled — it is never in `definitions.Values`, and storage-only jobs are not
   enumerated. If the job under test had no definition in that environment, it could not
   appear; the "initially enabled, can toggle" cases were the ones that *did* have a
   definition. (In Nd6Automation the integrity jobs were `Production`-gated, so in non-prod
   they had no definition — now changed to register in all environments.)
2. **`refreshRow` cannot materialise a row.** Since the script never inserts rows, any flow
   that expects a row to *appear* after a state change (rather than already being
   server-rendered) silently no-ops. Decide whether the client should re-render the table /
   insert a row after enable, instead of relying solely on the server render.
3. **Packaging mismatch.** Confirm the consumed `0.2.0` binary was actually built from this
   commit (vs. an earlier build whose listing may have been storage-only).

**Suggested next steps:**

- Reproduce in a real browser against the Razor page with a *registered-but-disabled*
  definition; capture the rendered DOM and the `/api/jobs/recurring` payload side by side.
  Server data is already proven correct, so this isolates the render layer.
- Add an endpoint/service test asserting `GetJobsAsync` returns a definition-backed job
  with `IsDisabled = true` when storage is empty — locks in the server contract.
- Decide and document intended behaviour for storage-only jobs that have no definition
  (show read-only? hide?).

---

## Issue 2 — "Disable" removes from storage, so it does not survive a restart when `AutoRegisterOnStartup = true`

`DisableAsync` calls `recurringJobManager.RemoveIfExists(id)`; `EnableAsync` calls
`AddOrUpdate(...)` from the definition. "Disabled" therefore means *absent from Hangfire
storage*. But `RecurringJobRegistrationHostedService.StartAsync` re-`AddOrUpdate`s **every**
definition on startup when `AutoRegisterOnStartup = true`. Net effect: a job disabled via
the UI is **silently re-enabled on the next restart / redeploy**.

Consider a persisted "disabled" flag, or have auto-register skip operator-disabled jobs. At
minimum, document that UI-disable does not survive a restart under `AutoRegisterOnStartup`.

---

## Issue 3 — Registration runs in `IHostedService.StartAsync` (timing note)

`AutoRegisterOnStartup` registration happens inside
`RecurringJobRegistrationHostedService.StartAsync`, i.e. during host start — *after* any
consumer code that runs before `app.Run()`. Consumers trying to reconcile/disable jobs in
pre-`Run` startup code will execute *before* registration and see nothing in storage. (This
is exactly why Nd6Automation gates scheduling via `AutoRegisterOnStartup = IsProduction()`
rather than via a pre-`Run` purge.) Worth a short note in the README.
