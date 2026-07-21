# SunyaSuite — Client Management & Billing System

A Blazor Web App for managing clients, tracking projects, and generating professional PDF invoices, with traffic-light health monitoring.

## Tech Stack

- **.NET 10** (Blazor Web App, Interactive Server)
- **MudBlazor 9.5** (UI components, Vercel-inspired theme)
- **Entity Framework Core 10** (SQL Server, Code-First)
- **ASP.NET Core Identity** (auth, role-based policies)
- **QuestPDF** (PDF invoices)
- **Serilog** (structured logging)
- **Docker** (multi-stage chiseled images + Compose)

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Run with Docker (recommended)

```bash
cp .env.example .env
docker compose up --build
```

App: http://localhost:8080  
Login: admin@sunya.local / Admin@123

### Run locally (requires SQL Server)

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=SunyaSuite;..."
dotnet ef database update -p src/SunyaSuite.Infrastructure -s src/SunyaSuite.Web
dotnet run --project src/SunyaSuite.Web
```

## Project Structure

```
src/
├── SunyaSuite.Domain/        # Entities, enums, constants
├── SunyaSuite.Application/   # Interfaces, DTOs, validators, business logic
├── SunyaSuite.Infrastructure/ # EF Core, repositories, external services
└── SunyaSuite.Web/           # Blazor UI, theme, composition root
```

## Phases

| Phase | Feature | Status |
|-------|---------|--------|
| 1 | Foundation, auth, data model, Docker | ✅ |
| 2 | Client CRUD + traffic-light | ⬜ |
| 3 | Project tracking | ⬜ |
| 4 | Invoicing + line items | ⬜ |
| 5 | PDF invoices + dashboard + background service | ⬜ |
| 6 | Reports, audit log, email, polish | ⬜ |
| 7 | Testing + hardening | ⬜ |
