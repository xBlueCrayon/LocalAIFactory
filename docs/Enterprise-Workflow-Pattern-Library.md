# Enterprise Workflow Pattern Library

Original, concise reference for the 40 enterprise-workflow families shipped in the
`enterprise-workflows-v1` knowledge pack. Each family lists its lifecycle states, roles,
validations, approvals, audit, exception paths, a DB-model sketch over the
`workflow-core` schema, a key service rule, and the anti-pattern to avoid.

All content is original practical guidance. It is not a regulatory control attestation,
not legal/compliance advice, and not a substitute for institution policy or scheme rules.
Fixtures referenced here are synthetic prototypes, not a deployed workflow engine.

| # | Family | Key | Initial -> Terminal states |
|---|--------|-----|----------------------------|
| 1 | Maker / checker / approver workflow | `maker-checker-approver` | Draft (initial) -> PendingCheck -> PendingApproval -> Approved/Rejected (terminal); Returned re-enters Draft. |
| 2 | Four-eyes (dual control) approval | `four-eyes-approval` | Pending (initial) -> AwaitingSecond -> Authorized/Declined/Expired (terminal). |
| 3 | Multi-level approval matrix | `multi-level-approval-matrix` | Submitted (initial) -> AtTier{n} -> FullyApproved/Rejected (terminal). |
| 4 | Transaction release / batch release | `transaction-release` | Captured (initial) -> Held -> Released/Cancelled (terminal); Expired if past cut-off. |
| 5 | Payment authorization | `payment-authorization` | Initiated (initial) -> PendingAuth -> Screened -> Authorized -> Settled / Rejected / Held (terminal Settled/Rejected). |
| 6 | Direct-debit mandate lifecycle | `direct-debit-mandate-lifecycle` | Created (initial) -> Active -> Amended -> Cancelled/Expired (terminal); Suspended on dispute. |
| 7 | Claim generation and response matching | `claim-generation-response` | Raised (initial) -> AwaitingResponse -> Accepted/Rejected/TimedOut (terminal). |
| 8 | Rejection-code mapping and handling | `rejection-code-mapping` | Received (initial) -> Mapped -> AutoRetried/ManualReview/ReturnedToCustomer (terminal). |
| 9 | KYC customer onboarding | `kyc-onboarding` | Initiated (initial) -> DocumentsCollected -> Verified -> ComplianceReview -> Approved/Rejected (terminal). |
| 10 | Customer due diligence / enhanced due diligence | `cdd-edd` | Pending (initial) -> CDDComplete -> EDDRequired -> EDDComplete -> Cleared/Escalated (terminal Cleared). |
| 11 | AML transaction screening and monitoring | `aml-screening` | Generated (initial) -> UnderReview -> ClosedFalsePositive/EscalatedToCase/Reported (terminal). |
| 12 | Sanctions / PEP / adverse-media screening | `sanctions-pep-adverse-media` | Screened (initial) -> PotentialMatch -> Reviewed -> Cleared/Blocked (terminal). |
| 13 | Customer risk scoring | `customer-risk-scoring` | Computed (initial) -> Banded -> Overridden/Confirmed (terminal). |
| 14 | Account opening | `account-opening` | Requested (initial) -> KYCCleared -> Configured -> Active/Rejected (terminal). |
| 15 | Document collection and verification | `document-collection-verification` | Requested (initial) -> Uploaded -> Verified/Rejected -> Accepted (terminal). |
| 16 | Cheque OCR review | `cheque-ocr-review` | Captured (initial) -> OCRDone -> NeedsReview -> Confirmed/Rejected (terminal). |
| 17 | Signature / forgery review | `signature-forgery-review` | Submitted (initial) -> Scored -> UnderReview -> Verified/Queried/SuspectedForgery (terminal Verified). |
| 18 | Exception queue management | `exception-queue` | Open (initial) -> Assigned -> InProgress -> Resolved/Escalated (terminal Resolved). |
| 19 | Incident management | `incident-management` | Reported (initial) -> Acknowledged -> Mitigating -> Resolved -> Closed (terminal); Reopened re-enters. |
| 20 | Change request management | `change-request` | Submitted (initial) -> Assessed -> Approved -> Implemented -> Reviewed/Closed (terminal); RolledBack on failure. |
| 21 | SLA escalation | `sla-escalation` | Within (initial) -> Warning -> Breached -> Escalated -> Resolved (terminal). |
| 22 | Asset intervention / field service | `asset-intervention` | Raised (initial) -> Dispatched -> InProgress -> Completed/Cancelled (terminal); OnHold for parts. |
| 23 | Parts consumption | `parts-consumption` | Requested (initial) -> Issued -> Consumed/Returned (terminal). |
| 24 | Inventory adjustment | `inventory-adjustment` | Counted (initial) -> VarianceIdentified -> PendingApproval -> Posted/Rejected (terminal). |
| 25 | Purchase-order approval | `purchase-order-approval` | Drafted (initial) -> Submitted -> AtTier{n} -> Approved -> Issued/Rejected (terminal). |
| 26 | Invoice approval (3-way match) | `invoice-approval` | Received (initial) -> Matched -> PendingApproval -> ApprovedForPayment/Disputed (terminal Approved). |
| 27 | GL posting and finance approval | `gl-posting-finance-approval` | Drafted (initial) -> PendingReview -> Posted/Rejected (terminal); Reversed via new entry. |
| 28 | Support ticket workflow | `support-ticket` | New (initial) -> Assigned -> InProgress -> Resolved -> Closed (terminal); Reopened re-enters. |
| 29 | CRM lead-to-opportunity | `crm-lead-to-opportunity` | New (initial) -> Qualified -> Converted -> Won/Lost (terminal). |
| 30 | ERP order-to-cash | `erp-order-to-cash` | OrderCreated (initial) -> CreditCleared -> Fulfilled -> Invoiced -> Settled (terminal); OnCreditHold. |
| 31 | ERP procure-to-pay | `erp-procure-to-pay` | Requisitioned (initial) -> Ordered -> Received -> Invoiced -> Paid (terminal). |
| 32 | Operational dashboard review | `operational-dashboard-review` | Generated (initial) -> Reviewed -> ActionsAssigned/Acknowledged (terminal Acknowledged). |
| 33 | Daily ops-manager review | `daily-ops-manager-review` | Pending (initial) -> InReview -> SignedOff (terminal); Escalated for carry-over. |
| 34 | Audit evidence export | `audit-evidence-export` | Requested (initial) -> Approved -> Generated -> Delivered (terminal); Rejected. |
| 35 | Backup / restore / rollback | `backup-restore-rollback` | Planned (initial) -> Approved -> Executed -> Verified (terminal); Failed -> Escalated. |
| 36 | Release approval | `release-approval` | Built (initial) -> Tested -> Approved -> Deployed -> Verified (terminal); RolledBack on failure. |
| 37 | Autonomous patch proposal (AI-suggested fix) | `autonomous-patch-proposal` | Proposed (initial) -> UnderReview -> Approved -> Applied/Rejected (terminal). |
| 38 | Human approval before AI code change | `human-approval-before-ai-code-change` | Requested (initial) -> AwaitingApproval -> Approved -> Committed/Rejected (terminal). |
| 39 | Knowledge import / dedupe / approval | `knowledge-import-dedupe-approval` | Imported (initial) -> Deduplicated -> PendingApproval -> Approved/Rejected (terminal). |
| 40 | Production incident postmortem | `production-incident-postmortem` | Draft (initial) -> InReview -> Approved -> ActionsTracked -> Closed (terminal). |

