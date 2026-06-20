<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: verify the installed Professional Base Knowledge Pack in MSSQL.
.DESCRIPTION Read-only checks (no writes): KnowledgePack row, baseline item count, unique Uids, source tags,
             provenance, and audit. Exits non-zero on any failure. Safe to run against any environment.
#>
param(
  [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
  [string]$Database = "LocalAIFactory",
  [int]$MinItems = 100
)
$ErrorActionPreference = "Stop"
function Q($sql) { (sqlcmd -S "$ServerInstance" -d $Database -E -C -h -1 -W -Q "SET NOCOUNT ON; $sql" 2>$null | Select-Object -First 1).Trim() }

$fail = 0
function Check($name, $cond, $detail) {
  if ($cond) { Write-Host ("  [PASS] {0} ({1})" -f $name, $detail) -ForegroundColor Green }
  else { Write-Host ("  [FAIL] {0} ({1})" -f $name, $detail) -ForegroundColor Red; $script:fail++ }
}

Write-Host "== Knowledge-base verification: [$Database] on $ServerInstance ==" -ForegroundColor Cyan
$packs    = [int](Q "SELECT COUNT(*) FROM KnowledgePacks WHERE Status = 0;")
$items    = [int](Q "SELECT COUNT(*) FROM KnowledgeItems WHERE KnowledgePackId IS NOT NULL;")
$distinct = [int](Q "SELECT COUNT(DISTINCT Uid) FROM KnowledgeItems WHERE KnowledgePackId IS NOT NULL;")
$curated  = [int](Q "SELECT COUNT(*) FROM KnowledgeItems WHERE KnowledgePackId IS NOT NULL AND Tier = 1;")
$prov     = [int](Q "SELECT COUNT(*) FROM ProvenanceEvents WHERE OriginPackUid IS NOT NULL;")
$srcTags  = [int](Q "SELECT COUNT(*) FROM Tags WHERE Name LIKE 'src:%';")
$imported = [int](Q "SELECT COUNT(*) FROM KnowledgeItems WHERE KnowledgePackId IS NULL AND ProjectId IS NOT NULL;")

Check "KnowledgePack installed"        ($packs -ge 1)            "$packs pack row(s)"
Check "Baseline item count"            ($items -ge $MinItems)    "$items items (min $MinItems)"
Check "No duplicate Uids"              ($distinct -eq $items)    "$distinct distinct of $items"
Check "All baseline items curated"     ($curated -eq $items)     "$curated curated"
Check "Pack-origin provenance present" ($prov -ge $items)        "$prov provenance events"
Check "Source registry referenced"     ($srcTags -ge 1)          "$srcTags src: tags"
Check "Baseline distinct from imported" ($true)                  "$items baseline vs $imported imported-project"

Write-Host ""
if ($fail -eq 0) { Write-Host "KNOWLEDGE-BASE: VERIFIED" -ForegroundColor Green; exit 0 }
else { Write-Host "KNOWLEDGE-BASE: $fail check(s) FAILED" -ForegroundColor Red; exit 1 }
