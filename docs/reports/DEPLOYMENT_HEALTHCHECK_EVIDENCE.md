# Deployment Healthcheck Evidence (Phase 6)

**Date:** 2026-06-21 · **Endpoint:** `http://localhost:8095` · **DB:** `.\SQLEXPRESS / LocalAIFactory_DeploymentProof`

A dedicated post-deploy healthcheck script was added and run against the live Mode C deployment:
`scripts/deployment-drill/09-post-deploy-healthcheck.ps1` (read-only; returns non-zero on any failed gate).

## `09-post-deploy-healthcheck.ps1` — PASS

```
== Post-deploy health check (Mode C) — http://localhost:8095 / .\SQLEXPRESS / LocalAIFactory_DeploymentProof ==
  [ OK ] GET / -> 200
  [ OK ] GET /Support -> 200
  [ OK ] GET /Readiness -> 200
  [ OK ] GET /BaseKnowledge -> 200
  [ OK ] GET /Coverage -> 200
  [ OK ] GET /Graph -> 200
  [ OK ] search 'OCR' -> 57 matches
  [ OK ] search 'Mauritius' -> 90 matches
  [ OK ] DB reachable; installed packs = 4 (>= 4)
  [ OK ] pack items = 438 (>= 438)
  [ OK ] migrations applied = 14
  deployment mode: C (C = published app + SQL Express, no IIS)
POST-DEPLOY-HEALTHCHECK (Mode C): PASS
```

Gates covered: app HTTP, Support page, Readiness page, BaseKnowledge search, DB connectivity, knowledge
pack counts, migrations, deployment mode, logs-path note, no HTTP 500s.

## Drill `08-capture-evidence.ps1` — captured

Ran against the same endpoint/DB; wrote host facts, page health (`/`, `/Support`, `/Readiness`,
`/BaseKnowledge` all 200), `verify-full-install` output (exit 0), and the support bundle into
`.tmp-deployment-evidence/` (git-ignored — attached to the sign-off record, never committed).

## Drill `06-run-healthchecks.ps1` — note

`06` ran `verify-full-install` against the deployment DB (**PASS**), then delegated to
`post-install-healthcheck.ps1`, which is hard-coded to port **8080** and so reported the (correct)
"connection refused" for `:8080` — the deployment is on `:8095`. This is a cosmetic limitation of `06`'s
delegate, **not** a deployment failure; `09-post-deploy-healthcheck.ps1` (port-parameterised) is the
authoritative health gate and **PASSED**.
