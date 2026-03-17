using ECommerce.API;
using ECommerce.API.BackgroundJobs;
using ECommerce.Modules.Billing;
using ECommerce.Modules.Billing.Application.Events;
using ECommerce.Modules.Catalog;
using ECommerce.Modules.Ordering;
using ECommerce.Shared.Application;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Migrations", LogEventLevel.Warning)
    .MinimumLevel.Override("Quartz", LogEventLevel.Warning)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

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

// Register modules — PostgreSQL
builder.Services.AddCatalogModule(db => db.UseNpgsql(connectionString));
builder.Services.AddOrderingModule(db => db.UseNpgsql(connectionString));
builder.Services.AddBillingModule(db => db.UseNpgsql(connectionString));

// MassTransit — in-memory transport with Billing consumers
builder.Services.AddMassTransit(cfg =>
{
    cfg.AddConsumers(typeof(BillingModule).Assembly);
    cfg.UsingInMemory((context, bus) =>
    {
        bus.ConfigureEndpoints(context);
    });
});

// Quartz.NET — background job for Transactional Outbox processing
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("ProcessOutboxJob");
    q.AddJob<ProcessOutboxJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("ProcessOutboxTrigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInSeconds(
                builder.Configuration.GetValue("Outbox:IntervalSeconds", 10))
            .RepeatForever()));
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", options =>
    {
        options
            .WithTitle("ECommerce Modular API")
            .WithTheme(ScalarTheme.BluePlanet)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
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
        else
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
        }
    });
});

// Redirect root to API docs
app.MapGet("/", () => Results.Redirect("/docs")).ExcludeFromDescription();

// Map module endpoints
app.MapCatalogEndpoints();
app.MapOrderingEndpoints();
app.MapBillingEndpoints();

// Initialize database (EnsureCreated for development)
await app.InitializeDatabaseAsync();

// Seed fake data if database is empty (skip in Testing to keep tests deterministic)
if (!app.Environment.IsEnvironment("Testing"))
    await app.SeedAsync();

app.Run();

// Make the implicit Program class accessible to the test project
public partial class Program;
