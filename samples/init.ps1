Param()
$ErrorActionPreference = 'Stop'
Write-Host "Adding KeelMatrix.QueryWatch package to samples using local NuGet source (../artifacts/packages)..." -ForegroundColor Cyan
dotnet --info | Out-Null

dotnet add ./EFCore.Sqlite/EFCore.Sqlite.csproj package KeelMatrix.QueryWatch
dotnet add ./Ado.Sqlite/Ado.Sqlite.csproj package KeelMatrix.QueryWatch

Write-Host "Restore completed. You can now run the samples." -ForegroundColor Green
