[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..')
)

$ErrorActionPreference = 'Stop'
$script:Failures = [System.Collections.Generic.List[string]]::new()
$canonicalRepositoryUrl = 'https://github.com/KeelMatrix/QueryWatch'
$knownVisibilityTags = @(
    'keelmatrix-public-product',
    'keelmatrix-internal-package'
)
$knownRoleTags = @(
    'keelmatrix-primary',
    'keelmatrix-component'
)

function Add-Failure {
    param([string]$Message)
    $script:Failures.Add($Message)
}

function Get-JsonProperty {
    param(
        [System.Text.Json.JsonElement]$Element,
        [string]$Name
    )

    foreach ($property in $Element.EnumerateObject()) {
        if ($property.Name -ceq $Name) {
            return $property.Value
        }
    }

    return $null
}

function Assert-JsonObjectShape {
    param(
        [System.Text.Json.JsonElement]$Element,
        [string]$Context,
        [string[]]$ExpectedNames
    )

    if ($Element.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
        Add-Failure "$Context must be a JSON object."
        return
    }

    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($property in $Element.EnumerateObject()) {
        if (-not $seen.Add($property.Name)) {
            Add-Failure "$Context contains duplicate property '$($property.Name)'."
        }
        if ($ExpectedNames -cnotcontains $property.Name) {
            Add-Failure "$Context contains unexpected property '$($property.Name)'."
        }
    }

    foreach ($expectedName in $ExpectedNames) {
        if (-not $seen.Contains($expectedName)) {
            Add-Failure "$Context is missing property '$expectedName'."
        }
    }
}

