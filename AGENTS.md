# AGENTS.md

## Build & test

- .NET 8 solution: `progcoder-shop-microservices.sln`. Central package management is on via `src/Directory.Packages.props` — add new package versions there, not in the `.csproj`.
- The SDK is not preinstalled in this container. Install per-user with:
  `/tmp/dotnet-install.sh --channel 8.0 --install-dir /home/openhands/.dotnet` (script from https://dot.net/v1/dotnet-install.sh), then invoke `/home/openhands/.dotnet/dotnet`.
- First restore is slow (~90s); subsequent builds are fast. Restore/build emits NU19xx vulnerability warnings for pinned packages (AutoMapper, Marten, OpenTelemetry) — pre-existing, not caused by your change.
- Tests: `tests/` has its own `Directory.Build.props` + `Directory.Packages.props` so test packages stay out of the src version file. Run with `dotnet test progcoder-shop-microservices.sln`.
- Test files need an explicit `using Xunit;` — implicit usings do not cover it.

## Conventions

- C# files use `#region using` / `#region Fields, Properties and Indexers` / `#region Implementations` blocks. Match this style.
- `decimal` in a relational `is` pattern fails with CS9135 ("A constant value of type 'decimal' is expected") — use `>` / `<` / `&&` instead.

## Repo context

This is a deliberately minimal observability slice built to give an AI monitoring agent (AMS) real signals. Two defects were **intentionally seeded** and documented in `ARCHITECTURE.md` under "AMS seeded incidents":

1. `GetProductByIdQuery` — `NullReferenceException` on unpublished products (still live, left alone).
2. `GetAllProductsQuery` — `DivideByZeroException` when `SalePrice = 0` (fixed in PR #4).

If an incident ticket points at one of these, check whether the ticket is the monitoring drill itself before fixing. Fixing them removes the signal the stack exists to produce.
