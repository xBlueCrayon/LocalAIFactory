# Supportability Dashboard Spec (/Support)

Specification of the read-only **Support** page — the operations / supportability dashboard that
answers "is this install healthy and what version is it?" at a glance, for an admin or support
engineer. Implemented in `src/LocalAIFactory.Web/Controllers/SupportController.cs`.

The page exists to make an install **self-describing** during a support call without touching the
database or any external service in a way that could slow or break it.

---

## 1. Hard rules the page obeys

1. **Health is read from the cached snapshot** (`IServiceHealthCache`) — **never** a synchronous
   Qdrant/Ollama probe on the request path.
2. **DB facts use lightweight `CountAsync` queries**, each independently guarded — a DB hiccup
   degrades a tile to "unavailable" rather than returning a 500. **No large text columns are
   materialised.**
3. **License/edition is evaluated deterministically and demo-safe** — a missing license resolves to
   Community core; it never blocks the page.
4. **The page must always render** — on an empty DB, a seeded DB, or in MSSQL-only mode.

These mirror the platform-wide rules in `CLAUDE.md` and make `SupportController` a reference example
of them.

---

## 2. Tiles and their data sources

### Build / version
| Field | Source |
|---|---|
| Environment | `IWebHostEnvironment.EnvironmentName` |
| Machine | `System.Environment.MachineName` |
| Framework | `RuntimeInformation.FrameworkDescription` |
| OS | `RuntimeInformation.OSDescription` |
| Version | entry assembly version |
| Informational version | `AssemblyInformationalVersionAttribute` (falls back to `—`) |
| Server time (UTC) | `DateTime.UtcNow` |
| Process uptime | now − process start time (formatted `d/h/m` or `h/m/s`) |

### Edition / license
| Field | Source |
|---|---|
| Edition | `ILicenseVerifier.Evaluate(...).EffectiveEdition` |
| License status | `...Status` (e.g. Valid, GracePeriod, Expired, Invalid) |
| License reason | `...Reason` |
| Enabled feature count | `...Features.Count` |

License inputs are read from configuration: `Licensing:Edition`, `Licensing:ExpiryUtc`,
`Licensing:CustomerId`, `Licensing:CustomerName`. An absent or `Community` edition ⇒ `null` license ⇒
**Community core** (no license file required). Editions: **Community**, **ProfessionalPilot**,
**Enterprise**.

### Service health (cached snapshot only)
| Field | Source |
|---|---|
| Mode | `IServiceHealthCache.Current.ModeLabel` (Minimal / Standard / FullAi) |
| Chat available | `...ChatAvailable` |
| Health checked (UTC) | `...LastCheckedUtc` |
| Qdrant (optional vector store) | snapshot label |
| Ollama (optional local AI) | snapshot label |
| Embeddings (optional) | snapshot label |

No probe is performed by the page; it reflects whatever the background `HealthMonitorService` last
wrote.

### Database facts (guarded counts)
Shown only when the DB is reachable (`SELECT 1` guarded probe). Each count is wrapped so a failure
yields `-1` ("unavailable") instead of failing the page:

- Projects, Knowledge items, Knowledge packs, Code symbols, Imported files, Chat messages, Audit
  events.

### Last import / last audit
| Field | Source |
|---|---|
| Last import | most recent `IngestionJobs` row (file name, status, completed-or-created UTC), `AsNoTracking`, lightweight columns only |
| Last audit | most recent `AuditEvents` row (action, created UTC), `AsNoTracking`, lightweight columns only |

Both are wrapped; on error the tile shows nothing rather than failing.

### Disk (best effort)
| Field | Source |
|---|---|
| Drive | content-root drive name |
| Free GB / Total GB | `DriveInfo` on the content-root drive |

Wrapped in try/catch — the disk tile is best-effort and never breaks the page.

---

## 3. The non-blocking health rule (why it matters)

The platform's core failure mode is a page that hangs waiting on an optional service. The Support page
must be **more** reliable than any other page, because it is the page an operator opens **when
something is already wrong**. It therefore:

- reads health from the in-memory cached snapshot, not from a live probe;
- guards every DB call so a slow or unavailable database degrades a tile, not the page;
- treats disk and last-import/last-audit as best-effort.

The net effect: the Support page renders even when the DB is unreachable, when no optional service is
up, and when no health probe has run yet.

---

## 4. Warnings logic

Derived warnings are computed at the end of the action and surfaced as a list:

| Condition | Warning |
|---|---|
| DB not reachable | "Database is not reachable — core pages depend on MSSQL." |
| License status = GracePeriod | the evaluator's `Reason` |
| License status = Expired or Invalid | the evaluator's `Reason` |
| Disk free `> 0` and `< 5` GB | "Low disk space: {free} GB free on {drive}." |
| Health never probed (`HealthCheckedUtc` is null) | "Optional-service health has not been probed yet (first probe pending)." |

An empty warnings list means the install looks healthy on every dimension the page can cheaply check.

---

## 5. View model

`SupportController.SupportVm` carries: build/version fields; `Mode`, `ChatAvailable`,
`HealthCheckedUtc`, and a `Health` dictionary; `Edition`, `LicenseStatus`, `LicenseReason`,
`EnabledFeatureCount`; `DbReachable`, a `Counts` dictionary, `LastImport`, `LastAudit`; disk fields;
and a `Warnings` list. All fields are plain scalars/dictionaries — no entities and no large text
columns cross into the view.

---

## 6. Access and extension

- The page is a read-only operations view; surface it to admins/support engineers.
- To add a tile: add a guarded read (use the existing `CountAsync` / `TryAsync` helpers), populate the
  view model, and add any derived warning. **Never** add a synchronous external-service call or
  materialise a large text column.