## 1. Maker / checker / approver workflow

- **Family key:** `maker-checker-approver`
- **States:** Draft (initial) -> PendingCheck -> PendingApproval -> Approved/Rejected (terminal); Returned re-enters Draft.
- **Roles:** Maker (initiates), Checker (independent verification), Approver (final authorization), Auditor (read-only review).
- **Required data:** Entity payload, monetary or sensitive fields, maker identity, timestamps, reason notes.
- **Validations:** Maker cannot equal checker; mandatory fields present; value within maker limit; duplicate detection.
- **Approvals:** Stage 1 Make, Stage 2 Check (independent user), Stage 3 Approve when above threshold.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Returned-for-correction loops to Maker; rejected with reason terminates; recall before check allowed.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'maker-checker-approver'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Segregation of duties, value limits per role, full audit event per transition, no self-approval.
- **Anti-pattern to avoid:** Allowing the same user to make and check by reusing a shared service account.

## 2. Four-eyes (dual control) approval

- **Family key:** `four-eyes-approval`
- **States:** Pending (initial) -> AwaitingSecond -> Authorized/Declined/Expired (terminal).
- **Roles:** Initiator, Second Authorizer (must differ from initiator), Auditor.
- **Required data:** Action descriptor, target record, both actor identities, decision timestamps.
- **Validations:** Two distinct authenticated users; both within authority; quorum of 2 met before commit.
- **Approvals:** Single approval stage requiring two independent authorizations before the action executes.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Timeout without second authorization expires the request; either party may decline with reason.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'four-eyes-approval'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Dual control enforced server-side, distinct-actor check, audit of both signatures, idempotent commit.
- **Anti-pattern to avoid:** Counting two browser sessions of one identity as two eyes.

## 3. Multi-level approval matrix

- **Family key:** `multi-level-approval-matrix`
- **States:** Submitted (initial) -> AtTier{n} -> FullyApproved/Rejected (terminal).
- **Roles:** Requestor, Tier approvers (L1..Ln) resolved from an authority matrix, Delegate approvers.
- **Required data:** Request amount/category, cost centre, approval matrix rows (threshold, role, sequence).
- **Validations:** Resolve required tiers from amount/category; each tier within its band; no skipped mandatory tier.
- **Approvals:** Sequential or parallel tiers; higher amount adds tiers; each tier approves or rejects.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Delegation when approver absent; escalation on timeout; rejection at any tier stops the chain.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'multi-level-approval-matrix'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Matrix-driven routing (no hardcoded approvers), per-tier audit, delegation logged, threshold enforced.
- **Anti-pattern to avoid:** Hardcoding approver user IDs in code instead of resolving from the matrix table.

