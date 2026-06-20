# LocalAIFactory — Documentation Hub

LocalAIFactory is a private, **local-first, MSSQL-authoritative** AI software-engineering platform for a banking
middleware estate (.NET 10 / ASP.NET Core MVC, EF Core, MSSQL; optional Ollama/Qdrant). It imports legacy
projects, understands them structurally (C#/T-SQL/Python symbols, C#↔SQL bridge, impact analysis), and
accumulates a **governed, approval-gated knowledge base**. `MASTER_VISION.md` is authoritative.

This index links the documentation by role. Authoritative readiness scores live in
[`readiness-scorecard.json`](readiness-scorecard.json) and render live at `/Readiness`.

## Start here
- [Quick Start (with screenshots)](Quick-Start-With-Screenshots.md) · [Architecture](01-Architecture.md) · [Setup](02-Setup.md)
- [Final 20X Completion Report](Final-20X-Completion-Report.md) — latest sprint outcome + honest scores
- [Final 20X Gap Audit](Final-20X-Gap-Audit.md) — 42-area honest readiness audit

## By role

| Role | Read |
|---|---|
| **Executive / buyer** | [Industrial Ship-Readiness Certificate](Industrial-Ship-Readiness-Certificate.md), [Enterprise Readiness Scorecard](Enterprise-Readiness-Scorecard.md), [Commercial Pilot Package](Commercial-Pilot-Package.md), [Edition Matrix](Edition-Matrix.md) |
| **User** | [User Manual](User-Manual.md), [Quick Start](Quick-Start-With-Screenshots.md) |
| **Admin** | [Admin Manual](Admin-Manual.md), [RBAC Matrix](RBAC-Matrix.md), [Audit Model](Audit-Model.md) |
| **Operator** | [Operator Manual](Operator-Manual.md), [Supportability Dashboard Spec](Supportability-Dashboard-Spec.md), [Backup/Restore Runbook](Backup-Restore-Runbook.md), [Upgrade/Rollback Runbook](Upgrade-Rollback-Runbook.md) |
| **Developer** | [Developer Manual](Developer-Manual.md), [Development Workflow](03-Development-Workflow.md), [Knowledge-Pack Authoring Guide](Knowledge-Pack-Authoring-Guide.md) |
| **Deployment** | [Industrial Installation Guide](Industrial-Installation-Guide.md), [Windows Server / IIS](Windows-Server-IIS-Deployment-Guide.md), [SQL Server Deployment](SQL-Server-Deployment-Guide.md), [Clean-Machine Install Proof](Clean-Machine-Install-Proof.md), [Customer Handover Package](Customer-Handover-Package.md) |
| **Database** | [Database & Knowledge-Base Ship Proof](Database-and-KnowledgeBase-Ship-Proof.md), `database/README.md` |
| **Security** | [Security Model](Security-Model.md), [Threat Model](Threat-Model.md), [Security Pen-test Readiness](Security-Pentest-Readiness.md), [Security Test Checklist](Security-Test-Checklist.md), [Data Protection Plan](Data-Protection-Plan.md), [Secrets Handling](Secrets-Handling.md) |
| **Support** | [Support Runbook](Support-Runbook.md), [Troubleshooting](07-Troubleshooting.md), [Resource & Performance Evidence](Resource-and-Performance-Evidence.md) |
| **AI governance** | [Knowledge Architecture (general/project/chat)](Knowledge-Architecture-General-Project-Chat.md), [Chat-Learning Pipeline](Chat-Learning-Pipeline.md), [Multi-Agent Knowledge Factory](Multi-Agent-Knowledge-Factory.md) |
| **Knowledge base** | [Knowledge Separation & Retrieval Rules](Knowledge-Separation-and-Retrieval-Rules.md), [Knowledge-Pack Authoring Guide](Knowledge-Pack-Authoring-Guide.md) |
| **ERP / CRM** | `enterprise-scenarios/`, ERP/CRM benchmark fixture (`benchmarks/fixtures/erp-crm-industrial`) |
| **Core banking** | Core-banking fixture (`benchmarks/fixtures/core-banking`), [Financial institution scenarios](../enterprise-scenarios/financial-institution-operations/README.md) |
| **KYC / AML / approval** | [KYC→Transaction-Approval scenario](../enterprise-scenarios/kyc-to-transaction-approval/README.md), KYC/AML fixture (`benchmarks/fixtures/kyc-aml-approval`) |
| **Market intelligence** | [Market-Intelligence Forecast Module](Market-Intelligence-Forecast-Module.md), [Market-Module Disclaimers](Market-Module-Disclaimers.md) |
| **Document intelligence / OCR** | Document-intelligence design + cheque risk-triage prototype (see scorecard area 14) |
| **Autonomous engineering** | [Controlled Autonomous Engineering Runbook](Controlled-Autonomous-Engineering-Runbook.md), [Autonomous Local Fix-Loop Proof](Autonomous-Local-Fix-Loop-Proof.md) |
| **SSO / enterprise auth** | [SSO / IdP Readiness](SSO-IdP-Readiness.md), [Enterprise Auth Integration Plan](Enterprise-Auth-Integration-Plan.md), [Claims-Roles Mapping](Claims-Roles-Mapping.md) |

## Honesty note
Scores are conservative: 100 means implemented + tested + demonstrated + documented + reviewable. Domain,
deployment, and commercial areas are deliberately modest until shipped and proven. Items labelled **DESIGN** are
not yet implemented. The platform makes **no** vendor-certification, regulatory, financial, or fraud-proof claim.
