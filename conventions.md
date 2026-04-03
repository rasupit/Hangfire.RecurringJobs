# Global Conventions

## Core Principles

- Prefer simplicity over architectural purity
- Optimize for readability and maintainability
- Avoid over-engineering
- Design for current requirements, not hypothetical future scale

---

## Architecture

- Prefer pragmatic architecture over strict clean architecture
- Start with a single project by default

- Separate API, business logic, and data access logically:
  - use folders and namespaces
  - avoid physical separation (projects/DLLs) unless necessary

- Prefer separation by class, folder, and namespace before introducing new projects

- Only introduce separate projects or DLLs if ALL are true:
  - clear and immediate benefit
  - reduces complexity (not increases it)
  - real boundary exists (deployment, ownership, or incompatible dependencies)

- For small to medium solutions:
  - keep runtime assemblies minimal
  - preserve layering logically within the same project
  - avoid unnecessary abstraction layers

- Keep namespace structure aligned with folder structure

---

## Anti-Overengineering Rules (Critical)

- Do NOT create additional projects or DLLs without clear justification
- Do NOT introduce patterns unless they solve a real problem

Avoid:
- unnecessary interfaces
- excessive abstraction
- deep folder hierarchies
- splitting code across too many files
- designing for future scale prematurely

Prefer:
- simple, direct, readable code
- minimal layers
- fewer moving parts

When unsure:
→ choose the simplest working solution

---

## Database

- PostgreSQL preferred for large projects
- SQL Server Express for small to medium projects
- SQLite for lightweight or embedded use cases

- Use migrations:
  - prefer `.sql` when SQL is the natural up/down language

- Avoid destructive changes unless explicitly required

- Do NOT:
  - introduce unnecessary abstraction over data access
  - overcomplicate schema design early

---

## API Design

- Follow RESTful conventions with OpenAPI
- Maintain consistent response structure
- Implement proper error handling

- Use Swagger for:
  - documentation
  - development
  - testing
  - debugging

- Keep endpoints simple and predictable
- Avoid unnecessary layers between controller and logic

---

## Workers

- Workers must be idempotent
- Must include logging for key steps
- Must support retries

- Keep processing logic:
  - simple
  - traceable
  - easy to debug

- Hangfire is preferred where applicable

---

## Docker

- Docker is the preferred platform
- Use docker-compose for local development

- Keep services:
  - isolated
  - simple to run and understand

- Map volumes to local subfolders for:
  - data
  - configurations
  - logs

- Use:
  - `.env` for secrets
  - `.env.example` for templates

- Avoid:
  - unnecessary service splitting
  - overly complex networking setups

---

## Decision Rule

Before introducing complexity, ask:

1. Is this solving a real problem now?
2. Does this make the code easier to understand?
3. Would a simpler approach work?

If unsure:
→ choose the simpler approach