# Prompt Governance

> **Status:** Architecture / governance reference.
> **Authoritative source:** `MASTER_VISION.md`.

## 1. Why prompts are governed

In LocalAIFactory the prompt is the *interface* between an optional local model and the curated
knowledge base. A careless prompt can cause a model to fabricate, to leak unrelated context, or to
propose a change that misrepresents a banking rule. Because the platform's value is **trustworthy,
human-approved knowledge**, prompts that can influence authoritative knowledge are treated as
governed artifacts — versioned, reviewed, and traceable — not as ad-hoc strings.

This complements `LLM-Knowledge-Enhancement-Architecture.md`: prompts produce *proposals*, never
authoritative writes. Governance hardens the proposal-producing step.

## 2. Prompt template registry

The platform already persists named prompts in the **`PromptTemplate`** entity
(`src/LocalAIFactory.Core/Entities/ModelConfiguration.cs`):

```csharp
public class PromptTemplate
{
    public int Id;
    public string Name;          // stable identifier, e.g. "kb.summarize.v3"
    public string Kind;          // e.g. "ChatSystem", "Summarize", "DuplicateDetect"
    public string Content;       // the template body
    public bool IsDefault;
    public DateTime CreatedUtc; UpdatedUtc;
}
```

Governance treats this table as the **registry of record**:

- A prompt used by an automated enhancement path must exist as a `PromptTemplate` row — not inlined in
  C#. This makes the prompt reviewable and referenceable.
- The `Name` is a stable id (recommended convention: `<area>.<purpose>.v<N>`). That id is what gets
  stamped into a proposal's provenance (`ProvenanceEvent.Reason`), so a reviewer can see exactly which
  prompt produced a given suggestion.

## 3. Versioning

- Prompts are **versioned by name**: never silently edit a prompt that has already produced approved
  knowledge. Create a new version (`kb.summarize.v4`) and migrate the default over to it.
- The previously active version is retained so historical proposals remain explainable. This mirrors
  the knowledge base's own append-only stance (`KnowledgeVersion`, `ProvenanceEvent` are never
  mutated/deleted).
- `IsDefault` selects the active version for a `Kind`; flipping it is a reviewed change (§4).

## 4. Review

- A prompt whose output can flow into curated knowledge (summarization, consolidation, weak-item
  rewrite) is **reviewed before it becomes default**, the same way the *output* is reviewed before it
  becomes authoritative. Two gates, not one.
- Review checks: scope (does it only touch the intended `Kind`?), injection-safety (§5), determinism
  expectations (§6), and refusal behaviour (does it instruct the model to omit, not invent, missing
  sources?).
- Prompts that *do not* touch authoritative knowledge (e.g. an interactive scratch chat) are lower
  risk but still benefit from living in the registry.

## 5. Injection safety

Imported artifacts and user text are **untrusted input** and must never be able to rewrite the
instructions:

- Keep a clear separation between the **governed system/template portion** and the **untrusted data
  portion** of every prompt. Untrusted content is inserted as data, clearly delimited, and the
  template instructs the model to treat it as content to analyze — not as commands to follow.
- The template explicitly forbids the model from changing its task, fabricating sources, or emitting
  authoritative claims, regardless of anything embedded in the data.
- Retrieved context injected first (the platform's curated-knowledge-first design) is itself
  approved knowledge; untrusted import text is never elevated to that position.
- Output is parsed defensively: a model that "decides" to approve its own change cannot — approval is
  a human DB action, not a token the model can emit.

## 6. Determinism notes

- Local generation is **not fully deterministic.** Even at low temperature (`ModelConfiguration`
  defaults to `0.2`), identical inputs can yield slightly different text across runs and across model
  digests.
- Implication for governance: pin **temperature**, **token limits**, **model id + digest**, and the
  **prompt version** when reproducibility matters, and record all of them in provenance. Even then,
  treat reproducibility as "close", not "exact".
- Because output varies, **no prompt result is auto-trusted**. Every result is a proposal subject to
  human review (see `AI-Output-Provenance-and-Approval.md`).

## 7. Reasoning-model caveat

`deepseek-r1:14b` is a reasoning model: it emits hidden `<think>` tokens *before* the answer, and
those tokens consume the same output budget.

- Prompts targeting a reasoning model must allocate a larger token budget (`MaxTokens` /
  `num_predict`), or the visible answer can be truncated.
- For short, structured proposals (summaries, dedup verdicts) prefer a non-reasoning model
  (e.g. `qwen2.5-coder:14b`); the helper `scripts/ai/test-installed-model.ps1` deliberately defaults
  away from reasoning models for exactly this reason.
- Strip or ignore `<think>` content when parsing; never store hidden reasoning as if it were the
  answer, and never treat it as a citation or a fact.

## 8. Honest limits

- Governance reduces risk; it does not make a small local model reliable. Output still needs a human.
- A governed prompt cannot prevent a model from being wrong — only from being *unaccountable*. The
  audit trail (prompt id + model digest + provenance) is the real guarantee.
- All of this is optional infrastructure. With no model present, prompts are inert and MSSQL is
  unaffected.
