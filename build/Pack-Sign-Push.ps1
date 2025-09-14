<#
.SYNOPSIS
    Locally pack, sign, and push your NuGet package.

.DESCRIPTION
    This script restores, builds, packs, optionally signs and pushes your package. It is provided as a convenience
    for local workflows and mirrors the CI release pipeline. Customize it according to your signing and publishing
    requirements. Secrets and certificates must be provided outside of source control.
#>

$ErrorActionPreference = 'Stop'

Write-Host "Restoring..."
dotnet restore

Write-Host "Building (Release)..."
dotnet build --configuration Release --no-restore

Write-Host "Packing..."
dotnet pack --configuration Release --no-build --include-symbols --p:SymbolPackageFormat=snupkg --output ./artifacts/packages

# Signing packages (optional)
Write-Host "Signing packages... (stub)"
Write-Host "TODO: Uncomment and configure the following lines once your certificate is installed:"
# Get-ChildItem -Path ./artifacts/packages -Filter *.nupkg | ForEach-Object {
#     dotnet nuget sign $_.FullName --certificate-fingerprint YOUR_CERT_FINGERPRINT --timestamper http://timestamp.digicert.com
# }

# Pushing packages
Write-Host "Pushing packages... (stub)"
Write-Host "TODO: Provide an API key and adjust the source URL as needed:"
# $apiKey = 'YOUR_NUGET_API_KEY'
# Get-ChildItem -Path ./artifacts/packages -Filter *.nupkg | ForEach-Object {
#     dotnet nuget push $_.FullName --source https://api.nuget.org/v3/index.json --api-key $apiKey --skip-duplicate
# }

Write-Host "Done. Packages are available in ./artifacts/packages."