## 4. Transaction release / batch release

- **Family key:** `transaction-release`
- **States:** Captured (initial) -> Held -> Released/Cancelled (terminal); Expired if past cut-off.
- **Roles:** Capturer, Releaser (independent), Operations supervisor.
- **Required data:** Held transaction/batch, captured totals, control totals, release window, cut-off time.
- **Validations:** Captured count and amount equal control totals; within cut-off; releaser <> capturer.
- **Approvals:** Hold after capture, independent release before settlement cut-off.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Partial release blocked; hold past cut-off rolls to next window; recall before release.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'transaction-release'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Control-total reconciliation, cut-off enforcement, independent release, audit of release.
- **Anti-pattern to avoid:** Auto-releasing on capture, removing the independent release gate to hit a deadline.

## 5. Payment authorization

- **Family key:** `payment-authorization`
- **States:** Initiated (initial) -> PendingAuth -> Screened -> Authorized -> Settled / Rejected / Held (terminal Settled/Rejected).
- **Roles:** Payment initiator, Authorizer(s) by limit, Sanctions/compliance gate, Settlement operator.
- **Required data:** Beneficiary, amount, currency, account, value date, mandate reference, screening result.
- **Validations:** Account active and funded, beneficiary valid, amount within limit, screening clear, no duplicate.
- **Approvals:** Limit-based authorization tiers plus mandatory screening pass before queueing for settlement.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Screening hit routes to compliance hold; insufficient funds rejects; recall before settlement.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'payment-authorization'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Limit checks, sanctions gate, duplicate detection, idempotency key, full payment audit trail.
- **Anti-pattern to avoid:** Authorizing payment before sanctions screening completes.

## 6. Direct-debit mandate lifecycle

- **Family key:** `direct-debit-mandate-lifecycle`
- **States:** Created (initial) -> Active -> Amended -> Cancelled/Expired (terminal); Suspended on dispute.
- **Roles:** Customer, Creditor/originator, Bank operator, Dispute handler.
- **Required data:** Mandate reference, debtor account, creditor id, scheme, signature/consent, status, amendment history.
- **Validations:** Valid consent captured, unique mandate reference, account reachable on scheme, amendment authorized.
- **Approvals:** Mandate setup verification, amendment approval, cancellation confirmation.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Refund/indemnity claim path, expired mandate, revoked consent, returned collection.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'direct-debit-mandate-lifecycle'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Consent evidence retained, scheme rule compliance, amendment audit, refund window enforced.
- **Anti-pattern to avoid:** Collecting against a cancelled or expired mandate.

## 7. Claim generation and response matching

- **Family key:** `claim-generation-response`
- **States:** Raised (initial) -> AwaitingResponse -> Accepted/Rejected/TimedOut (terminal).
- **Roles:** Claim originator, Counterparty/scheme, Reconciliation operator.
- **Required data:** Claim message, original transaction reference, claim reason, response code, deadlines.
- **Validations:** Original transaction exists, reason valid for scheme, within claim window, response matches claim.
- **Approvals:** Claim raised, counterparty responds (accept/reject), reconciliation confirms.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** No response by deadline auto-closes, partial acceptance, re-claim path.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'claim-generation-response'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Reference integrity, deadline timers, response-to-claim matching, audit of each exchange.
- **Anti-pattern to avoid:** Closing a claim without matching the response to the originating transaction.

## 8. Rejection-code mapping and handling

- **Family key:** `rejection-code-mapping`
- **States:** Received (initial) -> Mapped -> AutoRetried/ManualReview/ReturnedToCustomer (terminal).
- **Roles:** Processing engine, Operations analyst, Customer-facing agent.
- **Required data:** Raw reason from scheme/host, mapped internal code, retryable flag, customer message template.
- **Validations:** Every raw code maps to a known internal code; unknown codes flagged; retryable computed correctly.
- **Approvals:** Mapping table changes are approved; automated handling for retryable, manual for non-retryable.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Unmapped code routes to manual triage; non-retryable returns to customer with reason.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'rejection-code-mapping'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Versioned mapping table, no silent drop of unknown codes, audit of mapping decisions.
- **Anti-pattern to avoid:** Swallowing unrecognized rejection codes as generic success.

## 9. KYC customer onboarding

- **Family key:** `kyc-onboarding`
- **States:** Initiated (initial) -> DocumentsCollected -> Verified -> ComplianceReview -> Approved/Rejected (terminal).
- **Roles:** Customer, Onboarding officer, Compliance reviewer, Approver.
- **Required data:** Identity documents, proof of address, beneficial ownership, risk inputs, verification results.
- **Validations:** Mandatory documents present and valid, identity verified, screening run, risk rating assigned.
- **Approvals:** Officer captures, compliance reviews, approver activates account; EDD adds a senior approval.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Missing-document hold, failed verification reject, escalation to EDD on high risk.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'kyc-onboarding'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Document verification evidence, screening linkage, risk rating recorded, four-eyes activation.
- **Anti-pattern to avoid:** Activating an account before identity verification and screening complete.

## 10. Customer due diligence / enhanced due diligence

