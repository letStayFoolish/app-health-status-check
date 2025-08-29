# DeepCheck

DeepCheck is an ASP.NET Core application for automated application health and availability checks. It schedules and runs synthetic tests (including browser-based checks via headless Chromium), stores and exposes the test run results, and streams live status updates to the browser via SignalR.

Typical use cases:
- Continuous health checks of web applications and APIs
- Synthetic monitoring: log in, navigate, verify key screens and responses
- Scheduled background tests and on-demand executions
- Real-time status dashboards

## Key Features

- Job scheduling and background execution with Hangfire
- Real browser automation with PuppeteerSharp (headless Chromium)
- Real-time live updates using SignalR (frontend widget under wwwroot/uptime)
- EF Core with migrations for persistence
- REST API and interactive docs via Swagger
- Configurable test definitions and steps
- Extensible design with services, repositories, and interfaces
- Unit tests and coverage (coverlet)

## Tech Stack

- Platform: .NET 9 / ASP.NET Core
- Data access: Entity Framework Core (DbContext + Migrations)
- Background jobs: Hangfire.AspNetCore (1.8.21)
- Browser automation: PuppeteerSharp (20.2.2)
- Scheduling: Cronos (0.11.1)
- Real-time: SignalR (Microsoft.AspNetCore.SignalR.Client 9.0.8 for client)
- API docs: Swashbuckle.AspNetCore.SwaggerGen (9.0.3)
- Testing/coverage: coverlet.collector (6.0.2)
- Frontend: Static assets served from wwwroot (SignalR client for uptime page)
- Containerization: Dockerfile included

## Project Structure

- DeepCheck/
    - Controllers/ — API endpoints (e.g., TestsController, DeepCheckHomeController)
    - Data/ — EF Core DbContext and configuration
    - DTOs/ — Request/response and data transfer types
    - Entities/ — Persistence models
    - Hubs/ — SignalR hubs (e.g., UptimeHub)
    - Helpers/ — Utility and configuration classes (e.g., Puppeteer settings)
    - Interfaces/ — Abstractions for services and repositories
    - Migrations/ — EF Core migrations
    - Middlewares/ — Cross-cutting concerns
    - Models/ — Domain models (RunInfo, TestRunDefinition, TestStepDefinition, enums)
    - Repositories/ — Data access implementations
    - Services/
        - Jobs/ — Job implementations (e.g., PushTestJob, WsUserLoginAndMarketOverview)
        - Puppeteer/ — Puppeteer service abstraction and browser provider
        - Ttws/ — External system client(s)
        - JobCleanup/ — Cleanup routines
        - TestRunService/ — Orchestration for test runs
        - HangfireTestRunner, TestRunner — Execution engines
        - BackgroundJobSchedulerHostedService — Startup and recurring schedules
    - wwwroot/ — Static site content (see uptime dashboard under wwwroot/uptime)
    - Program.cs — App composition, DI, middlewares, endpoints
    - appsettings.json — Application configuration
- DeepCheck.Tests/
    - Unit tests (with coverage via coverlet.collector)
- Dockerfile — Container build definition
- DeepCheck.sln — Solution
- .gitignore — Standard .NET ignores

## Getting Started

### Prerequisites

- .NET 9 SDK
- (Optional) Docker if you want to containerize
- Internet access for PuppeteerSharp to download a compatible Chromium on first run (or configure an existing executable path)

### Configuration

Key configuration is in DeepCheck/appsettings.json. You will find sections such as:
- Connection strings (database provider)
- PuppeteerSettings (Chromium download/cache/executable)
- Logging levels (including Hangfire)
- Any external endpoints for your tests

Example (illustrative):

```json 
{ "ConnectionStrings": { "Default": "Data Source=deepcheck.db" }, "PuppeteerSettings": { "DownloadChromium": true, "ExecutablePath": "", "Headless": true, "DefaultTimeoutMs": 30000 }, "Logging": { "LogLevel": { "Default": "Information", "Microsoft.EntityFrameworkCore": "Warning", "Hangfire": "Information" } } }
```

Note: Adjust to match your environment and secrets. You can override settings via environment variables or appsettings.Production.json.

### Run Locally

1) Restore and build
`bash dotnet restore dotnet build`
2) Apply EF Core migrations
`bash dotnet tool install --global dotnet-ef dotnet ef database update --project DeepCheck`
3) Run the app
`bash dotnet run --project DeepCheck`
4) Open in browser
- Swagger UI: http://localhost:5000/swagger (or the port shown in console)
- Uptime dashboard: http://localhost:5000/uptime/index.html

### Docker

Build the image:
`bash docker build -t deepcheck:latest .`
    Run the container:
`bash docker run --rm -p 5000:8080 -e ASPNETCORE_URLS=http://+:8080 deepcheck:latest`

Open:
- Swagger: http://localhost:5000/swagger
- Uptime: http://localhost:5000/uptime/index.html

If your tests or storage need persistence, mount volumes (e.g., for the database or Chromium cache).

## How It Works

- Scheduling: Background jobs (including recurring) are configured and executed to run checks on a schedule.
- Execution: Test runners orchestrate test run definitions and steps to perform checks.
- Browser flows: Headless Chromium automates user scenarios (login, navigation, assertions).
- Persistence: Test run data and metadata are stored for querying and dashboards.
- Live updates: SignalR hubs push real-time statuses to connected clients; a simple dashboard is provided under wwwroot/uptime.
- API: Controllers expose endpoints to manage and trigger tests and to retrieve results; Swagger provides interactive docs.

## Tests

Run tests:
`bash dotnet test`

Collect coverage:
`bash dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov`

## Extending

- Add new test types or steps by extending models/services and wiring them via DI.
- Add UI dashboards by serving static assets and subscribing to SignalR hubs.
- Change storage by updating the connection string and applying new migrations.

## Troubleshooting

- Puppeteer/Chromium
    - First run may download Chromium; ensure outbound internet or set an executable path.
    - On Linux/Docker, ensure system dependencies for Chromium are present.

- Jobs/Scheduling
    - Verify schedules and ensure a durable storage provider if required for production workloads.

- Database
    - Ensure migrations are applied and the connection string is valid and writable.

## License

Add your preferred license (e.g., MIT) here.

## Contributing

- Fork, create a feature branch, and open a PR.
- Add tests for new features or fixes.