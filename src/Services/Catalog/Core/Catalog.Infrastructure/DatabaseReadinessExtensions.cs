#region using

using Catalog.Application.Services;
using Catalog.Infrastructure.Services;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

#endregion

namespace Catalog.Infrastructure;

public static class DatabaseReadinessExtensions
{
    #region Methods

    /// <summary>
    /// Waits until the Marten-backed database answers a query before the host
    /// starts accepting HTTP traffic. Without this, the service comes up while
    /// Postgres is still initializing and every request fails with
    /// Npgsql.PostgresException 57P03 ("the database system is starting up").
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the database is still unavailable after the configured
    /// number of attempts, so the host fails fast instead of serving errors.
    /// </exception>
    public static async Task WaitForDatabaseReadinessAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        var cfg = app.Configuration;
        var section = DatabaseReadinessCfg.Section;
        if (!cfg.GetValue($"{section}:{DatabaseReadinessCfg.Enable}", true)) return;

        var maxAttempts = Math.Max(1, cfg.GetValue($"{section}:{DatabaseReadinessCfg.MaxAttempts}", 10));
        var baseDelay = TimeSpan.FromSeconds(Math.Max(0, cfg.GetValue($"{section}:{DatabaseReadinessCfg.BaseDelaySeconds}", 2)));
        var maxDelay = TimeSpan.FromSeconds(Math.Max(0, cfg.GetValue($"{section}:{DatabaseReadinessCfg.MaxDelaySeconds}", 30)));

        // A slow database must never trap the host in the gate while the
        // container is being told to shut down.
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping);

        var checker = app.Services.GetRequiredService<IDatabaseReadinessService>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Catalog.DatabaseReadiness");

        for (var attempt = 1; ; attempt++)
        {
            var failure = await checker.CheckAsync(stopping.Token);

            if (failure is null)
            {
                if (attempt > 1)
                {
                    logger.LogInformation("Database became ready after {Attempts} attempt(s)", attempt);
                }

                return;
            }

            stopping.Token.ThrowIfCancellationRequested();

            var retryable = DatabaseReadinessService.IsRetryable(failure);
            var message = Describe(failure);

            if (attempt >= maxAttempts || !retryable)
            {
                var reason = retryable
                    ? $"still not accepting connections after {maxAttempts} attempts"
                    : "reported a non-retryable error";

                logger.LogCritical("Database readiness gate failed: {Reason}. Last error: {Error}", reason, message);

                throw new InvalidOperationException(
                    $"Catalog database {reason}: {message}. " +
                    "The service will not start until PostgreSQL is fully initialized (SQLSTATE 57P03 = starting up).");
            }

            var delay = Backoff(attempt, baseDelay, maxDelay);
            logger.LogWarning(
                "Database not ready yet (attempt {Attempt}/{MaxAttempts}), retrying in {Delay:F1}s: {Error}",
                attempt, maxAttempts, delay.TotalSeconds, message);

            try
            {
                await Task.Delay(delay, stopping.Token);
            }
            catch (OperationCanceledException)
            {
                stopping.Token.ThrowIfCancellationRequested();
            }
        }
    }

    private static TimeSpan Backoff(int attempt, TimeSpan baseDelay, TimeSpan maxDelay)
    {
        // Exponential growth from the base delay, capped, with jitter so a
        // restarted fleet does not stampede the database in lockstep.
        var exponential = baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        var jitter = Random.Shared.NextDouble() * 0.3 + 0.85; // 85%-115%
        var delay = TimeSpan.FromMilliseconds(exponential * jitter);
        return delay > maxDelay ? maxDelay : delay;
    }

    private static string Describe(Exception failure) =>
        failure is PostgresException pg ? $"Postgres {pg.SqlState}: {pg.MessageText}" : failure.Message;

    #endregion
}
