# Global Conventions

## Core Principles

- Prefer simplicity over architectural purity
- Optimize for readability, maintainability, and consumer ergonomics
- Avoid over-engineering
- Design for current requirements, not hypothetical future scale
- A clean public API is part of code quality

---

## Architecture

- Prefer pragmatic architecture over strict clean architecture
- Start with a single project by default
- Keep layering logical within the same project unless a real boundary exists

Separate concerns logically with:
- folders
- namespaces
- focused classes

Avoid physical separation into multiple projects unless there is a clear immediate payoff.

Only introduce separate projects or DLLs if all are true:
- there is a real boundary
- it reduces complexity
- it improves the current system, not a hypothetical future one

---

## Public API Design

- Design public APIs for the consumer, not the internal folder structure
- Prefer one obvious way to use the library
- Configure a concept once and derive related behavior internally
- Make misuse difficult

Prefer:
- root-level namespaces for user-facing types
- small, intention-revealing extension methods
- minimal required `using` directives
- names that describe the feature, not the implementation history

Avoid:
- forcing consumers to repeat the same setting twice
- exposing internal layers through multiple public namespaces
- naming that reflects old project layout instead of the current design

---

## Anti-Overengineering Rules

- Do NOT create additional projects or DLLs without clear justification
- Do NOT introduce patterns unless they solve a real problem now

Avoid:
- unnecessary interfaces
- excessive abstraction
- deep folder hierarchies
- duplicate registration flows
- compatibility wrappers that keep bad APIs alive without a good reason
- designing for future scale prematurely

Prefer:
- simple, direct, readable code
- minimal layers
- fewer moving parts
- one source of truth for important configuration

When unsure:
choose the simpler approach

---

## ASP.NET Core Library Guidance

- Respect the host application's startup and configuration
- Keep host integration minimal and explicit
- Prefer conventional ASP.NET Core patterns over custom framework behavior

For Razor Pages based library UI:
- keep routes canonical and stable
- keep navigation intuitive
- avoid internal route leakage
- avoid areas unless they provide a real benefit

For endpoint mapping:
- route configuration should come from one place
- endpoint registration should not require consumers to restate the same route prefix

For static assets:
- include only the assets the library actually needs
- keep asset mapping/documentation aligned with the real host integration shape

---

## Hangfire Guidance

- Use public Hangfire APIs only
- Centralize Hangfire access in a small integration layer
- Treat recurring job definitions as explicit source-of-truth data
- Avoid behavior that hides missing definitions or infrastructure failures

Workers and jobs should be:
- idempotent where practical
- easy to trace
- easy to debug

Concurrency behavior should be explicit and predictable.

---

## API Design

- Keep endpoints simple and predictable
- Maintain consistent response shapes
- Handle errors safely
- Do not expose internal exception details
- Prefer stable routes and stable semantics

---

## Naming

- Names should match current purpose, not project history
- Prefer feature-centered naming over implementation-centered naming
- Keep namespaces aligned with folders unless that harms the consumer API

When renaming:
- update docs
- update release metadata
- update package identity if needed
- remove stale names instead of keeping the old and new shapes side-by-side indefinitely

---

## Documentation

- Documentation is part of the product surface
- Keep `README.md`, release notes, package metadata, and code examples in sync
- Example code should reflect the recommended integration path
- Remove stale examples quickly after refactors

---

## Decision Rule

Before introducing complexity, ask:

1. Is this solving a real problem now?
2. Does this make the code easier to understand?
3. Does this make the library easier to consume?
4. Would a simpler approach work?

If unsure:
choose the simpler approach
