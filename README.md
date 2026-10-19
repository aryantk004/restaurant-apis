# RestaurantManagement API

A RESTful restaurant-management backend built with **C#**, **ASP.NET Core 10**, **Entity Framework Core 10**, **Microsoft SQL Server**, and **Redis**. The application supports restaurant CRUD operations, soft deletion, paginated retrieval, cache-aside caching, request validation, IP-based rate limiting, centralized exception handling, and structured logging.

## Tech stack

| Technology | Purpose |
| --- | --- |
| C# / ASP.NET Core 10 | REST API and dependency injection |
| Entity Framework Core 10 | SQL Server data access and migrations |
| Microsoft SQL Server | Persistent relational storage |
| Redis / StackExchange.Redis | Cache-aside caching for single-restaurant reads |
| Docker | Local SQL Server and Redis instances |
| `ILogger<T>` | Structured application and error logging |

## Solution structure

```text
RestaurantManagement/
├── RestaurantManagement.Api/
│   ├── Controllers/              # HTTP endpoints
│   ├── Exceptions/               # GlobalExceptionHandler
│   ├── Program.cs                # Dependency injection and middleware
│   └── appsettings.json
├── RestaurantManagement.Application/
│   ├── DTOs/                     # API request/response contracts
│   ├── Interfaces/               # Service, repository, cache abstractions
│   └── Services/                 # RestaurantService
├── RestaurantManagement.Domain/
│   └── Entities/                 # Restaurant entity
└── RestaurantManagement.Infrastructure/
    ├── Caching/                  # RedisRestaurantCache
    ├── Persistence/              # AppDbContext and configurations
    ├── Repositories/             # SqlRestaurantRepository
    └── Migrations/               # EF Core schema migration
```

**Request flow:** `RestaurantsController` → `IRestaurantService` / `RestaurantService` → `IRestaurantRepository` / `SqlRestaurantRepository` → SQL Server. Individual `GET` requests also use `IRestaurantCache` / `RedisRestaurantCache`.

## Features

- **RESTful CRUD:** Create, get, update, and delete restaurants.
- **Soft deletion:** DELETE marks records as deleted instead of removing them; an EF Core global query filter excludes them from normal queries.
- **Pagination:** `GET /api/restaurants?page=1&pageSize=10`, with a default page size of 10 and a maximum of 100.
- **Caching:** `GET /api/restaurants/{id}` checks Redis first, falls back to SQL Server, and caches the response for **5 minutes**. Successful updates and deletes attempt cache invalidation. The paginated list endpoint is not cached.
- **Validation:** Data-annotation validation on request DTOs plus database check constraints for nonblank name/city/area and delivery times from 1 to 180 minutes.
- **Rate limiting:** Fixed-window, **5 requests per minute per client IP**, with HTTP 429 for requests exceeding the limit.
- **Error handling:** A global exception handler returns standardized error responses.
- **Logging:** Structured logs for HTTP method/path/status/duration, CRUD events, cache activity, and exceptions.

## API endpoints

Base URL for the local HTTP launch profile: `http://localhost:5078`

| Method | Endpoint | Description | Successful response |
| --- | --- | --- | --- |
| `GET` | `/api/restaurants?page=1&pageSize=10` | List restaurants with pagination | `200 OK` |
| `GET` | `/api/restaurants/{id}` | Get an individual restaurant (Redis cache-aside) | `200 OK` |
| `POST` | `/api/restaurants` | Create a restaurant | `201 Created` |
| `PUT` | `/api/restaurants/{id}` | Update an existing restaurant | `200 OK` |
| `DELETE` | `/api/restaurants/{id}` | Soft-delete a restaurant | `204 No Content` |

Individual GET, PUT, and DELETE requests return `404 Not Found` if the restaurant does not exist or has been soft-deleted. Invalid request DTOs are rejected with HTTP 400. Rate-limited requests receive HTTP 429.

### Create a restaurant

**Request:** `POST /api/restaurants`

```json
{
  "name": "Spice Garden",
  "city": "Bangalore",
  "area": "Indiranagar",
  "cuisine": "Indian",
  "deliveryTimeMinutes": 35,
  "isActive": true
}
```

A successful response contains the created restaurant, its generated ID, and HTTP `201 Created` with a `Location` header.

### Update a restaurant

**Request:** `PUT /api/restaurants/1`

```json
{
  "name": "Spice Garden",
  "city": "Bangalore",
  "area": "Indiranagar",
  "cuisine": "North Indian",
  "deliveryTimeMinutes": 30,
  "isActive": true
}
```

### Paginated response

**Request:** `GET /api/restaurants?page=1&pageSize=10`

```json
{
  "items": [],
  "page": 1,
  "pageSize": 10,
  "totalCount": 0,
  "totalPages": 0
}
```

