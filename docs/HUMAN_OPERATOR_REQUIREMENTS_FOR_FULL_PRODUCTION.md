# Human / Operator Requirements for Full Production

What **cannot be supplied by code alone** on this workstation. Each item blocks `FULL_PRODUCTION_READY`
until a human/operator/external party/customer provides it. Code completes everything else (see the
production-readiness gate: **PILOT_READY**, 0 technical FAIL).

| # | Item | Why needed | Exact value needed | Provided by | How to validate | Blocked without it | Safe placeholder |
|---|---|---|---|---|---|---|---|
| 1 | Windows **Server** host/VM | Production hosting (not a Win 11 workstation) | a Server 2022+ host with IIS | operator/IT | `Get-CimInstance Win32_OperatingSystem` shows Server | "production host evidence" | the local Win 11 IIS pilot |
| 2 | Domain name + production DNS | Real URL + TLS SAN | e.g. `laf.bank.internal` | operator/IT | DNS resolves | HTTPS/CA cert | `localhost` |
| 3 | **CA-issued TLS certificate** | Trusted production TLS | cert for the domain (internal PKI or public CA) | operator/PKI | chain validates without `-SkipCertificateCheck` | "HTTPS proof" stays self-signed/PARTIAL | self-signed localhost cert |
| 4 | Production firewall rules | Controlled exposure | inbound 443 to the host | operator/network | port reachable from clients | exposure | none |
| 5 | **Entra ID / OIDC tenant** | Enterprise SSO | tenant id, app registration, redirect URI, client secret/cert | operator/identity | `scripts/sso/check-oidc-config.ps1` PASS | SSO/OIDC gate | Windows/Negotiate (proven) |
| 6 | Service/domain account | Least-priv app-pool identity | `DOMAIN\svc-laf` | operator/AD | SQL login + folder ACL | domain-account posture | `ApplicationPoolIdentity` (proven) |
| 7 | SMTP relay endpoint | Notifications | host/port/auth | operator | health/test script | email integration | dev sink |
| 8 | SFTP endpoint | File integration | host/port/key | operator | health/test script | SFTP integration | dev sink |
| 9 | Vault / secret store | Production secrets | approved store | operator/security | secret retrieval works | secret management | Data Protection keys (local) |
| 10 | Customer / sanitized estate data | Real pilot | a sanitized dataset | customer | import + verify | "customer pilot" | synthetic fixtures |
| 11 | Customer acceptance criteria | Pilot signoff | written criteria | customer | acceptance test maps to criteria | signoff | `Customer-Acceptance-Test.md` |
| 12 | **External security reviewer / pen-test** | Independent assurance | a signed report | external | report + remediation | "external security review" | internal `security-audit` (0 HIGH) |
| 13 | Backup retention policy | Compliance | retention schedule | operator | scheduled job + policy doc | retention | manual backup proven |
| 14 | Monitoring/alerting destination | Ops | SIEM/alert sink | operator | alerts fire | "monitoring/alerting" | `/Support` + support bundle |
| 15 | Incident escalation contacts | On-call | rota + contacts | operator | escalation runbook resolves | "incident response" | runbooks only |
| 16 | Production change window | Controlled rollout | approved window | operator | change record | staged rollout | n/a |
| 17 | Rollback approval | Governance | named approver | operator | rollback authorised | rollback governance | rollback technically proven |
| 18 | License / commercial policy | Enforcement | a business decision | operator/business | enforcement active | "license enforcement" | edition/license model (designed) |

## Net

Everything **technical** that this host can prove is **done** (PILOT_READY, 0 FAIL). The list above is the
exact, ordered set of **non-code** inputs needed to reach `FULL_PRODUCTION_READY`. See
`FULL_PRODUCTION_BLOCKERS.md` for the by-owner breakdown.
