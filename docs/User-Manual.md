# User Manual

For analysts and viewers who use LocalAIFactory to understand a banking-middleware codebase and
the curated knowledge built around it. This manual covers what the platform does, how to sign in,
how to navigate, how to ask questions, and — just as importantly — what the platform does **not**
claim.

LocalAIFactory is a **local-first, MSSQL-authoritative** platform. Everything you see is served from
SQL Server. Pages load without waiting on an external service, and the system works even when no AI
model is present.

---

## 1. What the system does

- **Imports** projects (source, SQL, docs, exported AI chats) and turns them into structured,
  linked, curated knowledge.
- **Understands** C#/.NET, T-SQL, and Python code at a structural level — symbols, references,
  dependencies, and the bridge between code and SQL.
- **Answers questions** in Chat, grounded first in approved knowledge, with sources and confidence
  attached.
- **Shows impact** — "what breaks if this changes" — across the C#↔SQL and Python↔SQL bridges.
- **Curates knowledge** through an approval lifecycle so that the trusted memory grows over time.

It is **not** a chatbot, a stateless code generator, or a search box. Conversation is one interface;
the durable, governed memory is the product.

---

## 2. Signing in and what you can see

- You are authenticated automatically with your Windows account (`DOMAIN\user`) via Negotiate.
  In a developer environment a development identity is used instead.
- Roles form a total order: **Viewer (0) < Analyst (1) < Admin (2)**. Each higher role inherits the
  lower role's capabilities.
  - **Viewer** — read-only on projects you have been granted.
  - **Analyst** — Viewer, plus importing repositories and running consolidation on granted projects.
  - **Admin** — full control (covered in the Admin Manual).
- Access is **deny-by-default**. A new account is a Viewer with no project grants and therefore sees
  no projects until an administrator grants access.
- If you open a project you have not been granted, you get an HTTP **403** and the denial is audited.
  Request access from an administrator (the Users & Access page).

---

## 3. Navigating

The left navigation groups the pages by purpose:

| Area | Pages | What you use it for |
|---|---|---|
| Overview | Home / Dashboard | Counts, mode, recent activity, entry points |
| Ask | Chat | Ask questions grounded in approved knowledge |
| Projects | Projects, Import Project, Imports, Project Profiles | Your granted projects and their imports |
| Knowledge | Base Knowledge, Knowledge, Business Rules, Approved Code, Code Candidates | The curated memory and its lifecycle |
| Graph | Knowledge Graph, Explore Graph | Relationships, dependencies, impact |
| Quality | Readiness, Benchmarks | Honest maturity and validation status |
| Ops | Support | Read-only health/version dashboard |
| Admin | Users & Access, Audit Trail | Admin-only (see Admin Manual) |
| Runtime | Models, Task Profiles, Agent Tasks, Workspaces | Optional AI configuration and tasks |

The Home dashboard shows the current **environment mode**: *Minimal* (MSSQL only), *Standard*
(MSSQL + one optional service), or *FullAi* (MSSQL + Ollama + Qdrant). The mode tells you which
optional capabilities are available right now.

---

## 4. Asking questions in Chat

1. Open **Chat** and select the project context you want (or the global context for Base Knowledge).
2. Type your question in plain language.
3. The platform assembles relevant, trustworthy knowledge first — **approved** knowledge is injected
   first and weighted highest; **project-specific** knowledge overrides generic knowledge.
4. The answer arrives with its supporting **sources** and a **confidence** indication where available.

If no local model is configured, Chat that requires a model is unavailable, but every other page
still works — the curated memory does not depend on a model being present. The Home dashboard and the
Support page both indicate whether chat/AI is currently available.

**Treat AI answers as advisory.** They can be wrong or incomplete and require your verification
before you act on them. The platform's system of record (MSSQL) never depends on AI quality.

---

## 5. Browsing projects and imports

- **Projects** lists the projects granted to you. Open one to see its summary, imported files,
  coverage/gap report, and graph.