- **Family key:** `cdd-edd`
- **States:** Pending (initial) -> CDDComplete -> EDDRequired -> EDDComplete -> Cleared/Escalated (terminal Cleared).
- **Roles:** Relationship officer, Compliance analyst, MLRO/senior approver.
- **Required data:** Risk factors, source of funds/wealth, beneficial ownership, PEP indicators, review cadence.
- **Validations:** Risk level drives diligence depth; EDD triggers captured; source-of-funds evidence for high risk.
- **Approvals:** Standard CDD officer sign-off; EDD requires senior/MLRO approval and periodic review.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Incomplete diligence holds relationship; adverse findings escalate; periodic re-review due dates.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'cdd-edd'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Risk-based depth, evidence retention, senior approval for EDD, scheduled re-review, audit.
- **Anti-pattern to avoid:** Treating all customers with one fixed diligence depth regardless of risk.

## 11. AML transaction screening and monitoring

- **Family key:** `aml-screening`
- **States:** Generated (initial) -> UnderReview -> ClosedFalsePositive/EscalatedToCase/Reported (terminal).
- **Roles:** Screening engine, AML analyst, MLRO, SAR/STR filer.
- **Required data:** Transaction stream, screening rules/thresholds, alerts, disposition notes, case linkage.
- **Validations:** Rules applied consistently, alert thresholds calibrated, alerts dispositioned with rationale.
- **Approvals:** Analyst dispositions alert; MLRO approves SAR/STR filing decision.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** False positive closed with reason, true hit creates case, threshold tuning approved.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'aml-screening'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Auditable disposition, no auto-close without reason, MLRO sign-off on reporting, tuning governance.
- **Anti-pattern to avoid:** Bulk auto-closing alerts to clear backlog without recorded rationale.

## 12. Sanctions / PEP / adverse-media screening

- **Family key:** `sanctions-pep-adverse-media`
- **States:** Screened (initial) -> PotentialMatch -> Reviewed -> Cleared/Blocked (terminal).
- **Roles:** Screening service, Compliance reviewer, Senior approver.
- **Required data:** Customer/counterparty names, list versions, match scores, review decisions, evidence.
- **Validations:** Current list version used, fuzzy-match scored, potential hits reviewed before clearing.
- **Approvals:** Reviewer confirms or clears match; senior approval to override a true match.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Potential match holds the action, confirmed hit blocks and escalates, list-update re-screen.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'sanctions-pep-adverse-media'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** List versioning, match evidence retained, no clearance without review, override audit.
- **Anti-pattern to avoid:** Clearing a name match automatically below an arbitrary score with no human review.

## 13. Customer risk scoring

- **Family key:** `customer-risk-scoring`
- **States:** Computed (initial) -> Banded -> Overridden/Confirmed (terminal).
- **Roles:** Risk model/engine, Risk analyst, Model governance approver.
- **Required data:** Risk factors (geography, product, channel, behaviour), weights, score, band, rationale.
- **Validations:** Inputs complete, weights from approved model version, score maps to a defined band.
- **Approvals:** Model version and weight changes approved; manual overrides require justification.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Missing factor defaults to conservative band, override with reason, periodic recalibration.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'customer-risk-scoring'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Versioned model, explainable factors, override audit, no opaque hardcoded score.
- **Anti-pattern to avoid:** Hardcoding a customer to low risk to bypass enhanced controls.

## 14. Account opening

- **Family key:** `account-opening`
- **States:** Requested (initial) -> KYCCleared -> Configured -> Active/Rejected (terminal).
- **Roles:** Customer, Onboarding officer, KYC/compliance, Approver.
- **Required data:** Product selection, customer profile, KYC outcome, initial funding, account parameters.
- **Validations:** KYC cleared, product eligibility met, mandatory parameters set, duplicate customer check.
- **Approvals:** Officer creates, compliance confirms KYC, approver activates.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** KYC pending holds opening, ineligibility rejects, duplicate routes to merge review.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'account-opening'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** KYC dependency enforced, eligibility rules, four-eyes activation, full audit.
- **Anti-pattern to avoid:** Opening and funding an account while KYC is still pending.

## 15. Document collection and verification

- **Family key:** `document-collection-verification`
- **States:** Requested (initial) -> Uploaded -> Verified/Rejected -> Accepted (terminal).
- **Roles:** Customer/uploader, Verifier, Approver, Records officer.
- **Required data:** Document type, file (bounded size/format), checksum, verification result, expiry.
- **Validations:** Allowed type and format, size within limit, not expired, verification recorded.
- **Approvals:** Verifier validates each document; approver accepts the document set.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Rejected document requests re-upload, expired document blocks, missing mandatory document holds.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'document-collection-verification'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Bounded uploads, format/size validation, checksum integrity, verification audit, retention.
- **Anti-pattern to avoid:** Accepting unbounded file uploads with no type, size, or integrity check.

## 16. Cheque OCR review

