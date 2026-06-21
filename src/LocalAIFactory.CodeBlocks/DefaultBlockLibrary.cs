namespace LocalAIFactory.CodeBlocks;

/// <summary>The seeded catalogue of reusable blocks, grounded in the real ERP Gold + reasoning engine work.</summary>
public static class DefaultBlockLibrary
{
    public static IEnumerable<CodeBuildingBlock> Blocks()
    {
        yield return new CodeBuildingBlock
        {
            BlockId = "password-hashing", Name = "Password hashing (PBKDF2)",
            Purpose = "Store passwords irreversibly", ProblemSolved = "Never store plaintext or reversible passwords",
            Keywords = { "password", "hash", "pbkdf2", "credential" },
            GeneratedFiles = { "src/LafErp.Services/PasswordHasher.cs" },
            CodePatternSummary = "PBKDF2 SHA-256, 100k iterations, per-user salt, fixed-time compare",
            SecurityRules = { "no plaintext passwords", "fixed-time comparison", "per-user random salt" },
            ValidationRules = { "verify rejects wrong + null" },
            TestPattern = "xUnit: hash round-trips and rejects a wrong password",
            FailureModes = { "weak iteration count", "shared salt" },
            KnowledgeItemIds = { "erp-gold-auth-hardening-v1" }, GeneratorTemplatePaths = { "tools/LocalAIFactory.Generator/templates/erp-core/src/LafErp.Services/PasswordHasher.cs" },
            Confidence = 0.95
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "audit-event", Name = "Append-only audit event",
            Purpose = "Record who did what when", ProblemSolved = "Tamper-evident history of state changes",
            Keywords = { "audit", "audit trail", "audit event", "history" },
            GeneratedFiles = { "src/LafErp.Services/AuditService.cs" },
            CodePatternSummary = "AuditService.Record(entity,id,action) appends an AuditEvent with actor + UTC",
            SecurityRules = { "append-only, never edit/delete audit rows" }, ValidationRules = { "every state change records one event" },
            TestPattern = "xUnit: a state change appends an audited event with a non-empty actor",
            KnowledgeItemIds = { "erp-gold-rbac-company-audit-v1" }, Confidence = 0.92
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "anti-forgery", Name = "Anti-forgery on POST forms",
            Purpose = "Stop CSRF", Keywords = { "anti-forgery", "csrf", "antiforgery", "token" },
            CodePatternSummary = "[ValidateAntiForgeryToken] + @Html.AntiForgeryToken()",
            SecurityRules = { "validate the token on every state-changing POST" }, TestPattern = "Playwright: tokenless POST is 400",
            KnowledgeItemIds = { "erp-gold-auth-hardening-v1" }, Confidence = 0.9
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "secure-login", Name = "Secure login",
            Purpose = "Authenticate a user and issue a role-claimed cookie", ProblemSolved = "Real authentication, not a dev cookie",
            Keywords = { "login", "sign in", "authenticate", "secure login", "auth" },
            Dependencies = { "password-hashing", "audit-event", "anti-forgery" },
            GeneratedFiles = { "src/LafErp.Web/Controllers/AccountController.cs", "src/LafErp.Web/Views/Account/Login.cshtml", "src/LafErp.Services/UserAuthService.cs" },
            CodePatternSummary = "UserAuthService.Authenticate + cookie sign-in with Name + Role claims, login audited",
            SecurityRules = { "no username enumeration", "secure sliding cookie" },
            TestPattern = "xUnit: seeded admin authenticates with roles; wrong password rejected",
            PlaywrightPattern = "login.spec: wrong password rejected; correct password reaches dashboard",
            KnowledgeItemIds = { "erp-gold-real-authentication-v1", "erp-gold-auth-hardening-v1" }, Confidence = 0.9
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "login-lockout", Name = "Failed-login lockout",
            Purpose = "Throttle brute-force login", Keywords = { "lockout", "lock", "failed login", "brute force", "attempts" },
            Dependencies = { "secure-login", "audit-event" },
            CodePatternSummary = "increment failure count; lock for N minutes after threshold; reset + stamp last-login on success",
            SecurityRules = { "audited lockout", "lock even a correct password while locked" },
            TestPattern = "xUnit: account locks after max attempts; locked user refused; success resets count",
            KnowledgeItemIds = { "erp-gold-auth-hardening-v1" }, Confidence = 0.9
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "maker-checker", Name = "Maker/checker approval workflow",
            Purpose = "Separation of duties on documents", Keywords = { "maker", "checker", "approval", "approve", "workflow", "four eyes" },
            Dependencies = { "audit-event" }, GeneratedFiles = { "src/LafErp.Services/WorkflowService.cs" },
            CodePatternSummary = "submit -> pending; maker cannot approve own; approval threshold; rejection requires a reason",
            ValidationRules = { "maker != approver", "reason required on reject", "amount threshold gates a separate approver" },
            TestPattern = "xUnit (negative): maker cannot approve own; over-threshold needs a separate approver",
            KnowledgeItemIds = { "production-grade-erp-controls-v1" }, Confidence = 0.9
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "ef-migration", Name = "EF migration-safe change",
            Purpose = "Evolve the schema safely", Keywords = { "migration", "ef", "schema", "database update", "ddl" },
            GeneratedFiles = { "src/LafErp.Data/Migrations/*.cs", "scripts/apply-migrations.ps1" },
            CodePatternSummary = "design-time factory; Migrate() on SQL Server, EnsureCreated() on SQLite; additive migration",
            ValidationRules = { "additive/backward-compatible", "apply from the deterministic emit" }, SecurityRules = { "no destructive change without approval" },
            TestPattern = "xUnit: committed migration present + model generates a create script",
            FailureModes = { "PendingModelChangesWarning when model drifts", "stale SQLite file not migrated" },
            KnowledgeItemIds = { "erp-gold-ef-migrations-v1" }, Confidence = 0.85
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "crud-module", Name = "CRUD list/create/edit/deactivate",
            Purpose = "Master-data UI", Keywords = { "crud", "list", "create", "edit", "deactivate", "catalog", "master data" },
            Dependencies = { "audit-event" },
            GeneratedFiles = { "src/LafErp.Web/Controllers/CatalogController.cs", "src/LafErp.Web/Views/Catalog/*.cshtml" },
            CodePatternSummary = "generic CatalogCrudService<T> + reflection forms; soft-delete; empty string -> '' not null",
            ValidationRules = { "required Name", "soft-delete, never hard delete" },
            TestPattern = "xUnit: create/edit/deactivate per entity",
            PlaywrightPattern = "catalog-crud.spec: create -> list -> edit -> deactivate",
            FailureModes = { "NOT NULL 500 when empty string mapped to null" },
            KnowledgeItemIds = { "erp-gold-ui-crud-workflows-v1" }, Confidence = 0.88
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "report-endpoint", Name = "Report endpoint",
            Purpose = "Company-scoped reporting", Keywords = { "report", "register", "summary", "aging", "valuation", "trial balance" },
            GeneratedFiles = { "src/LafErp.Services/ReportsService.cs", "src/LafErp.Web/ApiEndpoints.cs" },
            CodePatternSummary = "submitted-docs-only, company-scoped EF aggregation (no group-by-constant); REST GET",
            ValidationRules = { "reconciles to the GL", "respects company scope" },
            TestPattern = "xUnit: register/summary reconcile; company scoping",
            KnowledgeItemIds = { "erp-gold-report-depth-v1" }, Confidence = 0.85
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "stock-movement", Name = "Stock movement (moving average)",
            Purpose = "Inventory ledger", Keywords = { "stock", "inventory", "moving average", "warehouse", "valuation" },
            GeneratedFiles = { "src/LafErp.Services/StockService.cs" },
            CodePatternSummary = "immutable signed stock-ledger entries; moving-average valuation; block negative stock",
            ValidationRules = { "no negative stock by default", "save between moves (Last reads the DB)" },
            TestPattern = "xUnit: receipt/issue/transfer/adjustment; insufficient stock blocked",
            KnowledgeItemIds = { "erp-gold-stock-depth-v1" }, Confidence = 0.85
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "accounting-posting", Name = "Double-entry accounting posting",
            Purpose = "General ledger", Keywords = { "accounting", "gl", "double entry", "ledger", "posting", "journal" },
            GeneratedFiles = { "src/LafErp.Services/AccountingService.cs" },
            CodePatternSummary = "post documents to GL; debits == credits; P&L / balance sheet / trial balance",
            ValidationRules = { "debits equal credits", "post only submitted documents" },
            TestPattern = "xUnit: GL always balances after a full cycle",
            KnowledgeItemIds = { "erp-accounting-production-v1" }, Confidence = 0.9
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "document-lifecycle", Name = "Document lifecycle",
            Purpose = "Draft -> submit -> approve -> cancel/reverse", Keywords = { "document lifecycle", "draft", "submit", "cancel", "immutable", "reverse" },
            Dependencies = { "maker-checker", "audit-event" },
            CodePatternSummary = "DocStatus Draft/Submitted/Cancelled; immutable after posting; reverse not edit",
            ValidationRules = { "immutable after approval", "cancel creates a reversal" },
            TestPattern = "xUnit: cannot edit posted; cancel reverses",
            KnowledgeItemIds = { "erp-gold-document-lifecycle-v1" }, Confidence = 0.85
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "manufacturing-order", Name = "Manufacturing production order",
            Purpose = "BOM-driven production", Keywords = { "manufacturing", "production order", "bom", "work order", "material issue", "quality" },
            Dependencies = { "stock-movement", "audit-event" },
            GeneratedFiles = { "src/LafErp.Services/ManufacturingService.cs", "src/LafErp.Core/ManufacturingEntities.cs" },
            CodePatternSummary = "BOM -> production order; material issue relieves stock; quality gate; finished-goods receipt at unit cost; immutable when complete",
            ValidationRules = { "block issue if material short", "complete requires quality pass" },
            TestPattern = "xUnit: issue reduces stock; insufficient blocks; quality fail blocks completion; cost computed",
            KnowledgeItemIds = { "erp-gold-manufacturing-depth-v1" }, Confidence = 0.85
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "import-export", Name = "Import/export with rejected-row report",
            Purpose = "Bulk data in/out", Keywords = { "import", "export", "csv", "rejected row", "bulk" },
            GeneratedFiles = { "src/LafErp.Services/RbacImportServices.cs" },
            CodePatternSummary = "CSV import with per-row validation; ImportBatch counts + error capture; audited",
            ValidationRules = { "reject invalid rows with a report", "record an audit action" },
            TestPattern = "xUnit: valid import works; invalid generates errors",
            KnowledgeItemIds = { "erp-gold-reporting-import-export-v1" }, Confidence = 0.8
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "playwright-proof", Name = "Playwright UI proof",
            Purpose = "Prove a UI flow end-to-end", Keywords = { "playwright", "browser", "ui test", "e2e", "smoke ui" },
            GeneratedFiles = { "playwright/tests/*.spec.ts" },
            CodePatternSummary = "data-testid hooks + webServer config; navigate/act/assert; reuseExistingServer",
            ValidationRules = { "stable data-testid selectors", "no HTTP 500" },
            TestPattern = "Playwright spec", PlaywrightPattern = "spec navigates and asserts a visible testid",
            KnowledgeItemIds = { "erp-gold-ui-crud-workflows-v1" }, Confidence = 0.85
        };
        yield return new CodeBuildingBlock
        {
            BlockId = "production-smoke", Name = "Production smoke + backup/restore",
            Purpose = "Prove a deployable build", Keywords = { "smoke", "production smoke", "health", "backup", "restore", "deploy" },
            GeneratedFiles = { "scripts/run-production-smoke.ps1", "scripts/backup-db.ps1", "scripts/restore-db.ps1" },
            CodePatternSummary = "publish + run; health + real login + no-HTTP-500; backup/restore scripts",
            ValidationRules = { "fresh DB so schema matches the model", "no plaintext production password" },
            TestPattern = "smoke script returns pass JSON",
            KnowledgeItemIds = { "erp-gold-local-production-and-reproduction-v1" }, Confidence = 0.8
        };
    }
}
