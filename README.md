# HexShield — Learning Management System (LMS)

**A multi-tenant Learning Management System built with ASP.NET Core, Blazor WebAssembly, Entity Framework Core, and SQL Server.**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/Language-C%23-239120)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Blazor](https://img.shields.io/badge/UI-Blazor-512BD4)](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
[![Database](https://img.shields.io/badge/Database-SQL%20Server-CC2927)](https://www.microsoft.com/en-us/sql-server)
[![CI/CD](https://img.shields.io/badge/CI%2FCD-Configured-blue)](#continuous-integration-and-deployment)

## Overview

HexShield is a role-based, multi-tenant Learning Management System designed to support educational organizations, administrators, teachers, and students through a centralized learning platform.

The application uses a modular .NET architecture with a server-side ASP.NET Core backend, a Blazor WebAssembly client, shared models and DTOs, REST APIs, and a relational database.

Its architecture supports tenant isolation, role-based authorization, academic course management, and structured learning workflows.

## Key Features

### Authentication and Authorization
- User registration and login.
- JWT-based authentication.
- Token storage and authorization headers for API requests.
- Role-based access control using ASP.NET Core Identity.
- Separate access policies for administrators, teachers, and students.
- Protected routes and API endpoints.

### Multi-Tenant Architecture
- Tenant-aware data access.
- Organization hierarchy and organizational node management.
- Tenant-specific users, courses, and academic resources.
- Tenant context resolution and isolation.

### Administration
- Administrative dashboard.
- User management.
- Organization management.
- Course management and publication controls.
- Role-based access to administrative functionality.

### Course Management
- Create, view, update, and delete courses.
- Course codes, titles, and descriptions.
- Primary teacher assignment.
- Organization assignment.
- Course publication and draft status.
- Course management for authorized administrators and teachers.

### Teaching and Learning
- Lesson management.
- Student enrollment.
- Assignment creation and submission workflows.
- Grading and assessment workflows.
- Quiz management and attempts.
- Student progress tracking.

### Security and Data Management
- ASP.NET Core Identity integration.
- JWT bearer authentication.
- Server-side authorization.
- Entity Framework Core data access.
- Auditing fields and soft-delete support where configured.
- Optimistic concurrency support where configured.

> Feature availability depends on the corresponding backend endpoints, authorization policies, and frontend implementations being enabled and connected.

## Technology Stack

| Area | Technologies |
|---|---|
| Language | C# |
| Framework | .NET 10 |
| Frontend | Blazor WebAssembly |
| Backend | ASP.NET Core Web API |
| Authentication | ASP.NET Core Identity, JWT Bearer |
| ORM | Entity Framework Core |
| Database | Microsoft SQL Server |
| API communication | REST APIs, `HttpClient` |
| Client-side storage | Blazored.LocalStorage |
| UI styling | Bootstrap, custom CSS |
| API testing | Swagger/OpenAPI, Postman |
| Version control | Git, GitHub |
| CI/CD | Repository-configured build and deployment pipeline |

## Architecture

HexShield separates its application into three primary projects.

```text
HexShield/
├── HexShield/                 # ASP.NET Core server
│   ├── Controllers/           # API endpoints
│   ├── Data/                  # ApplicationDbContext
│   ├── Infrastructure/        # Tenancy, middleware, exceptions
│   ├── Models/                # Domain entities and server models
│   ├── Services/               # Business logic
│   ├── Migrations/            # EF Core migrations
│   ├── Components/            # Server-hosted Razor components
│   ├── wwwroot/               # Server static assets
│   ├── Program.cs
│   └── HexShield.csproj
│
├── HexShield.Client/          # Blazor WebAssembly client
│   ├── Pages/                 # Login, dashboards, course pages
│   ├── Components/            # Reusable UI components
│   ├── Layout/                # Application layouts
│   ├── Services/              # Client API and authentication services
│   ├── wwwroot/               # Client static assets
│   ├── Routes.razor
│   ├── Program.cs
│   └── HexShield.Client.csproj
│
├── HexShield.Shared/          # Shared contracts
│   ├── Models/
│   ├── DTOs/
│   └── HexShield.Shared.csproj
│
└── README.md
```

*The directory names above are a representative structure. Adjust them to match the actual repository.*

### Request flow

1. The user signs in through the client application.
2. The server validates the credentials and issues an authentication response.
3. The client stores the access token using its configured token-storage service.
4. The HTTP authorization handler attaches the token to protected API requests.
5. The server validates the JWT and applies authorization and tenant-isolation rules.
6. The API returns data through shared DTO contracts.
7. The Blazor client renders the returned data in the relevant pages.

## Prerequisites

Install the following before running the application:

- .NET 10 SDK.
- Visual Studio 2022 or a compatible IDE with .NET support.
- SQL Server or SQL Server LocalDB.
- Git.
- Optional: Postman for API testing.

Verify the installed SDK:

```bash
dotnet --info
```

## Installation and Setup

### 1. Clone the repository

```bash
git clone <YOUR_REPOSITORY_URL>
cd HexShield
```

Replace `<YOUR_REPOSITORY_URL>` with the actual Git repository URL.

### 2. Configure the database

Configure the connection string in the server project's configuration or user secrets.

Example `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=HexShieldDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Use the connection-string key expected by your server's `Program.cs`. Do not commit production credentials or secrets.

### 3. Configure authentication

Configure the JWT issuer, audience, signing key, and token lifetime according to the server's authentication implementation.

Use environment variables, user secrets, or your deployment platform's secret manager for signing keys.

Ensure that the client API base address points to the running server.

### 4. Restore dependencies

From the solution root:

```bash
dotnet restore
```

### 5. Apply database migrations

If migrations are managed by Entity Framework Core CLI, run:

```bash
dotnet ef database update --project HexShield
```

If the server project applies migrations automatically during startup, verify its migration configuration instead. Avoid running migrations twice through competing startup and deployment mechanisms.

### 6. Build the solution

```bash
dotnet build
```

A successful build confirms compilation. It does not guarantee that the client can load its runtime assets or that every API and UI workflow works at runtime.

### 7. Run the application

Start the server project, `HexShield`, as the startup project in Visual Studio.

Alternatively, use:

```bash
dotnet run --project HexShield
```

Open the HTTPS address printed in the server console.

For example, if the application reports `https://localhost:7263`, navigate to that address.

> The exact URL and port depend on your launch configuration.

## Authentication and Roles

HexShield defines three primary application roles:

| Role | Intended access |
|---|---|
| Admin | Administrative dashboard, user management, organization management, and authorized course administration |
| Teacher | Teacher dashboard, assigned courses, lessons, assignments, and assessments permitted by policy |
| Student | Student dashboard, enrolled courses, learning materials, assessments, and progress |

Access should be enforced on the server using authorization policies and endpoint protection. Hiding a navigation item in the client is not a security boundary.

The client uses an authentication-state provider, token storage, and an HTTP message handler to maintain authentication state and send bearer tokens to the API.

## API Documentation and Testing

When Swagger/OpenAPI is enabled in the server, run the application and open its configured Swagger endpoint, commonly:

```text
https://localhost:<PORT>/swagger
```

Use Postman or Swagger to test:

- Authentication and registration endpoints.
- Protected API endpoints with and without a bearer token.
- Course CRUD operations.
- Publication status changes.
- Role-based authorization.
- Tenant isolation and invalid resource identifiers.

The actual endpoints and HTTP methods should be verified against the controllers in the repository.

## Continuous Integration and Deployment

HexShield has a configured CI/CD pipeline, and the build currently succeeds.

The pipeline should validate:

1. Dependency restoration.
2. Server and client compilation.
3. Automated tests, when configured.
4. Client static-asset generation.
5. Server publishing and deployment.
6. Post-deployment health checks.

A successful pipeline is not equivalent to a successful interactive application. Production validation must include opening the deployed UI, checking browser console errors, confirming static asset responses, and testing authenticated API requests.

**Status:** CI/CD build succeeds; the client runtime asset-loading and UI initialization fixed successfully.
---

**HexShield** — A modular, multi-tenant learning platform built with .NET.
