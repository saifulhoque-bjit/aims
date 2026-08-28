# Repository notes

## Build

No .NET SDK is preinstalled. Install locally without root:

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /tmp/dotnet
export PATH=/tmp/dotnet:$PATH
dotnet build progcoder-shop-microservices.sln
```

`apt-get install dotnet-sdk-8.0` does not work on Debian trixie (no candidate).
Build artifacts (`bin/`, `obj/`) are not gitignored — delete them before committing.

## Testing

There is no test project in this slice. Validation is `dotnet build` plus code review.

## Seeded defects (AMS observability)

Two intentional defects are documented in `ARCHITECTURE.md` under
"AMS seeded incidents". They exist to generate real signals for the monitoring
stack, and the table there is the source of truth for which are live. Update
that table whenever one is fixed or added.

## Known blocker: GitHub token is read-only

As of 2026-08-28, both `GITHUB_TOKEN` and the PAT embedded in
`remote.origin.url` are the same fine-grained PAT with **only `metadata: read`**.
It cannot push branches or create PRs — `git push` and
`POST /repos/.../pulls` both return 403 "Resource not accessible by personal
access token". Diagnose with the `X-Accepted-GitHub-Permissions` response
header. If write access is needed, the token must be re-issued with
`Contents: read/write` and `Pull requests: read/write`.
