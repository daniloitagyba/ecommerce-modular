using ECommerce.API;
using ECommerce.Modules.Billing;
using ECommerce.Modules.Catalog;
using ECommerce.Modules.Ordering;
using ECommerce.Shared.Application;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

// MediatR — register handlers from all module assemblies
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(CatalogModule).Assembly,
        typeof(OrderingModule).Assembly,
        typeof(BillingModule).Assembly);

    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// FluentValidation — register validators from all module assemblies
builder.Services.AddValidatorsFromAssemblies([
    typeof(CatalogModule).Assembly,
    typeof(OrderingModule).Assembly,
    typeof(BillingModule).Assembly
]);

// Register modules — the host owns the database provider choice
builder.Services.AddCatalogModule(db => db.UseSqlite(connectionString));
builder.Services.AddOrderingModule(db => db.UseSqlite(connectionString));
builder.Services.AddBillingModule(db => db.UseSqlite(connectionString));

// OpenAPI / Swagger
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Global error handling for validation exceptions
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

        if (exception is ValidationException validationException)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            var errors = validationException.Errors
                .Select(e => new { e.PropertyName, e.ErrorMessage })
                .ToArray();
            await context.Response.WriteAsJsonAsync(new { errors });
        }
        else if (exception is KeyNotFoundException)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = exception.Message });
        }
        else
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
        }
    });
});

// Map module endpoints
app.MapCatalogEndpoints();
app.MapOrderingEndpoints();
app.MapBillingEndpoints();

// Initialize database (EnsureCreated for development)
await app.InitializeDatabaseAsync();

app.Run();
