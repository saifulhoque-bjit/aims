#region using

using System.Net.Sockets;
using Catalog.Infrastructure.Services;
using Marten;
using Marten.Exceptions;
using Npgsql;
using Xunit;

#endregion

namespace Catalog.ReadinessTests;

public class DatabaseReadinessServiceTests
{
    #region Methods

    [Theory]
    [InlineData("57P03", true)]   // the database system is starting up - the incident's SQLSTATE
    [InlineData("57P02", true)]   // crash recovery in progress
    [InlineData("08000", true)]   // connection_exception
    [InlineData("08001", true)]   // sqlclient_unable_to_establish_sqlconnection
    [InlineData("08006", true)]   // connection_failure
    [InlineData("53300", true)]   // too_many_connections - clears once the pool drains
    [InlineData("28P01", false)]  // password authentication failed - retrying cannot help
    [InlineData("3D000", false)]  // catalog/database does not exist
    [InlineData("42602", false)]  // invalid connector / config error
    public void IsRetryable_ClassifiesPostgresSqlState(string sqlState, bool expected)
    {
        var ex = new PostgresException("message", "where", "detail", sqlState);

        Assert.Equal(expected, DatabaseReadinessService.IsRetryable(ex));
    }

    [Fact]
    public void IsRetryable_UnwrapsNpgsqlSocketFailures()
    {
        // Unreachable host: Npgsql reports a socket exception, not a SQLSTATE.
        var ex = new NpgsqlException("Failed to connect", new SocketException((int)SocketError.ConnectionRefused));

        Assert.True(DatabaseReadinessService.IsRetryable(ex));
    }

    [Fact]
    public void IsRetryable_UnwrapsMartenWrappedNpgsqlErrors()
    {
        var inner = new NpgsqlException("Failed to connect", new SocketException((int)SocketError.HostUnreachable));
        var ex = new InvalidOperationException("Marten could not connect", inner);

        Assert.True(DatabaseReadinessService.IsRetryable(ex));
    }

    [Fact]
    public void IsRetryable_FatalInnerErrorsAreNotRetryable()
    {
        var inner = new PostgresException("password authentication failed", "where", "detail", "28P01");
        var ex = new InvalidOperationException("Marten could not connect", inner);

        Assert.False(DatabaseReadinessService.IsRetryable(ex));
    }

    [Fact]
    public void IsRetryable_OperationCanceledIsNotRetryable()
    {
        Assert.False(DatabaseReadinessService.IsRetryable(new OperationCanceledException()));
    }

    [Fact]
    public async Task CheckAsync_ReportsFailureForUnreachableDatabase()
    {
        // Port 1 is discard-tier and refuses connections on every platform, so
        // this exercises the real Npgsql/Marten connection path without a server.
        await using var store = DocumentStore.For("Host=localhost;Port=1;Database=catalog_test;Username=none;Password=none;Timeout=2");
        var sut = new DatabaseReadinessService(store);

        var failure = await sut.CheckAsync();

        Assert.NotNull(failure);
        Assert.True(DatabaseReadinessService.IsRetryable(failure!), $"expected a retryable connect failure, got {failure!.GetType().Name}: {failure.Message}");
    }

    [Theory]
    [MemberData(nameof(StartupPhaseFailures))]
    public void IsRetryable_AllRealStartupPhaseFailures(Exception failure)
    {
        Assert.True(DatabaseReadinessService.IsRetryable(failure));
    }

    #endregion

    #region Fixtures

    /// <summary>
    /// Failure shapes observed from a real Npgsql/Marten stack while a
    /// PostgreSQL server is starting, captured by running the API against a
    /// listener that accepts and then drops the connection.
    /// </summary>
    public static TheoryData<Exception> StartupPhaseFailures => new()
    {
        // 57P03 - the incident: server answers, still initializing.
        new PostgresException("the database system is starting up", "CANNOT_CONNECT_NOW", "detail", "57P03"),
        // Server not reachable at all yet.
        new NpgsqlException("Failed to connect", new SocketException((int)SocketError.ConnectionRefused)),
        // Server accepts TCP, drops the handshake mid-read.
        new IOException("Exception while reading from stream"),
        // Marten wraps any of the above in its command failure.
        new MartenCommandException(new NpgsqlCommand(), new NpgsqlException(
            "Exception while reading from stream",
            new IOException("Exception while reading from stream"))),
        // Or in a plain runtime wrapper when no command was involved.
        new InvalidOperationException("Marten could not connect", new NpgsqlException(
            "Failed to connect",
            new SocketException((int)SocketError.ConnectionRefused)))
    };

    #endregion
}
