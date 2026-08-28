#region using

using System.Net;
using System.Net.Sockets;
using Catalog.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

#endregion

namespace Catalog.ReadinessTests;

/// <summary>
/// Runs the real Catalog.Api startup path - including the database readiness
/// gate - under TestHost. Only the database check is substituted; the gate
/// itself, the host wiring and the middleware pipeline are production code.
/// </summary>
public class DatabaseReadinessGateTests
{
    #region Methods

    private static WebApplicationFactory<Program> CreateFactory(
        IDatabaseReadinessService readiness,
        bool enableGate = true,
        int maxAttempts = 2)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:Serilog:Enable"] = "false",   // keep the test console free of OTLP noise
                ["DatabaseReadiness:Enable"] = enableGate ? "true" : "false",
                ["DatabaseReadiness:MaxAttempts"] = maxAttempts.ToString(),
                ["DatabaseReadiness:BaseDelaySeconds"] = "0",
                ["DatabaseReadiness:MaxDelaySeconds"] = "1"
            }));
            builder.ConfigureTestServices(services =>
                // Registered after the production registration, so it wins.
                services.AddSingleton(readiness));
        });
    }

    /// <summary>Builds the host, returning any startup failure instead of throwing.</summary>
    private static Exception? StartHost(WebApplicationFactory<Program> factory)
    {
        try
        {
            _ = factory.Server;
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static string Unwrap(Exception? ex) => ex?.ToString() ?? string.Empty;

    [Fact]
    public void Startup_BlocksServing_WhenDatabaseNeverBecomesReady()
    {
        // Npgsql wraps the socket error the way the runtime actually does on a
        // refused connection during Postgres startup.
        var checker = new StubReadinessService(new NpgsqlException("Failed to connect", new SocketException((int)SocketError.ConnectionRefused)));
        using var factory = CreateFactory(checker, maxAttempts: 3);

        var failure = StartHost(factory);

        Assert.NotNull(failure);
        Assert.Equal(3, checker.Calls);
    }

    [Fact]
    public void Startup_BlocksServing_WhenPostgresReportsStartingUp()
    {
        // The incident itself: Postgres answers but is still initializing.
        var checker = new StubReadinessService(SqlState.AsException(SqlState.StartingUp));
        using var factory = CreateFactory(checker);

        var failure = StartHost(factory);

        Assert.NotNull(failure);
        Assert.Contains("still not accepting connections", Unwrap(failure), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("57P03", Unwrap(failure), StringComparison.Ordinal);
    }

    [Fact]
    public void Startup_FailsFast_OnNonRetryableDatabaseError()
    {
        // 28P01 = bad password: retrying cannot help, so the gate must stop now.
        var checker = new StubReadinessService(SqlState.AsException(SqlState.BadPassword));
        using var factory = CreateFactory(checker, maxAttempts: 5);

        var failure = StartHost(factory);

        Assert.NotNull(failure);
        Assert.Contains("non-retryable", Unwrap(failure), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, checker.Calls);
    }

    [Fact]
    public async Task Startup_ServesTraffic_WhenDatabaseIsReady()
    {
        var checker = new StubReadinessService(null);
        using var factory = CreateFactory(checker);

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Running", body, StringComparison.Ordinal);
        Assert.Equal(1, checker.Calls);
    }

    [Fact]
    public async Task Startup_DoesNotWait_WhenGateIsDisabled()
    {
        var checker = new StubReadinessService(SqlState.AsException(SqlState.StartingUp));
        using var factory = CreateFactory(checker, enableGate: false);

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, checker.Calls);
    }

    [Fact]
    public async Task Startup_Succeeds_AfterDatabaseRecoversMidway()
    {
        // Postgres finishing initialization while the service waits: attempt 1
        // fails with 57P03, attempt 2 succeeds.
        var checker = new StubReadinessService(SqlState.AsException(SqlState.StartingUp), succeedFromAttempt: 2);
        using var factory = CreateFactory(checker);

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, checker.Calls);
    }

    #endregion

    #region Fixtures

    private static class SqlState
    {
        #region Constants

        public const string StartingUp = "57P03";

        public const string BadPassword = "28P01";

        #endregion

        #region Methods

        public static PostgresException AsException(string sqlState) =>
            new("readiness failure", "P-1", null, sqlState);

        #endregion
    }

    private sealed class StubReadinessService(Exception? outcome, int succeedFromAttempt = int.MaxValue)
        : IDatabaseReadinessService
    {
        #region Fields, Properties and Indexers

        public int Calls;

        #endregion

        #region Implementations

        public Task<Exception?> CheckAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Calls >= succeedFromAttempt ? null : outcome);
        }

        #endregion
    }

    #endregion
}
