# Expected Questions — Core-Banking Integration

Pose these to LocalAIFactory against the committed fixture
(`benchmarks/fixtures/core-banking/` + `sample-csharp-middleware/`,
`sample-sftp-contracts/`, `sample-api-contracts/`). Each is tagged **GRAPH-DERIVED**
(answerable from the C#↔SQL dependency graph) or **ADVISORY** (contract/design reasoning,
not a graph proof). See `expected-answers.md` for the strong answers and exact bridge targets.

1. **What code posts a transaction?** — *GRAPH-DERIVED*
   Expect a `dependents("dbo.usp_PostTransaction")` resolution.

2. **What tables are touched by mandate generation?** — *GRAPH-DERIVED*
   Expect a `dependencies("...MandateService.GenerateMandate")` resolution.

3. **What changes if the claim response format changes?** — *GRAPH-DERIVED*
   Expect `dependents("dbo.Claim")` (claim-response handler).

4. **What retry / idempotency logic exists for SFTP file processing?** — *ADVISORY*
   (with a graph anchor on the idempotency control).

5. **What audit trail proves who approved a banking file?** — *ADVISORY*
   (maker/checker + append-only audit; partial graph anchor via `dbo.FileArchive`).

6. **What breaks if a rejection code changes?** — *GRAPH-DERIVED*
   Expect `dependents("dbo.RejectionCode")`.

7. **What controls prevent duplicate posting?** — *GRAPH-DERIVED*
   `UNIQUE IdempotencyKey` + `dbo.usp_PostTransaction` proc guard.
