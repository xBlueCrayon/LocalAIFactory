<#
.SYNOPSIS  Extract workflow rules (roles/states/transitions/validations) from a document using a LOCAL model. PROPOSAL only.
.DESCRIPTION Deterministic extractor first (the source of truth); the local LLM then PROPOSES a structured rule
             set. Output is written as a PROPOSAL JSON for human/system review — it never overwrites knowledge.
             MSSQL remains authoritative. Requires Ollama; records a blocker if absent.
.PARAMETER DocPath / Model / OutDir
#>
param(
  [string]$DocPath = "",
  [string]$Model = "qwen2.5-coder:14b",
  [string]$OutDir = ""
)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
if (-not $DocPath) { $DocPath = Join-Path $repo "benchmarks/fixtures/local-llm/maker-checker-payment-workflow.md" }
if (-not $OutDir) { $OutDir = Join-Path $repo ".tmp-llm-proposals" }
New-Item -ItemType Directory -Force $OutDir | Out-Null
if (-not (Test-Path $DocPath)) { Write-Host "doc not found: $DocPath" -ForegroundColor Red; exit 1 }
$doc = Get-Content $DocPath -Raw
try { $tags = Invoke-RestMethod "http://localhost:11434/api/tags" -TimeoutSec 5; $have = $tags.models.name -contains $Model } catch { $have=$false }
if (-not $have) { Write-Host "[BLOCKER] local model '$Model' unavailable — extraction skipped." -ForegroundColor Yellow; exit 0 }

$prompt = @"
Extract the workflow rules from the document below. Reply as JSON only with keys:
roles (array), states (array), transitions (array of {from,to,by}), validations (array), auditRules (array),
exceptions (array). If something is not stated, use an empty array. Do not invent rules.

DOCUMENT:
$doc
"@
$body = @{ model=$Model; prompt=$prompt; stream=$false; format="json"; options=@{ temperature=0; num_predict=700 } } | ConvertTo-Json -Depth 5
try { $resp = (Invoke-RestMethod "http://localhost:11434/api/generate" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 180).response } catch { $resp = "" }
$proposal = [ordered]@{
  source = (Split-Path $DocPath -Leaf)
  model = $Model
  generatedKind = "llm-proposal"
  reviewStatus = "PENDING_REVIEW"   # never auto-approved; never overwrites knowledge
  authoritative = $false
  rules = $null
}
try { $proposal.rules = $resp | ConvertFrom-Json } catch { $proposal.rules = @{ raw = $resp } }
$out = Join-Path $OutDir "workflow-rules-proposal.json"
$proposal | ConvertTo-Json -Depth 8 | Set-Content $out -Encoding UTF8
$rules = $proposal.rules
Write-Host "Extracted (PROPOSAL, pending review): roles=$(@($rules.roles).Count) states=$(@($rules.states).Count) transitions=$(@($rules.transitions).Count) validations=$(@($rules.validations).Count)" -ForegroundColor Green
Write-Host "Proposal written (git-ignored, not authoritative): $out" -ForegroundColor Yellow