- Deep structural understanding covers **C#/.NET, T-SQL, and Python**. Files in other languages are
  imported but reported as honest **gaps** — they are never silently counted as understood.
- **Imports** shows the status of each import job. **Project Profiles** shows profiling output for a
  project.

---

## 6. Exploring the code / SQL graph

- Open **Explore Graph** (or the project graph) to see the structural picture.
- Nodes are **symbols** (classes, methods), **SQL objects** (tables, stored procedures, views), and
  Python constructs; edges are **references and dependencies**.
- The platform bridges code and data:
  - **C#↔SQL bridge** — SQL embedded in C# is resolved to schema symbols.
  - **Python↔SQL bridge** — the same idea for Python data access.
- Use the graph to answer "what calls this", "what does this depend on", and "how does this C# touch
  that table or stored procedure".

---

## 7. Impact analysis — "what breaks if X changes"

This is the core analyst workflow and it is **deterministic and MSSQL-only** — it does not need a
model or vectors.

1. Pick a symbol, table, stored procedure, or function.
2. Run **impact analysis** to get its **blast radius** — everything that would be affected.
3. The analysis works in **both directions** across the bridges:
   - Change a **stored procedure or table** → see the **C#/Python** that would break.
   - Change a **C#/Python method** → see the **SQL** it reaches.
4. Each result carries **confidence and evidence** so you can judge how sure the platform is.

---

## 8. Base Knowledge and the knowledge pages

- **Base Knowledge** is the curated baseline — professional summaries across software engineering,
  databases, security, governance, finance/accounting, and banking-operations topics. Optional
  add-on packs (financial-institution-operations, KYC/AML, market-intelligence-forecasting) may be
  installed by an admin.
- Each item shows its **sources**, **jurisdiction** where relevant, and an explicit **limitation
  note**. Items are awareness-level only.
- **Knowledge**, **Business Rules**, **Approved Code**, and **Code Candidates** show the curated
  memory through its lifecycle: **Draft → NeedsReview → Approved → Deprecated/Superseded**. Approved
  items are the trusted, first-injected memory. Candidates are machine-extracted and awaiting review.
- Imported **project knowledge** is kept distinct from the curated baseline, so you always know
  whether an item came from a curated pack or from your own repository.

---

## 9. Inspecting evidence: graph, sources, confidence, coverage

- **Coverage / gap** — every file's extraction outcome is recorded. Skipped files are bucketed
  (binary, oversized, non-UTF-8, malformed, unsupported language) and shown honestly. **There are no
  silent zeros** — a gap is reported, not hidden as success.
- **Confidence** — impact and bridge results carry a confidence level. Higher confidence means
  stronger structural evidence; lower confidence means a heuristic match to confirm.
- **Evidence** — results link back to the symbols, edges, or SQL references that justify them, so you
  can trace *why* the platform reached a conclusion rather than trusting it blindly.

Treat confidence and evidence as a guide, not a guarantee. Verify high-stakes conclusions against the
evidence before acting.

---

## 10. Readiness

The **Readiness** page is an honest, self-reported maturity scorecard. It is deliberately candid
about what is proven and what is not — it does not inflate the platform's status. Use it to calibrate
how much to rely on any given capability. The **Benchmarks** page shows the validation harness status
against fixtures.

---

## 11. What this system does NOT claim

- Understanding is **syntactic / structural**, not a full semantic or runtime model.
- Only **C#/T-SQL/Python** are deeply understood; other languages are reported gaps.
- There is **no cross-repository estate model** — each project is understood independently.
- **OCR, cheque, PDF, and forecasting** features are prototypes / design notes, not shipped engines.
- Base Knowledge domain content is **advisory / awareness only** — **not legal, regulatory, tax,
  audit, financial, or investment advice**, and not a compliance or certification claim.
- AI outputs are advisory and unverified by default — they require your judgment.

If something looks wrong or missing, it is more likely a reported gap than a hidden failure. Check
the coverage report and the evidence, and raise it with your administrator.
