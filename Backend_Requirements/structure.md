# Backend Project Structure
**System:** Project Planning & Execution Tracking System
**Architecture:** Clean Architecture + Vertical Slice Architecture (Hybrid)
**Framework:** ASP.NET Core .NET 10 Web API
**API Style:** Minimal APIs with Endpoint Grouping and Feature Slices
**Mapping:** Manual LINQ projections (`.Select()`) and static factories (No AutoMapper)

---

## Architecture Pattern: Clean Architecture + Vertical Slice (Hybrid)

This architecture blends the domain safety of **Clean Architecture** with the maintainability and low cognitive load of **Vertical Slice Architecture (VSA)**:

1. **Domain Layer (Core)**: Remains clean, holds core entities, enums, and pure business domain services (like the Working Days Calendar and Status Engines).
2. **Infrastructure Layer**: Houses EF Core `DbContext`, SQL Server configurations, Migrations, and external services (e.g., ClosedXML Excel exporters).
3. **Vertical Slices (Application/API)**: Feature folders house everything required for an API endpoint. A single file or folder contains the request validation, the business handler, the response DTO, and the Minimal API endpoint mapping.

```
┌────────────────────────────────────────────────────────┐
│                      API Host                          │
│  (Program.cs, Configuration, Global Exception Handler)  │
├────────────────────────────────────────────────────────┤
│                 Vertical Feature Slices                │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Feature: CreateActionItem                         │  │
│  │ - Endpoint mapping (app.MapPost)                 │  │
│  │ - Request/Response DTOs                          │  │
│  │ - Handler Logic (EF Core Query & DB Save)        │  │
│  │ - Validation rules (FluentValidation)             │  │
│  └──────────────────────────────────────────────────┘  │
├────────────────────────────────────────────────────────┤
│                     Shared Core                        │
│  (Domain Entities, Enums, Shared Interfaces, Engines)  │
├────────────────────────────────────────────────────────┤
│                    Infrastructure                      │
│  (EF Core DbContext, SQL Migrations, Excel Export)     │
└────────────────────────────────────────────────────────┘
```

*Note: The term "Task" has been renamed to "ActionItem" globally to prevent conflicts with C# `System.Threading.Tasks.Task`.*

---

## Solution Structure

```
ProjectTracker.sln
│
├── src/
│   │
│   ├── ProjectTracker.API/                          ← Web Host & Feature Slices
│   │   ├── Extensions/
│   │   │   ├── EndpointExtensions.cs                ← Auto-registers all endpoints on startup
│   │   │   └── MigrationExtensions.cs               ← Automated migration runner
│   │   │
│   │   ├── Features/                                ← VERTICAL SLICES (Grouped by Resource)
│   │   │   ├── Auth/
│   │   │   │   ├── RegisterUser.cs                  ← Self-contained endpoint, request, handler, validation
│   │   │   │   ├── LoginUser.cs
│   │   │   │   └── RefreshToken.cs
│   │   │   │
│   │   │   ├── Projects/
│   │   │   │   ├── GetProjectsList.cs               ← Direct database projection
│   │   │   │   ├── CreateProject.cs
│   │   │   │   ├── GetProjectDetails.cs
│   │   │   │   └── UpdateProjectSettings.cs
│   │   │   │
│   │   │   ├── Categories/
│   │   │   │   ├── GetCategories.cs
│   │   │   │   ├── CreateCategory.cs
│   │   │   │   └── UpdateCategory.cs
│   │   │   │
│   │   │   ├── ActionItems/
│   │   │   │   ├── GetActionItemsList.cs            ← Filters, searches, projects DTO
│   │   │   │   ├── CreateActionItem.cs
│   │   │   │   ├── UpdateActionItem.cs
│   │   │   │   └── DeleteActionItem.cs
│   │   │   │
│   │   │   └── Timeline/
│   │   │       └── GetTimelineData.cs               ← Evaluates dynamic status and calendar
│   │   │
│   │   ├── Middleware/
│   │   │   └── GlobalExceptionHandler.cs            ← .NET 10 IExceptionHandler implementation
│   │   │
│   │   ├── appsettings.json                         ← Db connection strings & JWT settings
│   │   └── Program.cs                               ← Clean entry point mapping slices
│   │
│   ├── ProjectTracker.Domain/                       ← Domain Layer (Shared Entities)
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Project.cs
│   │   │   ├── ProjectMember.cs
│   │   │   ├── Category.cs
│   │   │   ├── SubCategory.cs
│   │   │   ├── ActionItem.cs
│   │   │   ├── PlannedSchedule.cs
│   │   │   ├── ActualExecution.cs
│   │   │   └── AuditLog.cs
│   │   │
│   │   ├── Enums/
│   │   │   ├── ActionItemStatus.cs
│   │   │   ├── ProjectStatus.cs
│   │   │   ├── UserRole.cs
│   │   │   ├── Priority.cs
│   │   │   └── TimelineScale.cs
│   │   │
│   │   └── Services/                                ← Domain Engines
│   │       ├── CalendarEngine.cs                    ← Excludes weekends and seed holidays
│   │       └── StatusEngine.cs                      ← Computes live status at runtime
│   │
│   └── ProjectTracker.Infrastructure/               ← Infrastructure Layer
│       ├── Data/
│       │   ├── ApplicationDbContext.cs              ← EF Core DbContext
│       │   └── Configurations/                      ← Entity Type Configurations
│       │       ├── ProjectConfiguration.cs
│       │       └── ActionItemConfiguration.cs
│       │
│       ├── Migrations/                              ← Database migrations
│       │
│       ├── Services/
│       │   └── ExcelExportService.cs                ← Generates ClosedXML output
│       │
│       └── Seeders/
│           └── HolidaySeeder.cs                     ← Database seed tool for national holidays
│
└── tests/
    └── ProjectTracker.Tests/                        ← Unit and slice-integration tests
```