This example shows an empty database. When records exist, `items` contains restaurant response objects.

## Local setup

### Prerequisites

- .NET 10 SDK
- Docker Desktop (running)
- EF Core CLI (`dotnet-ef`)
- A SQL client such as SQL Server Management Studio (optional)

Install or update the EF CLI if required:

```powershell
dotnet tool install --global dotnet-ef --version 10.*
```

If `dotnet-ef` is already installed, use `dotnet tool update --global dotnet-ef --version 10.*` instead.

### 1. Start SQL Server and Redis

Example Docker containers:

```powershell
# Replace <STRONG_SQL_PASSWORD> with your own password.
docker run -d --name restaurant-sql `
  -e "ACCEPT_EULA=Y" `
  -e "MSSQL_SA_PASSWORD=<STRONG_SQL_PASSWORD>" `
  -p 127.0.0.1:1433:1433 `
  mcr.microsoft.com/mssql/server:2025-latest

docker run -d --name restaurant-redis `
  -p 127.0.0.1:6379:6379 `
  redis:latest
```

If the containers already exist, start them instead:

```powershell
docker start restaurant-sql restaurant-redis
```

These commands are intended for local development. Do not use sample credentials or publicly expose SQL Server or Redis.

### 2. Configure connection strings

The application reads connection strings named `DefaultConnection` and `Redis`. Keep the SQL password **out of tracked configuration files**. From the solution root, configure .NET User Secrets for the API project:

```powershell
dotnet user-secrets init --project RestaurantManagement.Api

dotnet user-secrets set "ConnectionStrings:DefaultConnection" `
  "Server=127.0.0.1,1433;Database=RestaurantManagementDb;User Id=sa;Password=<STRONG_SQL_PASSWORD>;Encrypt=True;TrustServerCertificate=True;" `
  --project RestaurantManagement.Api

dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379" `
  --project RestaurantManagement.Api
```

Replace `<STRONG_SQL_PASSWORD>` with the password used for your local SQL Server container. User Secrets are for **development**, not a production secret store. In production, supply secrets through a secure provider or environment configuration.

### 3. Restore, migrate and run

Run these commands from the solution root:

```powershell
dotnet restore

dotnet ef database update `
  --project RestaurantManagement.Infrastructure `
  --startup-project RestaurantManagement.Api

dotnet run --project RestaurantManagement.Api --launch-profile http
```

The consolidated `InitialCreate` migration creates the `Restaurants` table and its constraints. With the local HTTP launch profile used during development, the API listens at `http://localhost:5078`.

## Database model

The `Restaurants` table contains:

| Column | Description |
| --- | --- |
| `Id` | Auto-generated `BIGINT` primary key |
| `Name` | Required, up to 150 characters |
| `City`, `Area` | Required, up to 100 characters each |
| `Cuisine` | Optional, up to 100 characters |
| `DeliveryTimeMinutes` | Integer from 1 to 180 |
| `IsActive` | Whether a restaurant is active |
| `IsDeleted` | Soft-deletion flag |
| `CreatedAtUtc` | Creation timestamp |
| `UpdatedAtUtc` | Optional last-update timestamp |

SQL Server check constraints prevent blank name/city/area values containing only ordinary spaces and reject delivery times outside the allowed range. The primary-key index supports lookups by ID and ordered pagination.

## Redis caching strategy

The service uses **cache-aside** caching for individual restaurant lookups:

1. GET by ID checks Redis with key `restaurant:{id}`.
2. A cache hit returns the deserialized response.
3. A cache miss queries SQL Server and writes the response to Redis with a five-minute TTL.
4. A successful UPDATE or soft DELETE saves to SQL Server, then attempts to remove the cached key.
5. A Redis failure is logged as a warning; the application continues to use SQL Server where possible.

**Consistency note:** Cache invalidation is best-effort. A failed invalidation or concurrent requests may temporarily leave stale data in Redis until the cached entry expires. The implementation does not provide strict consistency across Redis and SQL Server.

## Logging and error handling

The application uses the built-in `ILogger<T>` abstraction with structured message templates. It records:

- HTTP method, path, status code, elapsed time, and trace ID.
- Successful create, update, and soft-delete operations.
- Cache hits, misses, population, and invalidation (Debug level).
- Redis failures (Warning level) and unhandled exceptions (Error level).

Logging levels are configured in `appsettings.json`. Do not log passwords, connection strings, access tokens, or full sensitive request bodies.

## Scope

This project is a backend technical exercise. It does not implement authentication/authorization, distributed rate-limit counters, automated tests, health-check endpoints, or a production deployment pipeline. The rate limiter operates in-memory per API instance and identifies clients by IP address.

## License

No license has been specified. Add a `LICENSE` file if you intend to grant others permission to use or redistribute the code.
