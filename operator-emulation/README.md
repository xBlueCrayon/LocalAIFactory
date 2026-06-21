# operator-emulation

> **STATUS: EMULATED — NOT REAL.** Every value in this pack is a clearly-fake placeholder.
> Nothing here is wired to a live system. This pack exists so a human operator can later
> supply real values and prove which production inputs are still **MISSING**, **BLOCKED_EXTERNAL**,
> or **BLOCKED_CUSTOMER**.

## What this pack is

LocalAIFactory is a local-first platform. To stand it up in a real production/pilot
environment, an **operator** (not the developer, not the AI) must provide a number of
external inputs: a production host, a domain + CA certificate, identity provider (Entra)
registration, SMTP relay, SFTP endpoint, a customer pilot sign-off, an external security
review, and a commercial license policy.

None of those exist yet. This folder **emulates** each of those inputs with obviously-fake
placeholders so that:

1. The *shape* of every required input is documented and reviewable.
2. A human can see exactly which fields are still outstanding and who owns them.
3. No reviewer can mistake the project for being "integrated live" — every status is
   `EMULATED`, `MISSING`, `BLOCKED_EXTERNAL`, or `BLOCKED_CUSTOMER`, never `REAL`.

## What this pack is NOT

- It is **not** a set of working credentials. There are no real secrets here.
- It does **not** call any external service.
- It does **not** prove any integration. See `expected-production-outputs.json` for the
  list of things that would have to be true in a real deployment — all currently unproven.

## File index

| File | Emulated input | Owner |
| --- | --- | --- |
| `production-host-inputs.example.json` | Production Windows host, OS edition, admin account, IIS feature state | operator |
| `domain-and-ca-certificate-inputs.example.json` | Domain `laf.example.local`, CA cert thumbprint, SAN, expiry | operator |
| `entra-oidc-inputs.example.json` | Entra tenant id, client id, client-secret **reference** (vault path), authority, redirect URI, claims mapping | operator |
| `smtp-relay-inputs.example.json` | `smtp.example.local`, port 587, auth, from address | operator |
| `sftp-endpoint-inputs.example.json` | `sftp.example.local`, port 22, key reference | operator |
| `customer-pilot-signoff.example.json` | Customer name, acceptance criteria refs, sign-off date, signer | customer (`BLOCKED_CUSTOMER`) |
| `external-security-review.example.json` | Reviewer firm, scope, report ref, findings count | external (`BLOCKED_EXTERNAL`) |
| `commercial-license-policy.example.json` | Edition, entitlement, enforcement mode | external/operator (`BLOCKED_EXTERNAL`) |
| `expected-production-outputs.json` | ~12 production outputs that must validate before go-live | mixed |

## Field schema (each `*.example.json`)

```jsonc
{
  "kind": "operator-emulation",
  "status": "EMULATED",
  "fields": [
    {
      "name": "...",                       // logical input name
      "exampleValue": "...",               // a FAKE placeholder, never a real value
      "realProvider": "...",               // who/what supplies the real value
      "whyRequired": "...",                // why production needs it
      "validationCommand": "...",          // how an operator would later verify the REAL value
      "expectedSuccessfulOutput": "...",   // what a passing check looks like
      "sourceReferenceType": "...",        // operator-supplied | vault-reference | external-attestation | customer-attestation
      "status": "REAL|EMULATED|MISSING|BLOCKED_EXTERNAL|BLOCKED_CUSTOMER"
    }
  ]
}
```

## How an operator replaces these placeholders

1. Copy each `*.example.json` to a git-ignored real file (e.g. `production-host-inputs.json`)
   **outside** the repo, or into your secrets manager.
2. Replace every `exampleValue` and every `*-placeholder` token with the real value.
   - Secrets (Entra client secret, SFTP key) are stored as a **vault reference**, never inline.
   - See CLAUDE.md §10: "No secrets in the repo."
3. Run each field's `validationCommand` and confirm the `expectedSuccessfulOutput`.
4. Flip `status` from `EMULATED`/`MISSING` to `REAL` **only** after the validation passes.
5. For `BLOCKED_EXTERNAL` / `BLOCKED_CUSTOMER` fields, attach the external/customer
   attestation artifact and record its reference before changing status.

Until every field reads `REAL` and `expected-production-outputs.json` is fully satisfied,
LocalAIFactory is **not** production-validated.
