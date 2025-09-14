<#
.SYNOPSIS
    Guides the developer through setting up secrets required for publishing the package.

.DESCRIPTION
    This script prompts you to add secrets and certificates needed to sign and push packages. It does not perform any action by default because secrets should never be committed to source control. Customize this script to fit your workflow.
#>

Write-Host "Configuring development secrets for this package..."

# Prompt for NuGet API key
Write-Host "\nTODO: Add the 'NUGET_API_KEY' secret to your repository secrets (e.g., GitHub) or set it in your environment."

# Import code‑signing certificate
Write-Host "\nTODO: If you plan to sign packages, import your code‑signing certificate (PFX) into the CurrentUser\\My certificate store."
Write-Host "You can do this via the Certificates MMC snap‑in or using PowerShell:"
Write-Host "`$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2('path-to-your.pfx', 'pfxPassword');"
Write-Host "`$store = New-Object System.Security.Cryptography.X509Certificates.X509Store('My','CurrentUser');"
Write-Host "`$store.Open('ReadWrite');"
Write-Host "`$store.Add($cert);"
Write-Host "`$store.Close();"

Write-Host "\nThis script is a stub. Update it with your own implementation tailored to your environment."