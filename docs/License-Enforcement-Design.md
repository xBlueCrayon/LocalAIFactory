# License Enforcement Design

The implemented edition / license model. This is a **demo-safe placeholder enforcement seam**: the
edition tiers, feature flags, and evaluation rules are real, implemented, and unit-tested, but there
is **no DRM, no phone-home, and no dependency on a license server**. A missing, malformed, or expired
paid license **degrades gracefully to the Community core** — the product never bricks itself or blocks
a demo because a license file is absent.

For the commercial *strategy* (pricing models, what must exist before sale), see
`docs/Edition-and-Licensing-Strategy.md`. For the per-feature tier mapping, see
`docs/Edition-Matrix.md`. This document covers the **code**.

Source: `src/LocalAIFactory.Core/Licensing/Licensing.cs` and
`src/LocalAIFactory.Core/Licensing/LicenseVerifier.cs`. Covered by **8 tests**.

---

## 1. Design principles (enforced in code)

- **No DRM, no phone-home, no license server.** Evaluation is deterministic and offline. `today` is
  **passed in** as a parameter; Core never reaches for the wall clock or the network. This preserves
  the air-gapped, local-first guarantee.
- **Degrade, never block.** A `null`, malformed, or expired paid license collapses to the Community
  core. The product remains fully functional for its proven core.
- **Core features cannot be licensed away.** A fixed set of core features is always unioned into the
  effective feature set, regardless of edition or license validity.
- **Editions gate optional modules only.** Licensing controls optional capability tiers, not the
  correctness of the system of record.

---

## 2. The model

### 2.1 `Edition`

```
Community = 0          // free / dev — proven core only, perpetual
ProfessionalPilot = 1  // a scoped, time-boxed paid pilot
Enterprise = 2         // full module set
```

### 2.2 `LicenseFeature`

Twelve flags. The first four are **always-on core**; the rest are gated modules.

| Feature | Category |
|---|---|
| `CoreRepositoryUnderstanding` | core (always on) |
| `KnowledgeBase` | core (always on) |
| `BenchmarkHarness` | core (always on) |
| `CSharpSqlBridge` | core (always on) |
| `MultiProject` | gated |
| `ErpCrmModule` | gated |
| `CoreBankingModule` | gated |
| `MarketIntelligenceModule` | gated |
| `AutonomousEngineering` | gated |
| `SsoIntegration` | gated |
| `DocumentIntelligence` | gated |
| `PrioritySupport` | gated |

### 2.3 `LicenseStatus`

```
Valid       // within term, or perpetual
GracePeriod // past expiry, within the grace window — still functional, warn the operator
Expired     // past expiry + grace — paid modules off, falls back to Community core
Invalid     // malformed (e.g. paid edition with no customer id) — treated as Community
```

### 2.4 `LicenseInfo` / `LicenseEvaluation`

- `LicenseInfo(Edition, CustomerId, CustomerName, ExpiryUtc?, EnabledFeatures?)` —
  `ExpiryUtc == null` means perpetual. `LicenseInfo.Community()` is the safe default.
- `LicenseEvaluation(Status, EffectiveEdition, Features, Reason)` — the outcome of evaluating a
  license as of a given day, including a human-readable `Reason` for the dashboard.

---

## 3. `LicenseVerifier` rules

`ILicenseVerifier.Evaluate(LicenseInfo?, DateOnly today)` applies these rules in order:

1. **No license** → Community core, `Valid`. ("No license present — running Community edition.")
2. **Community edition** → `Valid`, perpetual, core features.
3. **Paid edition missing customer identity** → `Invalid` → Community core. (Malformed never blocks.)
4. **Paid edition, no expiry** → `Valid`, perpetual (e.g., a perpetual enterprise agreement).
5. **Paid edition, `today <= expiry`** → `Valid` for the licensed edition and features.
6. **Paid edition, within grace** (`today <= expiry + GraceDays`, `GraceDays = 14`) → `GracePeriod`:
   still functional, edition retained, but the reason string tells the operator to renew now.
7. **Paid edition, past grace** → `Expired` → Community core (paid modules off).

### Feature resolution (core can never be lost)

`Resolve(...)` builds the effective feature set from the license's explicit list if it enumerates
one, else the edition's default map — then **unions in the four core features**. So
`CoreRepositoryUnderstanding`, `KnowledgeBase`, `BenchmarkHarness`, and `CSharpSqlBridge` are present
in every evaluation, even `Invalid`/`Expired`.

`IsFeatureEnabled(license, feature, today)` simply evaluates and checks membership — `Expired` and
`Invalid` already collapse to the core set, so gated modules are correctly off.

### Default feature map

- **Community** → core only.
- **ProfessionalPilot** → core + `MultiProject`, `ErpCrmModule`, `CoreBankingModule`,
  `DocumentIntelligence`.
- **Enterprise** → all features.

(The authoritative per-feature table is `docs/Edition-Matrix.md`.)

---

## 4. How it is surfaced

The evaluation's `Status`, `EffectiveEdition`, and human-readable `Reason` are intended to be
surfaced on the **/Support** page, so an operator can see at a glance which edition is active, whether
a license is valid / in grace / expired, and what to do (e.g., "renew now"). Because evaluation is
deterministic and offline, the dashboard never depends on a network call to show license state.

---

## 5. Placeholder seam — what a real deployment adds

This is explicitly an **enforcement seam**, not a finished licensing product:

- A real deployment would **load `LicenseInfo` from a signed license file** (offline-verifiable, no
  internet check), and supply the real `today`. The evaluation rules and feature gating already live
  here and are tested.
- Signature verification, license-file format, and tamper-resistance of the file are **not yet
  implemented** — consistent with `docs/Edition-and-Licensing-Strategy.md` §6, which lists license
  enforcement (signed entitlements, edition gating, graceful lapse, tests) as a prerequisite before
  any commercial sale beyond a paid pilot.
- There is intentionally **no telemetry, no seat counting, and no kill switch**. The air-gapped,
  local-first guarantee is preserved.

**Do not market an edition or licensing term the code cannot enforce.** Today the code enforces the
*evaluation and gating logic*; it does not yet verify a signed license file.
