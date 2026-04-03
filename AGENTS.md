# AGENTS.md

## PROJECT CONTEXT

This project is now a **single library** named `Hangfire.Extension`.

It replaces `Hangfire.RecurringJobAdmin` with a lightweight recurring jobs admin surface for ASP.NET Core applications that already use Hangfire.

Core characteristics:
- Internal/admin usage
- Minimal UI + minimal API
- Stability > features
- Public Hangfire APIs only
- Single consumer namespace: `Hangfire.Extension`

---

## GLOBAL CONVENTIONS

Follow shared conventions defined in:
`conventions.md`

When there is a library-specific decision to make, prefer the library rules in this file.

---

## CRITICAL RULES (STRICT)

- Do NOT introduce additional runtime projects or DLLs
- Keep the implementation in a **single library project**
- Do NOT reintroduce a separate sample-host architecture into the runtime library
- Do NOT over-engineer abstractions
- Avoid interfaces unless there is a clear, current need
- Prefer simple, readable code over extensibility

- Do NOT rewrite entire files unless necessary
- Prefer incremental, minimal changes
- Preserve established structure and patterns unless they are part of a deliberate cleanup

---

## LIBRARY DESIGN RULES

- Optimize for the **consumer experience** first
- Public entry points should be easy to discover and hard to misuse
- Configure important values once and derive the rest internally
- Do NOT require consumers to repeat the same route, policy, or feature settings in multiple places
- Hide internal layering from consumers

Prefer:
- one root namespace for user-facing types
- small, intention-revealing extension methods
- canonical routes generated from one source of truth

Avoid:
- multiple namespaces just to consume one feature
- duplicate registration and mapping strings
- public APIs that expose internal folder structure
- compatibility shims unless explicitly requested

---

## ARCHITECTURE RULES

Keep layering logical, not physical:
- registration/options
- Razor Pages UI
- minimal API endpoints
- services
- Hangfire integration

Follow folders and namespaces instead of separate assemblies.

All Hangfire interactions must stay centralized:
- do NOT scatter storage access across pages/endpoints
- keep Hangfire operations inside a small service/integration layer

---

## HOST INTEGRATION RULES

Assume the host application already owns:
- Hangfire storage configuration
- authentication/authorization
- application startup shape

The library should plug into that host with minimal friction.

Prefer this integration model:
- `builder.Services.AddHangfireRecurringJobs(...)`
- `app.MapHangfireRecurringJobsApi()`
- `app.MapRazorPages()`

The route prefix must be configured once and reused internally.

If a design asks the consumer to keep two strings in sync for one feature, treat that as a design bug.

---

## API RULES

- Keep endpoints minimal and focused
- Always wrap endpoint logic in safe error handling
- Return safe, consistent responses
- Do NOT expose internal exception details
- Prefer stable routes over clever routing tricks

---

## RAZOR PAGES RULES

- Keep the UI simple and page-centered
- Prefer normal Razor Pages over areas when areas add noise without clear value
- Keep canonical UI paths stable and user-friendly
- Navigation should stay under the configured base route
- Back/cancel flows should preserve relevant user context when practical

---

## HANGFIRE RULES

- Use only **public APIs**
- Do NOT query Hangfire tables directly
- Do NOT use reflection or internal Hangfire types

Concurrency control:
- Use `SkipConcurrentExecutionAttribute`
- It must skip execution, not queue behind a lock

Enable/update behavior:
- Recreate jobs only from an explicit code-based definition source
- Do not invent fallback behavior that hides missing definitions

---

## PERFORMANCE & SAFETY

- No unbounded queries
- Add pagination where appropriate
- Never block threads unnecessarily
- Fail safely and predictably
- Do not disguise infrastructure failures as normal business data

---

## DOCUMENTATION RULES

- Keep `README.md` aligned with the actual shipped library shape
- Document the current package name, namespace, and integration pattern
- Remove stale references when renaming public types or packages
- Release docs should match the assets actually published

---

## AGENT BEHAVIOR

When implementing:
- do not assume missing requirements
- prefer the simplest working solution first
- avoid future-proofing unless asked
- favor cleanup when the current shape is actively confusing

When reviewing design:
- prioritize consumer ergonomics, stability, and clarity
- call out duplicate configuration, leaky abstractions, and naming drift

---

## OUTPUT STYLE

- Show only relevant code
- Prefer partial snippets over full files
- Keep explanations short and practical
