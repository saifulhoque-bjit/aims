namespace Catalog.Application.Services;

/// <summary>
/// Tells whether the backing database is accepting queries yet. Used by the
/// startup readiness gate so the service never serves traffic while the
/// database is still initializing (Postgres SQLSTATE 57P03).
/// </summary>
public interface IDatabaseReadinessService
{
    #region Methods

    /// <summary>Opens a connection and runs a trivial query.</summary>
    /// <returns>Null when the database answered, otherwise the failure that occurred.</returns>
    Task<Exception?> CheckAsync(CancellationToken cancellationToken = default);

    #endregion
}
