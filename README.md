# simple-blog-api

ASP.NET Core Web API for a simple blog with admin authentication and PostgreSQL storage.

## Features
- JWT-based admin auth
- Article CRUD
- Draft / Published status
- Slug-based public article routes
- PostgreSQL via Entity Framework Core
- Docker-ready

## Run locally

### With Docker
```bash
docker build -t simple-blog-api .
```

Usually this API is started together with the frontend and PostgreSQL via a shared `docker-compose.yml` from the parent workspace.

### Without Docker
Requires .NET 8 SDK and PostgreSQL.

```bash
dotnet restore
dotnet run
```

## Environment / config
Use `appsettings.json` as base config.
Override these values for production:
- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `Admin:Email`
- `Admin:Password`

## Main endpoints
- `POST /api/auth/login`
- `GET /api/articles`
- `GET /api/articles/{slug}`
- `GET /api/admin/articles`
- `POST /api/admin/articles`
- `PUT /api/admin/articles/{id}`
- `DELETE /api/admin/articles/{id}`

## Notes
- First admin user is seeded automatically from config.
- On startup the app runs `Database.Migrate()`.
- You still need to create and commit the first EF migration once .NET SDK is available.
