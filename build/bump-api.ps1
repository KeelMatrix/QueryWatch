[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [string]$Solution = "KeelMatrix.QueryWatch.sln"
)

Write-Host "==> Restoring packages..." -ForegroundColor Cyan
dotnet restore $Solution

Write-Host "==> Building ($Configuration)..." -ForegroundColor Cyan
dotnet build $Solution -c $Configuration --no-restore

Write-Host "==> Applying Public API analyzer fixes (RS0016/RS0017)..." -ForegroundColor Cyan
dotnet format analyzers $Solution --diagnostics RS0016,RS0017 --no-restore --verbosity minimal

Write-Host "==> Formatting whitespace..." -ForegroundColor Cyan
dotnet format whitespace $Solution --no-restore --verbosity quiet

Write-Host "==> Validating build..." -ForegroundColor Cyan
dotnet build $Solution -c $Configuration --no-restore

Write-Host "==> Done. Review changes under *PublicAPI.Unshipped.txt* and commit them." -ForegroundColor Green