- **Family key:** `cheque-ocr-review`
- **States:** Captured (initial) -> OCRDone -> NeedsReview -> Confirmed/Rejected (terminal).
- **Roles:** OCR engine, Operator/reviewer, Supervisor.
- **Required data:** Cheque image, OCR fields (amount, payee, date, MICR), confidence scores, corrected values.
- **Validations:** Low-confidence fields flagged for review; amount words vs figures reconciled; MICR valid.
- **Approvals:** Operator confirms or corrects low-confidence fields; supervisor approves overrides.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Unreadable image routes to manual capture, mismatch holds, duplicate cheque detection.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'cheque-ocr-review'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Confidence-gated review, human-in-the-loop, no auto-accept of low confidence, audit of corrections.
- **Anti-pattern to avoid:** Auto-accepting OCR amount below a confidence threshold without human review.

## 17. Signature / forgery review

- **Family key:** `signature-forgery-review`
- **States:** Submitted (initial) -> Scored -> UnderReview -> Verified/Queried/SuspectedForgery (terminal Verified).
- **Roles:** Verification engine, Reviewer, Senior approver, Fraud officer.
- **Required data:** Specimen signatures, presented signature, similarity score, decision, evidence package.
- **Validations:** Score is advisory not conclusive; high-risk items always reviewed; evidence retained.
- **Approvals:** Reviewer accepts or queries; senior approval for high-value or borderline cases.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Suspected forgery routes to fraud, no specimen blocks, borderline escalates.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'signature-forgery-review'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Detection separated from verification, human decision recorded, evidence retained, no conclusive auto-claim.
- **Anti-pattern to avoid:** Treating a similarity score as legally conclusive forgery proof with no human review.

## 18. Exception queue management

- **Family key:** `exception-queue`
- **States:** Open (initial) -> Assigned -> InProgress -> Resolved/Escalated (terminal Resolved).
- **Roles:** System (raises exceptions), Queue operator, Team lead, Approver.
- **Required data:** Exception type, source reference, severity, assignment, age, resolution notes.
- **Validations:** Every exception classified and assigned; severity drives priority; no item left unowned.
- **Approvals:** Operator resolves routine items; lead approves high-severity resolutions.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Aged items escalate, reassignment on absence, bulk actions require justification.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'exception-queue'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Ownership enforced, ageing SLA, severity routing, resolution audit, no silent dismissal.
- **Anti-pattern to avoid:** Bulk-closing exceptions without recording how each was resolved.

## 19. Incident management

- **Family key:** `incident-management`
- **States:** Reported (initial) -> Acknowledged -> Mitigating -> Resolved -> Closed (terminal); Reopened re-enters.
- **Roles:** Reporter, On-call responder, Incident commander, Approver/closure authority.
- **Required data:** Incident severity, affected service, timeline, actions, root-cause, comms log.
- **Validations:** Severity assigned, responder acknowledged within SLA, actions logged, closure criteria met.
- **Approvals:** Commander coordinates; closure requires verification that service is restored.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Sev escalation, paging fallback on no-ack, reopen on recurrence.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'incident-management'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Acknowledgement SLA, timeline audit, closure verification, link to postmortem for major incidents.
- **Anti-pattern to avoid:** Closing a Sev-1 without confirming restoration or capturing the timeline.

## 20. Change request management

- **Family key:** `change-request`
- **States:** Submitted (initial) -> Assessed -> Approved -> Implemented -> Reviewed/Closed (terminal); RolledBack on failure.
- **Roles:** Requestor, Change manager, CAB approvers, Implementer, Reviewer.
- **Required data:** Change description, risk/impact, rollback plan, schedule window, approvals.
- **Validations:** Risk assessed, rollback plan present, window valid, required approvals obtained before implement.
- **Approvals:** Standard pre-approved, normal via CAB, emergency with expedited approval and post-review.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Failed change triggers rollback, emergency change post-implementation review, conflict detection.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'change-request'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Mandatory rollback plan, approval before implement, schedule conflict check, full change audit.
- **Anti-pattern to avoid:** Implementing a change with no rollback plan or recorded approval.

## 21. SLA escalation

- **Family key:** `sla-escalation`
- **States:** Within (initial) -> Warning -> Breached -> Escalated -> Resolved (terminal).
- **Roles:** Owner, Escalation manager, Senior responder.
- **Required data:** SLA target, elapsed time, breach thresholds, escalation tiers, notification log.
- **Validations:** Timers run from a defined start, thresholds drive tier, business-hours calendar respected.
- **Approvals:** Auto-escalation at thresholds; manual escalation requires reason.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Pause on customer-wait, breach record, re-escalation if unresolved.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'sla-escalation'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Calendar-aware timers, tiered routing, breach audit, no silent timer reset.
- **Anti-pattern to avoid:** Resetting the SLA clock on internal reassignment to hide a breach.

## 22. Asset intervention / field service

- **Family key:** `asset-intervention`
- **States:** Raised (initial) -> Dispatched -> InProgress -> Completed/Cancelled (terminal); OnHold for parts.
- **Roles:** Requestor, Dispatcher, Field technician, Supervisor/approver.
- **Required data:** Asset id, fault description, intervention type, parts used, labour, sign-off.
- **Validations:** Asset exists, intervention authorized, parts availability checked, completion signed off.
- **Approvals:** Dispatcher assigns; supervisor approves costly interventions; customer/owner sign-off on completion.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Parts shortage holds, re-visit needed, escalation for major repair.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'asset-intervention'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Authorization before work, parts/labour audit, completion sign-off, cost approval threshold.
- **Anti-pattern to avoid:** Consuming parts and closing a job with no completion sign-off or cost record.

