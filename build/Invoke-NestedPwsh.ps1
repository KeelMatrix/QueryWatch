function Invoke-NestedPwsh {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0, ValueFromRemainingArguments = $true)]
        [object[]]$ArgumentList
    )

    $pwshArguments = [System.Collections.Generic.List[object]]::new()
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
            [System.Runtime.InteropServices.OSPlatform]::Windows)) {
        [void]$pwshArguments.Add('-WindowStyle')
        [void]$pwshArguments.Add('Hidden')
    }

    for ($index = 0; $index -lt $ArgumentList.Count; $index++) {
        $argument = $ArgumentList[$index]
        if ($argument -is [string] -and $argument.EndsWith(':') -and
            $index + 1 -lt $ArgumentList.Count -and
            ($ArgumentList[$index + 1] -is [System.Management.Automation.SwitchParameter] -or
             $ArgumentList[$index + 1] -is [bool])) {
            $switch = $ArgumentList[++$index]
            if (($switch -is [System.Management.Automation.SwitchParameter] -and $switch.IsPresent) -or
                ($switch -is [bool] -and $switch)) {
                [void]$pwshArguments.Add($argument.Substring(0, $argument.Length - 1))
            }
            continue
        }
        [void]$pwshArguments.Add($argument)
    }

    $pwshExecutable = 'pwsh'
    $nativeArguments = $pwshArguments.ToArray()
    & $pwshExecutable @nativeArguments
    $global:LASTEXITCODE = $LASTEXITCODE
}
