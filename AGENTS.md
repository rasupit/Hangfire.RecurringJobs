# AGENTS.md

## PROJECT CONTEXT

This project replaces `Hangfire.RecurringJobAdmin` with a **lightweight, stable internal admin tool**.

Core characteristics:
- Internal-only tool (not public-facing)
- Minimal UI + minimal API
- Stability > features
- Direct interaction with Hangfire public APIs only

---

## GLOBAL CONVENTIONS

Follow shared conventions defined in:
conventions.md

---

## CRITICAL RULES (STRICT)

- Do NOT introduce additional projects or DLLs
- Keep everything in a **single project**
- Do NOT over-engineer abstractions
- Avoid interfaces unless there is a clear need
- Prefer simple, readable code over extensibility

- Do NOT rewrite entire files unless necessary
- Prefer incremental, minimal changes
- Preserve existing structure and patterns

---

## ARCHITECTURE RULES (IMPORTANT)

- Keep layering **logical, not physical**
  - API
  - Services (business logic)
  - Hangfire integration layer

- Follow folder structure instead of creating new assemblies

- All Hangfire interactions must be centralized:
  - Do NOT scatter `JobStorage.Current` usage everywhere
  - Use a small service layer

---

## API RULES

- Keep endpoints minimal and focused
- Always wrap logic in try/catch
- Return safe, consistent responses
- Do NOT expose internal errors

---

## HANGFIRE RULES

- Use only **public APIs**
- Do NOT query Hangfire tables directly
- Do NOT use reflection or internal types

- Concurrency control:
  - Use `SkipConcurrentExecutionAttribute`
  - MUST skip execution (not queue)

---

## PERFORMANCE & SAFETY

- No unbounded queries
- Add pagination if needed
- Never block threads unnecessarily
- Fail gracefully (return partial data if needed)

---

## UI RULES (if implemented)

- Keep UI extremely simple
- No heavy frameworks
- Table + basic actions is enough

---

## AGENT BEHAVIOR

When implementing:
- Do not assume missing requirements
- Ask if behavior is unclear
- Prefer simplest working solution first
- Avoid “future-proofing” unless asked

---

## OUTPUT STYLE

- Show only relevant code
- Prefer partial snippets over full files
- Keep explanations short and practical