---

## Key Libraries & Packages (.NET 10)

| Package | Purpose |
|---|---|
| Microsoft.AspNetCore.Authentication.JwtBearer | Authentication handler |
| Microsoft.EntityFrameworkCore.SqlServer | Database provider |
| Microsoft.EntityFrameworkCore.Design | Migrations design-time tools |
| FluentValidation.DependencyInjectionExtensions| Request validation |
| ClosedXML | Excel generation |
| BCrypt.Net-Next | Password cryptography |
| Serilog.AspNetCore | Structured logging |

---

## Program.cs Setup (.NET 10 Minimal API Style)

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectTracker.Domain.Services;
using ProjectTracker.Infrastructure.Data;
using ProjectTracker.Infrastructure.Services;
using ProjectTracker.Infrastructure.Seeders;
using ProjectTracker.API.Extensions;
using ProjectTracker.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Register Core/Infrastructure Dependencies
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Domain Engines
builder.Services.AddSingleton<CalendarEngine>();
builder.Services.AddSingleton<StatusEngine>();

// Infrastructure Services
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddScoped<HolidaySeeder>();

// Add Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// JWT Authentication Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!))
        };
    });

builder.Services.AddAuthorization();

// CORS Settings
builder.Services.AddCors(options =>
    options.AddPolicy("ReactApp", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()));

// Swagger / OpenAPI docs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Run migrations and seed database
await app.Services.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(); // Maps ProblemDetails from GlobalExceptionHandler
app.UseCors("ReactApp");
app.UseAuthentication();
app.UseAuthorization();

// Grouped and Versioned Minimal APIs
var apiGroup = app.MapGroup("api");

// Extension method automatically scans and maps all feature slices implementing IEndpointRouteHandler
apiGroup.MapFeatureEndpoints();

app.Run();
```

---

## Vertical Slice Implementation Example (.NET 10 style)

Here is a full self-contained feature slice located in `ProjectTracker.API/Features/ActionItems/CreateActionItem.cs`. It combines the endpoint mapping, input validation, and execution logic in a single file:

```csharp
namespace ProjectTracker.API.Features.ActionItems;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectTracker.API.Extensions;
using ProjectTracker.Domain.Entities;
using ProjectTracker.Infrastructure.Data;

