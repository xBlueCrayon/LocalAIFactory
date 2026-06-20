# Edition Matrix

The feature-by-edition mapping implemented in `src/LocalAIFactory.Core/Licensing/`. This table is the
authoritative view of which `LicenseFeature` is available in each `Edition`, derived directly from
`LicenseVerifier.DefaultFeatures(...)` and the always-on core set.

For the rules behind the mapping (grace window, degrade-to-Community, core-cannot-be-licensed-away),
see `docs/License-Enforcement-Design.md`. For commercial strategy, see
`docs/Edition-and-Licensing-Strategy.md`.

> Demo-safe reminder: there is **no DRM and no license server**. A missing/expired paid license
> degrades to **Community**, which always retains the core features below.

---

## 1. Feature matrix

| LicenseFeature | Category | Community | ProfessionalPilot | Enterprise |
|---|---|:---:|:---:|:---:|
| `CoreRepositoryUnderstanding` | core (always on) | ✅ | ✅ | ✅ |
| `KnowledgeBase` | core (always on) | ✅ | ✅ | ✅ |
| `BenchmarkHarness` | core (always on) | ✅ | ✅ | ✅ |
| `CSharpSqlBridge` | core (always on) | ✅ | ✅ | ✅ |
| `MultiProject` | gated module | — | ✅ | ✅ |
| `ErpCrmModule` | gated module | — | ✅ | ✅ |
| `CoreBankingModule` | gated module | — | ✅ | ✅ |
| `MarketIntelligenceModule` | gated module | — | — | ✅ |
| `AutonomousEngineering` | gated module | — | — | ✅ |
| `SsoIntegration` | gated module | — | — | ✅ |
| `DocumentIntelligence` | gated module | — | ✅ | ✅ |
| `PrioritySupport` | gated module | — | — | ✅ |

Legend: ✅ = enabled by the edition's default feature map; — = not enabled by default.

> Source of truth: `LicenseVerifier.DefaultFeatures(...)`. Community = core only;
> ProfessionalPilot = core + `MultiProject`, `ErpCrmModule`, `CoreBankingModule`,
> `DocumentIntelligence`; Enterprise = all features. The four core features are always unioned in,
> so they cannot be licensed away.

---

## 2. Notes and honesty caveats

- **Core is always on.** The four core features are present in every edition and even when a paid
  license is missing, malformed, or expired. The proven core (repository understanding, knowledge
  base, benchmark harness, C#↔SQL bridge) cannot be paywalled.
- **Explicit overrides.** A `LicenseInfo` may enumerate `EnabledFeatures` explicitly; when it does,
  that set is used (still unioned with the core). The table above shows the **default** map per
  edition when no explicit list is supplied.
- **"Available by edition" ≠ "fully built".** This matrix reflects the **licensing gate**, not the
  maturity of each module. Several gated modules are early/planned (e.g., SSO/IdP is Windows-auth
  only today; estate-level and autonomy capabilities have known limitations). A feature being
  enabled by an edition does **not** assert it is production-complete — see `docs/Known-Limitations.md`.
- **Optional AI is never a paywall lever that breaks local-first.** The product must remain fully
  functional in MSSQL-only mode at every edition; optional Ollama/Qdrant are gated by config, not by
  license tier, and never block the system of record.

---

## 3. Edition summary

| Edition | Intended audience | Default capability |
|---|---|---|
| **Community** | Free / development | Proven core only, perpetual |
| **ProfessionalPilot** | A scoped, time-boxed paid pilot | Core + MultiProject, ERP/CRM, Core-Banking, Document-Intelligence modules |
| **Enterprise** | Estate-wide | All features (adds Market-Intelligence, Autonomous-Engineering, SSO, Priority-Support) |
