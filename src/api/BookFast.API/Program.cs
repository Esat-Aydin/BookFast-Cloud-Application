// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : Program.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Endpoints;
using BookFast.API.Diagnostics;
using BookFast.API.GraphQL;
using BookFast.API.Services;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["correlationId"] = ApiRequestContext.GetCorrelationId(context.HttpContext);
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks()
       .AddCheck(
           "self",
           () => HealthCheckResult.Healthy("BookFast API is ready."),
           ["ready"]);
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IBookFastCatalog, InMemoryBookFastCatalog>();
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddTypeExtension<RoomQueries>()
    .AddTypeExtension<ReservationQueries>()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 250;
        options.MaxTypeCost = 250;
        options.EnforceCostLimits = true;
        options.ApplyCostDefaults = true;
    });

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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

public partial class Program;
