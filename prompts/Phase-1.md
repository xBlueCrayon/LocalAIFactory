# Phase 1 — Baseline platform (brief)

**Goal:** stand up LocalAIFactory as a private, local-first AI software-engineering platform for
banking middleware — **not** a chatbot — centered on a curated project memory with an approval
lifecycle.

**Scope delivered:**
- Eight-project, acyclic .NET 10 / ASP.NET Core MVC solution: `Core, Data, Rag, Agent, Ingestion,
  Workspaces, Terminal, Web`.
- MSSQL + EF Core, 34-table schema, migration `InitialCreate` + model snapshot, seeding on startup.
- RAG approval lifecycle (Draft/Approved/Deprecated/NeedsReview); approved-first, project-over-generic
  context injection.
- Optional Qdrant (REST) vectors; optional Ollama (`qwen2.5-coder:14b`, `nomic-embed-text` 768-dim).
- Ingestion pipeline (ZIP import, extraction, profiling); Task Profiles for model execution.
- Bootstrap UI; client-side markdown via marked.js.

**Constraints:** no dependency cycles; `Core` dependency-free; complete, buildable, deployable
solution; minimal user input to create the database.