// Feature Slice definition
public class CreateActionItem : IEndpointRouteHandler
{
    // Maps the Minimal API route
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("projects/{projectId:guid}/action-items", HandleAsync)
               .WithName("CreateActionItem")
               .WithTags("ActionItems")
               .RequireAuthorization()
               .Produces<Response>(StatusCodes.Status201Created)
               .Produces(StatusCodes.Status400BadRequest);
    }

    // Input contracts
    public record Request(
        string ActionItemName,
        string? Description,
        Guid CategoryId,
        Guid? SubCategoryId,
        int Priority,
        string? OwnerName,
        decimal? Weight,
        string PlannedStartDate, // "YYYY-MM-DD"
        string PlannedEndDate
    );

    // Output contracts
    public record Response(
        Guid Id,
        string ActionItemName,
        string PlannedStartWeek,
        string PlannedEndWeek,
        int DurationWorkingDays
    );

    // Validator rules (FluentValidation)
    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ActionItemName).NotEmpty().MaximumLength(500);
            RuleFor(x => x.CategoryId).NotEmpty();
            RuleFor(x => x.PlannedStartDate).NotEmpty();
            RuleFor(x => x.PlannedEndDate).NotEmpty();
        }
    }

    // Slice Logic Handler
    private static async Task<IResult> HandleAsync(
        Guid projectId,
        Request request,
        ApplicationDbContext dbContext,
        CalendarEngine calendarEngine,
        CancellationToken ct)
    {
        // 1. Run Validation
        var validator = new Validator();
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        // 2. Parse Dates
        var planStart = DateTime.Parse(request.PlannedStartDate);
        var planEnd = DateTime.Parse(request.PlannedEndDate);

        // 3. Compute working days (excluding weekends & seed holidays)
        var holidays = await dbContext.HolidayCalendar
            .Select(h => h.HolidayDate)
            .ToListAsync(ct);
        
        int workingDays = calendarEngine.CalculateWorkingDays(planStart, planEnd, DayOfWeek.Monday, holidays);

        // 4. Instantiate Entities
        var actionItem = new ActionItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            CategoryId = request.CategoryId,
            SubCategoryId = request.SubCategoryId,
            ActionItemName = request.ActionItemName,
            Description = request.Description,
            Priority = request.Priority,
            OwnerName = request.OwnerName,
            Weight = request.Weight,
            PlannedSchedule = new PlannedSchedule
            {
                PlannedStartDate = planStart,
                PlannedEndDate = planEnd,
                DurationWorkingDays = workingDays
            },
            ActualExecution = new ActualExecution()
        };

        dbContext.ActionItems.Add(actionItem);
        await dbContext.SaveChangesAsync(ct);

        // 5. Build DTO Projection manually
        var response = new Response(
            actionItem.Id,
            actionItem.ActionItemName,
            $"WW{planStart.DayOfYear / 7:D2}",
            $"WW{planEnd.DayOfYear / 7:D2}",
            workingDays
        );

        return Results.Created($"api/projects/{projectId}/action-items/{actionItem.Id}", response);
    }
}
```

---

## Manual Mapping Strategy (Vertical Slice Projection)

To ensure maximum execution efficiency and eliminate the overhead of third-party mappers, database query slices project direct queries using LINQ's `.Select()` syntax.

### Projection Query Example (from `GetProjectsList.cs`)

```csharp
public static async Task<IResult> HandleAsync(
    ApplicationDbContext dbContext,
    ClaimsPrincipal user,
    CancellationToken ct)
{
    var currentUserId = user.GetUserId();

    // Map the database entities directly into an anonymous or record DTO
    var projectList = await dbContext.Projects
        .AsNoTracking()
        .Where(p => p.ProjectMembers.Any(m => m.UserId == currentUserId))
        .OrderByDescending(p => p.CreatedAt)
        .Select(p => new ProjectItemResponse(
            p.Id,
            p.Name,
            p.Status,
            p.ActionItems.Count,
            p.ActionItems.Count(t => t.ActualExecution != null && t.ActualExecution.ActualEndDate != null),
            p.ActionItems.Count(t => t.ActualExecution == null || t.ActualExecution.ActualEndDate == null 
                && t.PlannedSchedule != null && t.PlannedSchedule.PlannedEndDate < DateTime.UtcNow),
            p.ProjectMembers.Where(m => m.UserId == currentUserId).Select(m => (int)m.Role).FirstOrDefault(),
            p.StartDate.ToString("yyyy-MM-dd"),
            p.EndDate.ToString("yyyy-MM-dd")
        ))
        .ToListAsync(ct);

    return Results.Ok(projectList);
}
```

---

## Shared Endpoint Registrator

Minimal API slices automatically register themselves on startup using reflection mapping.

```csharp
// ProjectTracker.API/Extensions/EndpointExtensions.cs

public interface IEndpointRouteHandler
{
    void MapEndpoint(IEndpointRouteBuilder builder);
}

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder builder)
    {
        var endpointTypes = typeof(EndpointExtensions).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEndpointRouteHandler).IsAssignableFrom(t));

        foreach (var type in endpointTypes)
        {
            var handler = (IEndpointRouteHandler)Activator.CreateInstance(type)!;
            handler.MapEndpoint(builder);
        }

        return builder;
    }
}
```
