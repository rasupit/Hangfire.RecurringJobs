# Global Conventions

## Architecture
- Prefer clean architecture for backend systems
- Separate API, business logic, and data access
- Prefer separation by class, folder, and namespace before introducing separate projects or DLLs
- Avoid physical project separation unless there is a clear benefit such as independent deployment, packaging, ownership, or incompatible dependencies
- For small to medium solutions, keep runtime assemblies minimal and preserve layering logically inside the same project when practical

## Database
- PostgreSQL preferred for big projects
- SQL Server Express for small-medium projects
- SQLite if just need a place to store
- Use migrations; prefer .sql when sql is the native up/down task language  
- Avoid destructive changes

## API Design
- RESTful conventions with OpenAPI
- Consistent response structure
- Proper error handling
- Use tool (Swagger) for API documentation, development, testing and debugging 

## Workers
- Must be idempotent
- Must include logging
- Must support retries
- Hangfire is preferred

## Docker
- docker is the preferred implementation platform
- Use docker-compose is preferred
- Keep services isolated
- map volumes to local sub-folder for data, configurations, logs
- use .env for secrets, .env.example for sample env