## 23. Parts consumption

- **Family key:** `parts-consumption`
- **States:** Requested (initial) -> Issued -> Consumed/Returned (terminal).
- **Roles:** Technician, Storekeeper, Inventory controller.
- **Required data:** Part number, quantity issued, job reference, stock level, valuation method.
- **Validations:** Sufficient stock, valid job link, quantity non-negative, stock decremented atomically.
- **Approvals:** Storekeeper issues; controller approves issues above a value/quantity threshold.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Stock shortfall blocks issue, return of unused parts, write-off for damaged parts.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'parts-consumption'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Atomic stock decrement under transaction, job linkage, valuation update, issue audit.
- **Anti-pattern to avoid:** Decrementing stock from the controller action without a transaction or audit event.

## 24. Inventory adjustment

- **Family key:** `inventory-adjustment`
- **States:** Counted (initial) -> VarianceIdentified -> PendingApproval -> Posted/Rejected (terminal).
- **Roles:** Counter, Inventory controller, Finance approver.
- **Required data:** Item, system quantity, counted quantity, variance, reason code, valuation impact.
- **Validations:** Variance computed, reason code mandatory, valuation impact within approval band.
- **Approvals:** Controller proposes; finance approves adjustments above a materiality threshold.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Large variance escalates, recount on dispute, write-off vs write-on routing.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'inventory-adjustment'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Mandatory reason code, four-eyes for material variance, GL posting linkage, full audit.
- **Anti-pattern to avoid:** Posting an inventory adjustment with no reason code or approval.

## 25. Purchase-order approval

- **Family key:** `purchase-order-approval`
- **States:** Drafted (initial) -> Submitted -> AtTier{n} -> Approved -> Issued/Rejected (terminal).
- **Roles:** Requestor, Budget holder, Procurement, Tier approvers.
- **Required data:** Vendor, line items, amount, budget/cost centre, delivery terms, approvals.
- **Validations:** Vendor approved, budget available, amount drives approval tier, no split to evade limits.
- **Approvals:** Tiered approval by amount; budget-holder sign-off; procurement review for new vendors.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Over-budget holds, split-PO detection, rejection with reason.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'purchase-order-approval'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Budget check, tiered approval matrix, split detection, vendor controls, audit.
- **Anti-pattern to avoid:** Splitting one purchase into several POs to stay under an approval limit.

## 26. Invoice approval (3-way match)

- **Family key:** `invoice-approval`
- **States:** Received (initial) -> Matched -> PendingApproval -> ApprovedForPayment/Disputed (terminal Approved).
- **Roles:** AP clerk, Approver, Finance controller.
- **Required data:** Invoice, matching PO, goods-receipt, amounts, tax, payment terms.
- **Validations:** Three-way match (PO/receipt/invoice) within tolerance, duplicate invoice check, tax validated.
- **Approvals:** Clerk matches; approver authorizes payment; controller for exceptions over tolerance.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Mismatch holds for query, duplicate blocks, partial-receipt handling.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'invoice-approval'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Three-way match, tolerance limits, duplicate detection, approval audit, segregation from payment run.
- **Anti-pattern to avoid:** Approving an invoice for payment with no PO/receipt match or duplicate check.

## 27. GL posting and finance approval

- **Family key:** `gl-posting-finance-approval`
- **States:** Drafted (initial) -> PendingReview -> Posted/Rejected (terminal); Reversed via new entry.
- **Roles:** Preparer, Reviewer, Finance approver.
- **Required data:** Journal lines, debit/credit, period, account validity, supporting evidence.
- **Validations:** Debits equal credits, period open, accounts valid, evidence attached for manual journals.
- **Approvals:** Preparer enters; independent reviewer approves before posting; period close locks.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Unbalanced journal blocked, closed-period rejection, reversal path.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'gl-posting-finance-approval'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Balanced-entry enforcement, segregation prepare/approve, period control, immutable posted lines, audit.
- **Anti-pattern to avoid:** Posting an unbalanced or back-dated journal into a closed period.

## 28. Support ticket workflow

- **Family key:** `support-ticket`
- **States:** New (initial) -> Assigned -> InProgress -> Resolved -> Closed (terminal); Reopened re-enters.
- **Roles:** Requester, Agent, Tier-2 specialist, Approver/closer.
- **Required data:** Ticket subject, priority, category, assignment, SLA, resolution, satisfaction.
- **Validations:** Category and priority set, assignment owned, SLA timers active, resolution recorded.
- **Approvals:** Agent resolves; specialist for escalations; requester confirmation before closure where required.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Reopen on dissatisfaction, escalation on SLA risk, merge of duplicates.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'support-ticket'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Ownership, SLA timers, resolution audit, closure confirmation, duplicate merge.
- **Anti-pattern to avoid:** Closing a ticket without recording resolution or notifying the requester.

