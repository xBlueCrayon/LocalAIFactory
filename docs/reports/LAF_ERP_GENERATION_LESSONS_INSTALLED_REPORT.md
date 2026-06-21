# LAF ERP Generation Lessons — Installed Report

**Date:** 2026-06-21
**Pack:** `laf-erp-generation-lessons-v1` (22 items)
**Files:** `generation-lessons.json`, `manifest.json`, `source-registry.json`

## Purpose

This pack captures the concrete lessons learned while LocalAIFactory generated ERP V1/V2/V3 — generator failure patterns, LLM hallucination patterns, collision guards, module-spec rules, and validation gates — so the generator can avoid re-making the same mistakes.

## Contents (22 items)

By category in `generation-lessons.json`:

| Category | Items |
|----------|------:|
| Generator Failure | 8 |
| LLM Hallucination | 3 |
| Collision Guard | 2 |
| Module Spec | 2 |
| Validation Gate | 2 |
| Parity Scoring | 2 |
| API Generation | 1 |
| UI Generation | 1 |
| Generation Process | 1 |

### Representative lessons

- **Generator Failure** — Minimal-API open-generic binding needs explicit `[FromServices]`/`[FromBody]`; MVC controller/view/route names must agree; template markers must sit on their own line; `EnsureCreated` vs migrations for generated schemas.
- **LLM Hallucination** — proposed entities can collide with or duplicate core engine entities (the reason `Supplier` was rejected in V3).
- **Collision Guard** — reject any proposed entity that overlaps a core engine entity before emission.
- **Module Spec** — rules for authoring `erpnext-grade-modules.json` against `erp-module-spec.schema.json`.
- **Validation Gate** — build-green and tests-green gates that generated output must pass before it counts.

Each item is a structured record (`uid`, `category`, `title`, `knowledgeType`, `description`, `applicability`, `example`, `limitation`, `confidence`, `reviewStatus: approved`, `tags`).

## Installed by default and usable by the generator

- The pack lives under `knowledge-packs/`, so it is **installed by default** with the product.
- It validates cleanly (part of the `verify-all-knowledge-packs.ps1` PASS: 10 packs / 648 items / 648 distinct UIDs / no collisions).
- The lessons are written as generation **rules** so they can be fed back into the generation prompt/context — i.e. usable by the generator to avoid the documented failure and hallucination patterns.

## Honest note

The pack documents and encodes the lessons; it is a knowledge asset, not an automatic fix. Realising the benefit requires the generator to consume these lessons during generation. They are captured, approved, and installed — the wiring of every lesson into every generation path is an ongoing improvement, not a completed one.
