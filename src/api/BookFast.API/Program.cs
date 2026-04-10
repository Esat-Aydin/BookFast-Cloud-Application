// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : Program.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Common;
using BookFast.API.Endpoints;
using BookFast.API.Diagnostics;
using BookFast.API.Infrastructure.Eventing;
using BookFast.API.GraphQL;
using BookFast.API.Infrastructure.Persistence;
using BookFast.API.Services;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

builder.Services.AddOpenApi();
builder.Services.Configure<ObservabilityOptions>(
    builder.Configuration.GetSection(ObservabilityOptions.SectionName));
builder.Services.Configure<ApiGovernanceOptions>(
    builder.Configuration.GetSection(ApiGovernanceOptions.SectionName));
builder.Services.Configure<ApiCorsOptions>(
    builder.Configuration.GetSection(ApiCorsOptions.SectionName));
builder.Services.Configure<BookFastDatabaseOptions>(
    builder.Configuration.GetSection(BookFastDatabaseOptions.SectionName));
string? connectionString = builder.Configuration.GetConnectionString(BookFastDatabaseOptions.ConnectionStringName);
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        $"Connection string '{BookFastDatabaseOptions.ConnectionStringName}' is required.");
}

ApiCorsOptions corsOptions = builder.Configuration.GetSection(ApiCorsOptions.SectionName).Get<ApiCorsOptions>() ?? new ApiCorsOptions();
builder.Services.AddDbContext<BookFastDbContext>(options =>
{
    options.UseSqlServer(
        connectionString,
        sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure();
        });
});
builder.Services.AddBookFastEventing(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy(ApiCorsOptions.PolicyName, policyBuilder =>
    {
        if (corsOptions.AllowedOrigins.Length == 0)
        {
            return;
        }

        policyBuilder
            .WithOrigins(corsOptions.AllowedOrigins)
            .WithMethods(corsOptions.AllowedMethods)
            .WithHeaders(corsOptions.AllowedHeaders)
            .WithExposedHeaders(corsOptions.ExposedHeaders);
    });
});
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["correlationId"] = ApiRequestContext.GetCorrelationId(context.HttpContext);

        if (!context.ProblemDetails.Extensions.ContainsKey("errorCode"))
        {
            context.ProblemDetails.Extensions["errorCode"] =
                ApiProblemDetailsFactory.ResolveDefaultErrorCode(context.ProblemDetails.Status);
        }
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks()
       .AddCheck(
           "self",
           () => HealthCheckResult.Healthy("BookFast API is ready."),
           ["ready"])
       .AddDbContextCheck<BookFastDbContext>(
           "database",
           tags: ["ready"]);
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<IBookFastCatalog, SqlBookFastCatalog>();
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddTypeExtension<RoomQueries>()
    .AddTypeExtension<ReservationQueries>()
    .AddTypeExtension<ReadModelQueries>()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 250;
        options.MaxTypeCost = 250;
        options.EnforceCostLimits = true;
        options.ApplyCostDefaults = true;
    });

WebApplication app = builder.Build();

await ApplyDatabaseMigrationsAsync(app);

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiGovernanceHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(ApiCorsOptions.PolicyName);

app.MapHealthChecks("/health")
   .WithName("HealthCheck")
   .WithTags("Monitoring");

app.MapHealthChecks(
       "/health/live",
       new HealthCheckOptions
       {
           Predicate = _ => false
       })
   .WithName("LivenessCheck")
   .WithTags("Monitoring");

app.MapHealthChecks(
       "/health/ready",
       new HealthCheckOptions
       {
           Predicate = registration => registration.Tags.Contains("ready")
       })
   .WithName("ReadinessCheck")
   .WithTags("Monitoring");

app.MapGraphQL("/graphql");

RouteGroupBuilder apiGroup = app.MapGroup("/api/v1");

apiGroup.MapRoomEndpoints();
apiGroup.MapReservationEndpoints();

app.Run();

static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    BookFastDatabaseOptions databaseOptions = app.Services.GetRequiredService<IOptions<BookFastDatabaseOptions>>().Value;
    if (!databaseOptions.ApplyMigrationsOnStartup)
    {
        return;
    }

    await using AsyncServiceScope serviceScope = app.Services.CreateAsyncScope();
    BookFastDbContext dbContext = serviceScope.ServiceProvider.GetRequiredService<BookFastDbContext>();
    ILogger logger = serviceScope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("BookFastDatabase");

    logger.LogInformation("Applying BookFast database migrations at startup.");
    await dbContext.Database.MigrateAsync();
}

public partial class Program;
