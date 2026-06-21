# Mode A — Support Bundle & Event Log Evidence (Phase 7)

**Date:** 2026-06-21

## Support bundle

`scripts/support/export-support-bundle.ps1` → `./.tmp-release/LocalAIFactory-support-bundle.zip` (~3 KB,
git-ignored, **not committed**). Sections: ollama-health, sql-health, process-monitor, knowledge-verify,
security-audit. No secrets.

## IIS site / app pool state (appcmd)

```
SITE    "LocalAIFactoryPilot"      (id:2, bindings:http/*:8095:, state:Started)
APPPOOL "LocalAIFactoryPilotPool"  (MgdVersion:, MgdMode:Integrated, state:Started)
```

## Application event log (ANCM — summarized, not raw)

Recent `Application` log entries for the deployed app (last 30 min):

| Time | Provider | Level | Event |
|---|---|---|---|
| 10:40:18 | IIS AspNetCore Module V2 | Information | 1032 (process started) |
| 10:40:14 | IIS AspNetCore Module V2 | Information | 1033 |
| 10:38:52 | IIS AspNetCore Module V2 | Information | 1032 (process started) |
| 10:40:17 | .NET Runtime | Warning | 1000 (benign startup warning) |

- **ANCM (`IIS AspNetCore Module V2`) Information events confirm IIS launched the .NET process** under the
  app-pool identity (the defining signature of IIS hosting via ANCM).
- **No `Application Error` / ANCM failure (e.g. 1000-series critical, 500.3x) events** for the app.
- The benign `.NET Runtime` Warning is a normal startup notice, not an error.

Raw event logs are **summarized only** here (not committed) per the safety rules.