## 29. CRM lead-to-opportunity

- **Family key:** `crm-lead-to-opportunity`
- **States:** New (initial) -> Qualified -> Converted -> Won/Lost (terminal).
- **Roles:** Marketing, Sales rep, Sales manager (discount/approval).
- **Required data:** Lead source, qualification, opportunity stage, amount, discount, stage history.
- **Validations:** Qualified before conversion, stage transitions valid, discount within authority.
- **Approvals:** Rep advances stages; manager approves discounts above threshold; conversion gate.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Disqualified lead closes, lost opportunity with reason, discount over-limit escalates.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'crm-lead-to-opportunity'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Qualification gate, stage-history audit, discount approval, no skipped mandatory stage.
- **Anti-pattern to avoid:** Applying an over-limit discount without manager approval.

## 30. ERP order-to-cash

- **Family key:** `erp-order-to-cash`
- **States:** OrderCreated (initial) -> CreditCleared -> Fulfilled -> Invoiced -> Settled (terminal); OnCreditHold.
- **Roles:** Sales, Credit controller, Fulfilment, Finance.
- **Required data:** Sales order, credit limit, fulfilment, invoice, receipt, GL posting.
- **Validations:** Credit check passed, stock available, order-to-invoice amounts reconcile, receipt matched.
- **Approvals:** Credit hold release, fulfilment authorization, invoice approval before posting.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Credit-block holds order, backorder, short payment dispute.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'erp-order-to-cash'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Credit control gate, fulfilment-to-invoice reconciliation, GL linkage, audit across steps.
- **Anti-pattern to avoid:** Shipping and invoicing while the customer is on credit hold.

## 31. ERP procure-to-pay

- **Family key:** `erp-procure-to-pay`
- **States:** Requisitioned (initial) -> Ordered -> Received -> Invoiced -> Paid (terminal).
- **Roles:** Requestor, Procurement, Receiver, AP, Approver.
- **Required data:** Requisition, PO, goods receipt, invoice, payment, three-way match.
- **Validations:** Approved requisition, valid PO, receipt confirms delivery, invoice matches, no duplicate payment.
- **Approvals:** PO approval, receipt confirmation, invoice approval, payment release.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Receipt shortfall, invoice mismatch hold, payment-run exception.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'erp-procure-to-pay'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Three-way match, segregation of order/receive/pay, duplicate-payment prevention, audit.
- **Anti-pattern to avoid:** Paying an invoice with no matching goods receipt.

## 32. Operational dashboard review

- **Family key:** `operational-dashboard-review`
- **States:** Generated (initial) -> Reviewed -> ActionsAssigned/Acknowledged (terminal Acknowledged).
- **Roles:** Operator, Team lead, Operations manager.
- **Required data:** Queue depths, SLA breaches, exception counts, throughput, ageing, health snapshot.
- **Validations:** Metrics sourced from a cached snapshot (never live blocking calls), thresholds defined, drill-down available.
- **Approvals:** Lead acknowledges flagged metrics; manager signs off the periodic review.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Stale snapshot warning, threshold breach action item, missing-data flag.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'operational-dashboard-review'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Cached read (no synchronous external calls on render), threshold alerting, review audit.
- **Anti-pattern to avoid:** Rendering a dashboard by calling external services synchronously on the request path.

## 33. Daily ops-manager review

- **Family key:** `daily-ops-manager-review`
- **States:** Pending (initial) -> InReview -> SignedOff (terminal); Escalated for carry-over.
- **Roles:** Operations manager, Team leads, Approver for action items.
- **Required data:** Prior-day exceptions, breaches, pending approvals, incidents, sign-off record.
- **Validations:** All open queues reviewed, action items owned with due dates, sign-off recorded daily.
- **Approvals:** Manager signs off the daily review; action items approved and assigned.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Unresolved carry-over flagged, escalation for repeated breaches, missing sign-off alert.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'daily-ops-manager-review'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Mandatory daily sign-off, action-item ownership, audit of review, carry-over tracking.
- **Anti-pattern to avoid:** Signing off the daily review with open critical items and no owner.

## 34. Audit evidence export

- **Family key:** `audit-evidence-export`
- **States:** Requested (initial) -> Approved -> Generated -> Delivered (terminal); Rejected.
- **Roles:** Auditor, Data owner/approver, Export operator.
- **Required data:** Scope/date range, record set, redaction rules, export format, immutable hash, recipient.
- **Validations:** Scope authorized, sensitive fields redacted per policy, export hashed and logged.
- **Approvals:** Data owner approves the export request before generation.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Over-broad scope rejected, redaction failure blocks, recipient verification.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'audit-evidence-export'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Approval before export, redaction enforcement, tamper-evident hash, export audit, retention.
- **Anti-pattern to avoid:** Exporting raw audit data with no approval, redaction, or integrity hash.

## 35. Backup / restore / rollback

