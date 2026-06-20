# Third-Party Notices

LocalAIFactory uses third-party components under their respective licenses. Generate the authoritative list at
package time from the restored NuGet graph. Notable families:

- .NET / ASP.NET Core / EF Core (Microsoft) — MIT
- Microsoft.CodeAnalysis (Roslyn) — MIT
- Microsoft.SqlServer.TransactSql.ScriptDom — MIT
- Bootstrap, bootstrap-icons, marked.js (client CDN) — MIT
- Optional, external (not bundled): Qdrant, Ollama, local model weights — under their own licenses.

Run `dotnet list package --include-transitive` and your license scanner to produce the full, verified notice
file for a release. This file is a template placeholder.
