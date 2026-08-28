#region using

using Catalog.Api;
using Catalog.Application;
using Catalog.Infrastructure;

#endregion

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseApi();
app.UseInfrastructure();

// Gate serving on database readiness: PostgreSQL rejects connections with
// SQLSTATE 57P03 ("the database system is starting up") until it has finished
// initializing, so accept traffic only once it answers a query. Keeps
// cold starts, restarts and host reboots free of Npgsql 57P03 errors.
await app.WaitForDatabaseReadinessAsync();

app.Run();

// Partial class declaration so Program is visible to WebApplicationFactory-based tests.
public partial class Program;