- **Family key:** `backup-restore-rollback`
- **States:** Planned (initial) -> Approved -> Executed -> Verified (terminal); Failed -> Escalated.
- **Roles:** Operator, DBA, Change approver, Verifier.
- **Required data:** Backup set, restore point, verification result, rollback plan, environment.
- **Validations:** Backup integrity verified, restore point valid, target environment confirmed, approval obtained.
- **Approvals:** Restore/rollback into production requires change approval and a second operator.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Corrupt backup blocks restore, partial restore handling, failed-rollback escalation.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'backup-restore-rollback'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Integrity verification, approval before production restore, post-restore verification, audit.
- **Anti-pattern to avoid:** Restoring over production with no approval, verification, or rollback plan.

## 36. Release approval

- **Family key:** `release-approval`
- **States:** Built (initial) -> Tested -> Approved -> Deployed -> Verified (terminal); RolledBack on failure.
- **Roles:** Developer, Release manager, QA, Approver, Operations.
- **Required data:** Build artefact, test results, change list, rollback plan, approval sign-offs.
- **Validations:** Build succeeds, tests pass, change list reviewed, rollback plan present before deploy.
- **Approvals:** QA sign-off and release-manager approval gate the deployment; emergency path documented.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Failed gate blocks release, hotfix expedited approval, post-deploy verification failure rolls back.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'release-approval'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Build-and-test gate, mandatory rollback plan, approval before deploy, deployment audit.
- **Anti-pattern to avoid:** Deploying a build that has not passed tests or has no rollback plan.

## 37. Autonomous patch proposal (AI-suggested fix)

- **Family key:** `autonomous-patch-proposal`
- **States:** Proposed (initial) -> UnderReview -> Approved -> Applied/Rejected (terminal).
- **Roles:** AI agent (proposer), Reviewing engineer, Approver.
- **Required data:** Diagnosed issue, proposed diff, rationale, affected files, test evidence, confidence.
- **Validations:** Proposal scoped to the issue, diff bounded, tests included, no direct write to protected paths.
- **Approvals:** Engineer reviews and a human approves before any code change is applied; AI never self-approves.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Out-of-scope changes rejected, low-confidence routed to manual, failed tests block apply.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'autonomous-patch-proposal'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Human approval before apply, bounded diff, test gate, full proposal audit, no auto-merge.
- **Anti-pattern to avoid:** Letting the AI apply its own patch to the codebase without human approval.

## 38. Human approval before AI code change

- **Family key:** `human-approval-before-ai-code-change`
- **States:** Requested (initial) -> AwaitingApproval -> Approved -> Committed/Rejected (terminal).
- **Roles:** AI agent, Human approver, Audit log.
- **Required data:** Change intent, target files, diff, risk class, approver identity, decision timestamp.
- **Validations:** Approver authenticated and distinct from the agent, risk class assessed, approval recorded before write.
- **Approvals:** Mandatory single human approval gate; higher-risk changes add a second approver.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Rejected with reason, expired approval re-requested, scope change re-requires approval.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'human-approval-before-ai-code-change'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Hard gate before write, distinct human approver, risk-based second approver, immutable decision audit.
- **Anti-pattern to avoid:** Treating the AI agent identity as the approver to satisfy the gate automatically.

## 39. Knowledge import / dedupe / approval

- **Family key:** `knowledge-import-dedupe-approval`
- **States:** Imported (initial) -> Deduplicated -> PendingApproval -> Approved/Rejected (terminal).
- **Roles:** Importer, Reviewer/curator, Approver.
- **Required data:** Source item, similarity to existing items, dedupe decision, provenance, approval status.
- **Validations:** Provenance captured, near-duplicates detected, only approved items injected into context.
- **Approvals:** Curator reviews; approver promotes to Approved; rejected items retained but not injected.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Duplicate merged with provenance kept, low-quality rejected, conflicting item flagged.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'knowledge-import-dedupe-approval'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Provenance retention, dedupe before approval, approval-before-injection, full lineage audit.
- **Anti-pattern to avoid:** Injecting unreviewed imported knowledge into prompt context before approval.

## 40. Production incident postmortem

- **Family key:** `production-incident-postmortem`
- **States:** Draft (initial) -> InReview -> Approved -> ActionsTracked -> Closed (terminal).
- **Roles:** Incident commander, Contributors, Reviewer, Approver.
- **Required data:** Timeline, root cause, contributing factors, action items, owners, due dates.
- **Validations:** Root cause identified, action items owned with dates, blameless framing, evidence linked.
- **Approvals:** Reviewer validates analysis; approver signs off and tracks action items to closure.
- **Audit:** Immutable audit event per transition: actor, timestamp, from-state, to-state, reason, correlation id.
- **Exceptions:** Incomplete analysis re-opened, overdue action escalated, recurrence links prior postmortem.
- **DB model sketch:** WorkflowInstance bound to BusinessEntity 'production-incident-postmortem'; WorkflowStep per stage; WorkflowTransition + WorkflowAuditEvent per move; approvals in WorkflowApproval; controls/thresholds in WorkflowPolicy; exceptions in WorkflowException.
- **Service rule:** Blameless analysis, action-item ownership and tracking, evidence linkage, approval audit.
- **Anti-pattern to avoid:** Closing a postmortem with no owned, dated, tracked action items.

