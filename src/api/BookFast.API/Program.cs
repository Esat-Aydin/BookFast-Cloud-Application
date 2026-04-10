// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : Program.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Endpoints;
using BookFast.API.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IBookFastCatalog, InMemoryBookFastCatalog>();

WebApplication app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health")
    .WithName("HealthCheck")
    .WithTags("Monitoring");

RouteGroupBuilder apiGroup = app.MapGroup("/api/v1");

apiGroup.MapRoomEndpoints();
apiGroup.MapReservationEndpoints();

app.Run();

public partial class Program;
