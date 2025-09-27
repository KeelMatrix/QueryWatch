Param()
$ErrorActionPreference = 'Stop'
Write-Host "Adding KeelMatrix.QueryWatch packages to samples using local NuGet source (../artifacts/packages)..." -ForegroundColor Cyan
dotnet --info | Out-Null

dotnet add ./EFCore.Sqlite/EFCore.Sqlite.csproj package KeelMatrix.QueryWatch
dotnet add ./EFCore.Sqlite/EFCore.Sqlite.csproj package KeelMatrix.QueryWatch.EfCore
dotnet add ./Ado.Sqlite/Ado.Sqlite.csproj package KeelMatrix.QueryWatch
dotnet add ./Dapper.Sqlite/Dapper.Sqlite.csproj package KeelMatrix.QueryWatch

Write-Host "Restore completed. You can now run the samples." -ForegroundColor Green

