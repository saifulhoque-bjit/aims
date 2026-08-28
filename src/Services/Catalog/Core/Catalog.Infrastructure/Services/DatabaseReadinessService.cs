#region using

using System.Net.Sockets;
using Catalog.Application.Services;
using Marten;
using Npgsql;

#endregion

namespace Catalog.Infrastructure.Services;

/// <summary>
/// Reads the Marten-backed Postgres database with a trivial query to decide
/// whether it is ready to serve. Marten's <c>QuerySession</c> is the same
/// connection path the endpoints use, so a green check here means the real
/// traffic path works too.
/// </summary>
public sealed class DatabaseReadinessService : IDatabaseReadinessService
{
    #region Fields, Properties and Indexers

    private readonly IDocumentStore _store;

    #endregion

    #region Ctors

    public DatabaseReadinessService(IDocumentStore store)
    {
        _store = store;
    }

    #endregion

    #region Implementations

    public async Task<Exception?> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = _store.QuerySession();
            // Uses the same pooled Npgsql connection the endpoints use, but
            // executes a scalar round-trip directly so no Marten document
            // deserialization is involved. Marten opens the session's
            // connection eagerly, so this is a real server round-trip that
            // fails while Postgres is still starting up (57P03), recovering
            // (57P02) or unreachable - exactly what the gate must detect.
            await using var command = session.Connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// True when the failure is a transient database-not-ready / not-reachable
    /// condition worth retrying, false when retrying cannot help (bad
    /// credentials, unknown database, cancelled shutdown).
    /// </summary>
    public static bool IsRetryable(Exception exception)
    {
        switch (exception)
        {
            case OperationCanceledException:
                return false;

            case PostgresException pg:
                return SqlState.IsRetryable(pg.SqlState);

            // Npgsql reports an unreachable or refused host as a plain socket
            // error rather than as a SQLSTATE, and that is the other half of the
            // startup race this gate exists for.
            case SocketException:
            case TimeoutException:
                return true;

            // A server that accepts TCP but aborts the handshake ("Exception
            // while reading from stream") is still initializing, not broken.
            // PostgresException is matched first, so a SQLSTATE-bearing failure
            // (bad password, unknown database) is never mistaken for this.
            // Cost of the rare mis-hit: bounded retries, never a wrong verdict.
            case IOException:
                return true;

            case NpgsqlException { InnerException: not null } npgsql:
                return IsRetryable(npgsql.InnerException);

            default:
                // Marten and the health-check wrappers rethrow the underlying
                // Npgsql error, so keep digging before giving up.
                return exception.InnerException is not null && IsRetryable(exception.InnerException);
        }
    }

    #endregion

    #region Constants

    /// <summary>
    /// SQLSTATE classes that mean "the server exists but is not serving yet".
    /// 57P03 = the database system is starting up, 57P02 = crash recovery,
    /// 08000-08006 = connection exceptions.
    /// </summary>
    private static class SqlState
    {
        #region Constants

        private const string CannotConnectNow = "57P03";
        private const string CrashRecovery = "57P02";
        private const string TooManyConnections = "53300";
        private const string ConnectionException = "08";

        #endregion

        #region Methods

        public static bool IsRetryable(string? sqlState)
        {
            if (string.IsNullOrEmpty(sqlState)) return false;

            return sqlState == CannotConnectNow
                || sqlState == CrashRecovery
                || sqlState == TooManyConnections
                || sqlState.StartsWith(ConnectionException, StringComparison.Ordinal);
        }

        #endregion
    }

    #endregion
}
