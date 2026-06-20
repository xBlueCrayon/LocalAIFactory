# LocalAIFactory — POC Demo Script (30–45 minutes)

**Audience:** CEO, CTO, Head of IT, enterprise architect, senior developer, banking operations manager,
auditor, project manager. **Goal:** prove it is real, clean, tested, demonstrable — and honest about limits.

> Setup: start the app locally (`dotnet run --project src/LocalAIFactory.Web`), dev auth provides an admin
> identity. Have a terminal ready for `scripts/poc/verify-poc.ps1` and the benchmark. Keep the readiness page
> open in a tab.

## Flow (with timings)

**0–3 min — Framing.** "This is a local-first, MSSQL-authoritative engineering and knowledge platform. It is a
proof of capability, not a finished product, and not a replacement for SAP/Sage/Oracle/Cisco. Everything I
show is backed by tests, a benchmark, or a live query."

**3–6 min — Security / login / access.** Show Windows-auth concept; the role badge; that admin-only pages
(Users, Audit, pack install) are enforced **server-side** (403 + audit), not just hidden. *Auditor's eye:*
denials are recorded.

**6–9 min — Project-level access.** Show that a non-admin sees only granted projects; direct-URL access to an
ungranted project returns 403. Mention the IDOR regression test that pins cross-project isolation.

**9–13 min — Coverage / gap reporting.** Open a benchmarked/imported project's coverage page. "Every file's
extraction outcome is recorded — extracted, no-symbols, unsupported, parse-error. No silent blind spots."

**13–17 min — Repository graph + impact analysis.** Explore symbols, dependencies. Run an impact query: "what
breaks if this changes?" Show transitive blast radius. *Developer's eye:* this is deterministic, from real code.

**17–22 min — Base Knowledge.** Open `/BaseKnowledge`. Search **OCR**, **Mauritius banking**, **direct debit**,
**IFRS**, **VB6**, **signature forgery**, **PDF summarizer**. Open an item: show description, applicability,
example, **Limitation**, **Sources**, confidence, last-reviewed. "390 curated items, distinct from imported
project knowledge, versioned, never silently overwritten."

**22–25 min — Source registry.** Show that finance/standards/research items are **attributed** to a registry
with governance metadata, `verbatimCopyAllowed=false`, and research families marked *"verification required"*
(no fabricated citations). The installer **rejects** items that cite an unregistered source.

**25–29 min — Benchmark evidence.** Run `tools/LocalAIFactory.Benchmark --inmemory`. Show `Result: PASS` over
4 pinned public repos with Gold/Bronze tiers and golden-snapshot regression detection. "This is how we prove
capability objectively and catch regressions."

**29–32 min — Ingestion robustness.** Explain (or replay) the hostile-repo test: binary-as-`.cs`, oversized,
non-UTF-8, malformed SQL, deep nesting → import **Completed**, correct skip buckets, **no crash**.

**32–36 min — Enterprise scenario suite + Readiness.** Open `enterprise-scenarios/` (banking reconciliation,
cheque-OCR, VB6 migration). Open `/Readiness`: show the honest scorecard — strong core engineering, modest
domain-implementation and commercial scores, with a path-to-100 per area.

**36–40 min — Deployment blueprint.** `deploy/docs/hardware-sizing-guide.md`: MSSQL-only works; GPU optional;
honest limits of the reference workstation.

**40–43 min — What is NOT supported yet.** State plainly: no Python/VB6/Oracle parsing yet; OCR/PDF/forecasting
are knowledge+design, not engines; no SSO/backup/restore/supportability dashboard; not a vendor replacement;
domain content is advisory, not legal/regulatory/financial advice.

**43–45 min — Next roadmap.** C#↔SQL bridge → Python extractor → OCR/PDF prototype → deployment hardening →
estate model. Close: "Real, clean, tested, honest — and a credible path forward."

## Expected hostile questions and honest answers

**Is this real or just generated text?** — Real. The engine deterministically parses actual C#/T-SQL; results
are in MSSQL and reproduced by a benchmark with pinned repos and golden snapshots. I can run it now.

**Where is the data stored?** — MSSQL (SQL Server / LocalDB / Express). MSSQL is the source of truth; vectors
(Qdrant) and models (Ollama) are optional accelerators. It works with only SQL Server present.

**How do we know it isn't hallucinating?** — The structural engine is deterministic, not generative — symbols
and edges come from parsing. Knowledge items are curated and source-attributed. For any future summarization
(PDF), provenance is mandatory: extracted text is separated from model output, with page references and a
document hash.

**What happens if a file can't be parsed?** — It's recorded as a skip/parse-error with a reason and surfaced in
the gap report — never a silent loss, never a crash. We proved this with a deliberately hostile repository.

**Can it understand SAP / Sage / Oracle / Cisco?** — It can *reason* about those domains (knowledge + scenarios)
and understand C#/T-SQL code in such estates. It does **not** parse Oracle PL/SQL yet, does not manage Cisco
devices, and is not an ERP.

**Can it replace SAP / Sage / Oracle / Cisco?** — No, and we don't claim to. It is a code-and-knowledge
intelligence platform that complements such systems.

**Can it detect cheque fraud?** — It can produce a **risk signal with confidence and evidence** and route
high-risk items to human review. It is **never** legally conclusive proof, discloses false-positive/negative
risk, and there is no shipped engine yet — this is knowledge + design today.

**Can it predict financial markets?** — No. It assesses **model risk** and enforces validation/uncertainty
discipline. It does not give financial advice or imply profitability.

**Can it migrate VB6?** — Not automatically — there is no VB6 parser. It provides a strong modernization
**playbook**, assessment method, target architecture, and test strategy.

**Can it safely touch production?** — Not today. It is read-mostly; code-modification workspaces are guarded
off; there is no production deployment path yet. Safe production change is a deliberately gated future stage.

**What would make it enterprise-ready?** — SSO/IdP, backup/restore, supportability dashboard, scale/load
testing, the estate model, and an external security/operational audit — see the readiness scorecard.

**What is the biggest gap?** — Converting strong domain *knowledge/design* into delivered, tested vertical
*slices*, and broadening language coverage (Python, legacy). The C#↔SQL bridge is the first proof.
