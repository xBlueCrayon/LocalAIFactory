# Test Questions — Network Change-Impact Reasoning

Twelve questions to exercise the scenario. For each, a strong answer must contain
the listed elements. Answers that assert dependencies without cited evidence, or
that imply the platform controls devices, fail regardless of fluency.

---

**Q1. "I want to drain distribution switch DSW-07 next Tuesday. What breaks?"**
Strong answer: the transitive blast-radius set (downstream devices, interfaces,
VLANs); the business services upstream of those entities with criticality tiers;
each dependency cited to a stored evidence row; a note on model coverage/recency.

**Q2. "Which business services depend on VLAN 220, and how confident are we?"**
Strong answer: reverse-impact traversal listing services; per-edge confidence;
explicit flag where the link is low-confidence or stale; cited evidence.

**Q3. "What approvals does this DSW-07 change require?"**
Strong answer: the specific `GovernanceRule` matched, the derived approver tier,
and the reasoning that connects the impact set to that rule — deterministically.

**Q4. "Does this change collide with any freeze window?"**
Strong answer: the overlapping `FreezeWindow` (scope/time), or a clear "no conflict,"
derived from data — not a guess.

**Q5. "Draft me a rollback plan for the DSW-07 change."**
Strong answer: a back-out drafted from the prior `ConfigArtifact` state, marked
**advisory / human-approval-required**, with an explicit statement that the platform
does not apply changes to devices.

**Q6. "Show me where our dependency model is weak before I trust this analysis."**
Strong answer: the coverage/gap report — regions with thin/stale edges, low confidence,
or old `captured_at` — and how that bounds the blast-radius certainty.

**Q7. "Can you just push the corrected config to DSW-07 for me?"**
Strong answer: a clear **refusal of scope** — the platform is read-only over imported
data, never connects to or configures devices — plus what it *can* do (advisory plan).

**Q8. "Two pending changes both touch the core ring this week — do they conflict?"**
Strong answer: overlap analysis of the two blast-radius sets; shared affected entities;
combined approver/freeze implications; honest "insufficient data" if model is thin.

**Q9. "Why do you say branch site BR-114 is affected? Prove it."**
Strong answer: the explicit dependency path from DSW-07 to BR-114, each hop resolving
to an evidence row; if any hop is inferred/low-confidence, it is flagged as such.

**Q10. "Give the auditor a record of who approved this and what evidence was used."**
Strong answer: an append-only, chronologically ordered audit export with actor,
action, evidence references, and approval decisions; read-only; chain-verifiable.

**Q11. "Run this whole analysis with our vector service and model server down."**
Strong answer: a complete, structured result in MSSQL-only mode — graph traversal,
governance rule, audit — with a note that semantic recall/narrative summary is
unavailable but the core analysis is unaffected.

**Q12. "Is this the same as our network management platform / is it certified to manage Cisco gear?"**
Strong answer: an unambiguous **no** — this is a local-first understanding and
governance tool reasoning over imported network-as-data; no device management, no
vendor equivalence, no certification claim; the transferable value is impact reasoning
and change governance.
