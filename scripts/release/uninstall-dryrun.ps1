<# .SYNOPSIS R2-ACC-INDUSTRIAL: uninstall DRY-RUN — prints what WOULD be removed. Removes nothing. Never drops the DB. #>
param([string]$PublishDir = "C:\inetpub\LocalAIFactory", [string]$Database = "LocalAIFactory")
Write-Host "== Uninstall plan (DRY-RUN) ==" -ForegroundColor Cyan
Write-Host "  Would stop the app pool / service for the site."
Write-Host "  Would remove published files under: $PublishDir (after a confirmed backup)."
Write-Host "  Would NOT drop the database [$Database] — data removal is a separate, explicit, operator-approved step."
Write-Host "  Data Protection keys and any backups are preserved."
Write-Host "DRY-RUN: nothing removed. Uninstall is operator-gated and never destroys data automatically." -ForegroundColor Green
exit 0
