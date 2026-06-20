# Expected Questions — KYC → Transaction Approval

The questions an analyst or operating manager would ask, each mapped to a bridge query the
committed `KYCAML` fixture answers.

1. Where is the transaction-approval procedure defined? — `find("dbo.usp_ApproveTransaction")`
2. What code submits a transaction for approval? — `dependents("dbo.usp_SubmitTransactionForApproval")`
3. What code applies the checker decision (approves a transaction)? — `dependents("dbo.usp_ApproveTransaction")`
4. What code touches the AML alert table? — `dependents("dbo.AmlAlert")`
5. What does the screening service depend on? — `dependencies("KycAml.Services.ScreeningService.ScreenCustomer")`
6. What does the approval method depend on? — `dependencies("KycAml.Services.TransactionApprovalService.ApproveTransaction")`
7. What breaks in the C# services if the Customer table changes? — `impact("dbo.Customer")`

Advisory (not graph-derived) questions, answered from the operating manual / matrices:

- What is the approval limit for a Medium-risk customer? — see `approval-matrix.md`.
- Does the workflow enforce maker ≠ checker? — proc-level yes; full segregation is config-dependent,
  see `controls-matrix.md`.
