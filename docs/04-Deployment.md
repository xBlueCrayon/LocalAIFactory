# 04 — Deployment (IIS)

LocalAIFactory is an ASP.NET Core MVC app and deploys like any .NET 10 site, typically on IIS with
the ASP.NET Core Hosting Bundle.

## Publish

```powershell
dotnet publish src/LocalAIFactory.Web/LocalAIFactory.Web.csproj -c Release -o .\publish
```

Deploy the contents of `.\publish` to the IIS site's physical path. `publish/` is git-ignored.

## IIS setup

1. Install the **.NET 10 Hosting Bundle** on the server (provides the ASP.NET Core Module).
2. Create an IIS site/app pool: **No Managed Code**, pipeline integrated.
3. Point the site at the published folder.
4. Run the app pool under an identity that can reach SQL Server (Windows auth) or use a SQL login in
   the connection string.

## Configuration on the server

- Set the connection string via environment variable or a server-only config; do **not** ship
  credentials in source control. Example (machine/user env or `web.config` env section):
  `ConnectionStrings__DefaultConnection`.
- For minimal mode set `Ollama__Enabled=false`, `Qdrant__Enabled=false`, `Rag__UseVectorSearch=false`.
- The app migrates and seeds on startup; ensure the SQL login can create/alter the database, or
  pre-apply migrations with `dotnet ef database update` (project=Data, startup=Web).

## Data Protection keys

Stored API keys are encrypted with ASP.NET Data Protection. Keys persist to a `keys/` folder under
the content root (git-ignored). For multi-server or recycling-safe deployments, persist keys to a
shared location/secret store so encrypted values remain readable across instances.

## Health & smoke test after deploy

Hit `/`, `/Projects`, `/Knowledge`, `/Models` — all must return 200 quickly. `RequestTimingMiddleware`
logs each request's duration; a "started" line with no "completed" line indicates a stalled endpoint.

## Optional services

Run Ollama and/or Qdrant on the host (or reachable network) and enable the flags. No redeploy of code
is needed to turn AI features on or off — they are configuration-driven.