function Get-UnconditionalProperties {
    param([string]$ProjectPath)

    $projectXml = [System.Xml.XmlDocument]::new()
    $projectXml.XmlResolver = $null
    $projectXml.Load($ProjectPath)

    $properties = [System.Collections.Generic.Dictionary[string, System.Collections.Generic.List[string]]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($group in $projectXml.DocumentElement.ChildNodes) {
        if ($group.NodeType -ne [System.Xml.XmlNodeType]::Element -or $group.LocalName -ne 'PropertyGroup') {
            continue
        }
        if ($group.Attributes['Condition'] -and -not [string]::IsNullOrWhiteSpace($group.Attributes['Condition'].Value)) {
            continue
        }

        foreach ($property in $group.ChildNodes) {
            if ($property.NodeType -ne [System.Xml.XmlNodeType]::Element) {
                continue
            }
            if ($property.Attributes['Condition'] -and -not [string]::IsNullOrWhiteSpace($property.Attributes['Condition'].Value)) {
                continue
            }

            if (-not $properties.ContainsKey($property.LocalName)) {
                $properties.Add($property.LocalName, [System.Collections.Generic.List[string]]::new())
            }
            $properties[$property.LocalName].Add($property.InnerText.Trim())
        }
    }

    return ,$properties
}

function Get-RequiredProperty {
    param(
        [System.Collections.Generic.Dictionary[string, System.Collections.Generic.List[string]]]$Properties,
        [string]$Name,
        [string]$ProjectName
    )

    if (-not $Properties.ContainsKey($Name)) {
        Add-Failure "$ProjectName must define '$Name' in an unconditional PropertyGroup."
        return $null
    }
    if ($Properties[$Name].Count -ne 1) {
        Add-Failure "$ProjectName must define '$Name' exactly once; found $($Properties[$Name].Count)."
    }

    return $Properties[$Name][-1]
}

try {
    $root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
    $manifestPath = Join-Path $root 'keelmatrix.website.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Missing repository manifest: $manifestPath"
    }

    $manifestDocument = [System.Text.Json.JsonDocument]::Parse([System.IO.File]::ReadAllText($manifestPath))
    try {
        $manifestRoot = $manifestDocument.RootElement
        Assert-JsonObjectShape -Element $manifestRoot -Context 'Manifest root' -ExpectedNames @('schemaVersion', 'packages')

        $schemaVersion = Get-JsonProperty -Element $manifestRoot -Name 'schemaVersion'
        if ($null -ne $schemaVersion -and ($schemaVersion.ValueKind -ne [System.Text.Json.JsonValueKind]::Number -or $schemaVersion.GetInt32() -ne 1)) {
            Add-Failure 'Manifest schemaVersion must be the integer 1.'
        }

        $packagesElement = Get-JsonProperty -Element $manifestRoot -Name 'packages'
        $manifestPackages = [System.Collections.Generic.List[object]]::new()
        $manifestIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        $manifestIdsIgnoreCase = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

        if ($null -eq $packagesElement -or $packagesElement.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
            Add-Failure "Manifest 'packages' must be a JSON object."
        }
        else {
            foreach ($packageProperty in $packagesElement.EnumerateObject()) {
                $packageId = $packageProperty.Name
                if (-not $manifestIds.Add($packageId) -or -not $manifestIdsIgnoreCase.Add($packageId)) {
                    Add-Failure "Manifest package ID '$packageId' is duplicated or differs only by casing."
                    continue
                }

                Assert-JsonObjectShape -Element $packageProperty.Value -Context "Manifest package '$packageId'" -ExpectedNames @('visibility', 'role')
                $visibilityElement = Get-JsonProperty -Element $packageProperty.Value -Name 'visibility'
                $roleElement = Get-JsonProperty -Element $packageProperty.Value -Name 'role'
                if ($null -eq $visibilityElement -or $visibilityElement.ValueKind -ne [System.Text.Json.JsonValueKind]::String) {
                    Add-Failure "Manifest package '$packageId' visibility must be a string."
                    continue
                }
                if ($null -eq $roleElement -or $roleElement.ValueKind -ne [System.Text.Json.JsonValueKind]::String) {
                    Add-Failure "Manifest package '$packageId' role must be a string."
                    continue
                }

                $visibility = $visibilityElement.GetString()
                $role = $roleElement.GetString()
                if ($visibility -cnotin @('public-product', 'internal-package')) {
                    Add-Failure "Manifest package '$packageId' has unsupported visibility '$visibility'."
                }
                if ($role -cnotin @('primary', 'component')) {
                    Add-Failure "Manifest package '$packageId' has unsupported role '$role'."
                }
                $manifestPackages.Add([pscustomobject]@{
                    PackageId = $packageId
                    Visibility = $visibility
                    Role = $role
                })
            }
        }

        $publicPackages = @($manifestPackages | Where-Object { $_.Visibility -ceq 'public-product' })
        $primaries = @($manifestPackages | Where-Object { $_.Role -ceq 'primary' })
        $publicPrimaries = @($publicPackages | Where-Object { $_.Role -ceq 'primary' })
        if ($publicPackages.Count -eq 0) {
            Add-Failure 'Manifest must classify at least one package as public-product.'
        }
        if ($primaries.Count -ne 1) {
            Add-Failure "Manifest must contain exactly one primary package; found $($primaries.Count)."
        }
        if ($publicPrimaries.Count -ne 1) {
            Add-Failure "Manifest must contain exactly one primary public-product package; found $($publicPrimaries.Count)."
        }

        $projectFiles = Get-ChildItem -LiteralPath $root -Filter '*.csproj' -File -Recurse |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
        $sourcePackages = [System.Collections.Generic.List[object]]::new()
        $sourceIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        $sourceIdsIgnoreCase = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

        foreach ($projectFile in $projectFiles) {
            $projectName = [System.IO.Path]::GetRelativePath($root, $projectFile.FullName).Replace('\', '/')
            $properties = Get-UnconditionalProperties -ProjectPath $projectFile.FullName
            $isPackable = if ($properties.ContainsKey('IsPackable')) { $properties['IsPackable'][-1] } else { 'true' }
            if ($isPackable -ieq 'false') {
                continue
            }
            if ($isPackable -ine 'true') {
                Add-Failure "$projectName has unsupported IsPackable value '$isPackable'."
                continue
            }

            $packageId = Get-RequiredProperty -Properties $properties -Name 'PackageId' -ProjectName $projectName
            if ([string]::IsNullOrWhiteSpace($packageId)) {
                Add-Failure "$projectName must have a non-empty PackageId."
                continue
            }
            if (-not $sourceIds.Add($packageId) -or -not $sourceIdsIgnoreCase.Add($packageId)) {
                Add-Failure "Packable package ID '$packageId' is duplicated or differs only by casing."
                continue
            }

            $metadata = @{}
            foreach ($name in @('Description', 'PackageTags', 'Authors', 'PackageProjectUrl', 'RepositoryUrl', 'RepositoryType', 'PublishRepositoryUrl', 'PackageReadmeFile', 'PackageLicenseExpression')) {
                $metadata[$name] = Get-RequiredProperty -Properties $properties -Name $name -ProjectName $projectName
            }

            if ([string]::IsNullOrWhiteSpace($metadata.Description)) {
                Add-Failure "$projectName must have a non-empty package description."
            }
            if ($metadata.Authors -cne 'KeelMatrix') {
                Add-Failure "$projectName Authors must be 'KeelMatrix'."
            }
            if ($metadata.PackageProjectUrl -cne $canonicalRepositoryUrl) {
                Add-Failure "$projectName PackageProjectUrl must be '$canonicalRepositoryUrl'."
            }
            if ($metadata.RepositoryUrl -cne $canonicalRepositoryUrl) {
                Add-Failure "$projectName RepositoryUrl must be '$canonicalRepositoryUrl'."
            }
            if ($metadata.RepositoryType -cne 'git') {
                Add-Failure "$projectName RepositoryType must be 'git'."
            }
            if ($metadata.PublishRepositoryUrl -cne 'true') {
                Add-Failure "$projectName PublishRepositoryUrl must be 'true'."
            }
            if ($metadata.PackageLicenseExpression -cne 'MIT') {
                Add-Failure "$projectName PackageLicenseExpression must be 'MIT'."
            }
            if ([string]::IsNullOrWhiteSpace($metadata.PackageReadmeFile)) {
                Add-Failure "$projectName PackageReadmeFile must be set."
            }
            else {
                $readmePath = Join-Path $projectFile.DirectoryName $metadata.PackageReadmeFile
                if (-not (Test-Path -LiteralPath $readmePath -PathType Leaf)) {
                    Add-Failure "$projectName package README '$($metadata.PackageReadmeFile)' does not exist."
                }
            }

            $tagTokens = @($metadata.PackageTags -split ';' | Where-Object { $_.Length -gt 0 })
            $manifestPackage = @($manifestPackages | Where-Object { $_.PackageId -ceq $packageId }) | Select-Object -First 1
            if ($null -eq $manifestPackage) {
                Add-Failure "Packable package '$packageId' in $projectName is missing from the repository manifest."
            }
            else {
                $expectedVisibilityTag = if ($manifestPackage.Visibility -ceq 'public-product') { 'keelmatrix-public-product' } else { 'keelmatrix-internal-package' }
                $expectedRoleTag = if ($manifestPackage.Role -ceq 'primary') { 'keelmatrix-primary' } else { 'keelmatrix-component' }
                $visibilityTags = @($tagTokens | Where-Object { $knownVisibilityTags -icontains $_ })
                $roleTags = @($tagTokens | Where-Object { $knownRoleTags -icontains $_ })
                if ($visibilityTags.Count -ne 1) {
                    Add-Failure "$projectName must contain exactly one website visibility sentinel; found $($visibilityTags.Count)."
                }
                elseif ($visibilityTags[0] -cne $expectedVisibilityTag) {
                    Add-Failure "$projectName visibility sentinel '$($visibilityTags[0])' disagrees with manifest value '$($manifestPackage.Visibility)' or has incorrect casing."
                }
                if ($roleTags.Count -ne 1) {
                    Add-Failure "$projectName must contain exactly one website role sentinel; found $($roleTags.Count)."
                }
                elseif ($roleTags[0] -cne $expectedRoleTag) {
                    Add-Failure "$projectName role sentinel '$($roleTags[0])' disagrees with manifest value '$($manifestPackage.Role)' or has incorrect casing."
                }
            }

            $packAsTool = if ($properties.ContainsKey('PackAsTool')) { $properties['PackAsTool'][-1] } else { 'false' }
            $toolCommandName = if ($properties.ContainsKey('ToolCommandName')) { $properties['ToolCommandName'][-1] } else { '' }
            if ($packageId -ceq 'qwatch') {
                if ($packAsTool -ine 'true' -or $toolCommandName -cne 'qwatch') {
                    Add-Failure "$projectName must declare PackAsTool=true and ToolCommandName=qwatch."
                }
            }
            elseif ($packAsTool -ieq 'true' -or -not [string]::IsNullOrWhiteSpace($toolCommandName)) {
                Add-Failure "$projectName must remain a dependency package and must not declare .NET tool metadata."
            }

            $sourcePackages.Add([pscustomobject]@{
                PackageId = $packageId
                ProjectName = $projectName
            })
        }

        foreach ($sourcePackage in $sourcePackages) {
            if (-not $manifestIds.Contains($sourcePackage.PackageId)) {
                Add-Failure "Packable package '$($sourcePackage.PackageId)' has no manifest entry."
            }
        }
        foreach ($manifestPackage in $manifestPackages) {
            if (-not $sourceIds.Contains($manifestPackage.PackageId)) {
                Add-Failure "Manifest package '$($manifestPackage.PackageId)' has no packable source project."
            }
        }
        if ($sourcePackages.Count -eq 0) {
            Add-Failure 'No packable package projects were found.'
        }
    }
    finally {
        if ($null -ne $manifestDocument) {
            $manifestDocument.Dispose()
        }
    }
}
catch {
    Add-Failure $_.Exception.Message
}

if ($script:Failures.Count -gt 0) {
    foreach ($failure in $script:Failures) {
        [Console]::Error.WriteLine("ERROR: $failure")
    }
    exit 1
}

Write-Output "Website metadata contract is valid for $($sourcePackages.Count) package projects."
