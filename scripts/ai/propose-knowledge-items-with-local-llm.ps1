<#
.SYNOPSIS  Propose knowledge items from a document using a LOCAL model. PROPOSAL only — never overwrites memory.
.DESCRIPTION The local LLM proposes short knowledge items (title + summary + limitation + tags). Output goes to a
             git-ignored PROPOSAL file with reviewStatus=PENDING_REVIEW. It is NOT installed into MSSQL and is NOT
             authoritative — an approval flow (human/system) would convert reviewed proposals into versioned
             memory (propose-never-overwrite). Requires Ollama; records a blocker if absent.
.PARAMETER DocPath / Model / MaxItems / OutDir
#>
param(
  [string]$DocPath = "",
  [string]$Model = "qwen2.5-coder:14b",
  [int]$MaxItems = 3,
  [string]$OutDir = ""
)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
if (-not $DocPath) { $DocPath = Join-Path $repo "benchmarks/fixtures/local-llm/maker-checker-payment-workflow.md" }
if (-not $OutDir) { $OutDir = Join-Path $repo ".tmp-llm-proposals" }
New-Item -ItemType Directory -Force $OutDir | Out-Null
$doc = Get-Content $DocPath -Raw
try { $tags = Invoke-RestMethod "http://localhost:11434/api/tags" -TimeoutSec 5; $have = $tags.models.name -contains $Model } catch { $have=$false }
if (-not $have) { Write-Host "[BLOCKER] local model '$Model' unavailable — proposal skipped." -ForegroundColor Yellow; exit 0 }

$prompt = @"
From the document below, propose up to $MaxItems concise, ORIGINAL knowledge items. Reply as JSON only:
{ "items": [ { "title": "...", "summary": "1-2 sentences, original wording", "limitation": "what this does NOT cover", "tags": ["..."] } ] }
Do not copy the document verbatim. Do not invent facts not supported by the document.

DOCUMENT:
$doc
"@
$body = @{ model=$Model; prompt=$prompt; stream=$false; format="json"; options=@{ temperature=0.1; num_predict=700 } } | ConvertTo-Json -Depth 5
try { $resp = (Invoke-RestMethod "http://localhost:11434/api/generate" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 180).response } catch { $resp = "" }
$items = @()
try { $items = ($resp | ConvertFrom-Json).items } catch {}
$proposal = [ordered]@{
  source = (Split-Path $DocPath -Leaf); model = $Model; generatedKind = "llm-knowledge-proposal"
  reviewStatus = "PENDING_REVIEW"; authoritative = $false; installedToMssql = $false
  items = $items
}
$out = Join-Path $OutDir "knowledge-items-proposal.json"
$proposal | ConvertTo-Json -Depth 8 | Set-Content $out -Encoding UTF8
Write-Host "Proposed $(@($items).Count) knowledge item(s) (PROPOSAL, PENDING_REVIEW, NOT installed): $out" -ForegroundColor Green
Write-Host "Governance: these are proposals only — MSSQL stays authoritative; approval converts reviewed proposals to versioned memory." -ForegroundColor Yellow
