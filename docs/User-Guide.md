# User Guide

For analysts and viewers using LocalAIFactory to understand a banking-middleware codebase. You sign in with
your Windows identity; you see only the projects an administrator has granted you. Everything you see is
served from MSSQL — pages load in under a second and never wait on an external service.

---

## 1. Signing in and what you can see

- You are authenticated automatically with your Windows account (`DOMAIN\user`).
- **Viewer** — read-only access to granted projects. **Analyst** — query/explore granted projects.
- If you open a project you have not been granted, you get a **403** (and it is audited). Ask an admin for
  access via `/Users`.

---

## 2. Dashboard

The **dashboard** (home) is your overview: projects you can reach, recent activity, and entry points into
exploration, coverage, and Base Knowledge. It loads fast in MSSQL-only mode and never blocks on Ollama or
Qdrant (their status, if configured, comes from a cached health snapshot).

---

## 3. Browse projects

- The **Projects** page lists the projects granted to you.
- Open a project to see its summary, imported files, coverage/gap report, and graph.
- Supported languages for deep understanding are **C#/.NET, T-SQL, and Python**. Files in other languages are
  imported but reported as honest **gaps** — they are not silently counted as understood.

---

## 4. Explore the code / SQL graph

- Open the **graph explorer** for a project.
- Nodes are **symbols** (classes, methods), **SQL objects** (tables, stored procedures, views), and Python
  constructs; edges are **references and dependencies** (the `CodeEdge` graph).
- The platform bridges code and data:
  - **C#↔SQL bridge** — SQL embedded in C# is resolved to schema symbols (`AccessesSql` edges).
  - **Python↔SQL bridge** — the same idea for Python data access.
- Use the graph to answer "what calls this," "what does this depend on," and "how does this C# touch that
  table or stored procedure."

---

## 5. Run impact analysis — "what breaks if X changes"

This is the core analyst workflow.

1. Pick a symbol, table, stored procedure, or function.
2. Run **impact analysis** to get its **blast radius** — everything that would be affected if it changes.
3. The analysis works in **both directions** across the bridges:
   - Change a **stored procedure or table** → see the **C#/Python** that would break.
   - Change a **C#/Python method** → see the **SQL** it reaches.
4. Each result carries **confidence and evidence** so you can judge how sure the platform is (see §7).

This is deterministic and MSSQL-only — it does not require a model or vectors.

---

## 6. Search Base Knowledge (including domain knowledge)

- Open **Base Knowledge** to search the Professional Base Knowledge Pack: **390 curated items across 22
  categories**.
- Categories include software engineering, CRUD/MSSQL, databases, security, governance, finance/accounting,
  **Mauritius banking**, payments, leasing, insurance, cheque OCR/forgery, Python OCR/CV, PDF intelligence,
  financial-market model risk, source-attributed research, and enterprise playbooks.
- Each item shows its **sources** (`src:` tags), **jurisdiction** where relevant (`jur:` tags), and an
  explicit **limitation note**.
- Imported **project knowledge** is kept distinct from this curated baseline, so you always know whether an
  item came from the curated pack or from your own repository.

---

## 7. Coverage, gap, confidence, and evidence

- **Coverage / gap** — every file's extraction outcome is recorded. Skipped files are bucketed (binary,
  oversized, non-UTF-8, malformed, unsupported language) and shown honestly. **There are no silent zeros** —
  a gap is reported as a gap, not hidden as success.
- **Confidence** — impact and bridge results carry a confidence level. Higher confidence means stronger
  structural evidence; lower confidence means a heuristic match you should confirm.
- **Evidence** — results link back to the symbols, edges, or SQL references that justify them, so you can
  trace *why* the platform reached a conclusion rather than trusting it blindly.

Treat confidence and evidence as a guide, not a guarantee — verify high-stakes conclusions against the
evidence before acting.

---

## 8. Limitations to keep in mind

- Understanding is **syntactic / structural**, not a full semantic model — it reads the code's structure, not
  its runtime behaviour.
- Only **C#/T-SQL/Python** are deeply understood; other languages are reported gaps.
- There is **no cross-repository estate model** — each project is understood independently.
- **OCR, cheque, PDF, and forecasting** features are prototypes/design, not shipped engines — do not rely on
  them for production output.
- Base Knowledge domain content is **advisory / awareness only** — **not legal, regulatory, tax, audit, or
  financial advice**, and not a compliance or certification claim. Financial-market content is analysis
  framing, **not financial advice**.

If something looks wrong or missing, it is more likely a reported gap than a hidden failure — check the
coverage report and the evidence, and raise it with your admin.
