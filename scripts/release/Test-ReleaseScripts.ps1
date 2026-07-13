[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Release.Common.ps1')

function Assert-Equal {
    param(
        [AllowNull()]
        [object] $Expected,

        [AllowNull()]
        [object] $Actual,

        [Parameter(Mandatory)]
        [string] $Message
    )

    if ($Expected -ne $Actual) {
        throw "$Message Expected '$Expected', got '$Actual'."
    }
}

$packages = @(Get-ReleasePackages)
$abiPackages = @($packages | Where-Object IsDatasourceAbi | Select-Object -ExpandProperty PackageId | Sort-Object)
Assert-Equal -Expected 2 -Actual $abiPackages.Count -Message 'Unexpected datasource ABI package count.'
Assert-Equal -Expected 'Musoq.Plugins' -Actual $abiPackages[0] -Message 'Musoq.Plugins must be a datasource ABI package.'
Assert-Equal -Expected 'Musoq.Schema' -Actual $abiPackages[1] -Message 'Musoq.Schema must be a datasource ABI package.'

$baseline = Get-DatasourceAbiBaselineVersion `
    -PackageId 'Musoq.Schema' `
    -Version '17.0.2-alpha.1' `
    -AvailableVersions @('16.9.0', '17.0.0', '17.0.1-alpha.2', '17.0.2-alpha.2', '17.0.2')
Assert-Equal -Expected '17.0.1-alpha.2' -Actual $baseline -Message 'Prerelease baseline selection failed.'

$baseline = Get-DatasourceAbiBaselineVersion `
    -PackageId 'Musoq.Plugins' `
    -Version '17.1.0' `
    -AvailableVersions @('17.0.1-alpha.2', '17.0.1', '17.0.2-alpha.1', '18.0.0-alpha.1')
Assert-Equal -Expected '17.0.2-alpha.1' -Actual $baseline -Message 'Highest same-major baseline selection failed.'

$baseline = Get-DatasourceAbiBaselineVersion `
    -PackageId 'Musoq.Schema' `
    -Version '18.0.0-alpha.1' `
    -AvailableVersions @('16.9.0', '17.0.2')
Assert-Equal -Expected $null -Actual $baseline -Message 'A new major must not use an older-major baseline.'

$failurePropagated = $false
try {
    Invoke-ReleaseCommand -FilePath (Get-Process -Id $PID).Path -Arguments @(
        '-NoProfile',
        '-NonInteractive',
        '-Command',
        'exit 23'
    )
}
catch {
    $failurePropagated = $true
}

Assert-Equal -Expected $true -Actual $failurePropagated -Message 'Release command failures must stop packaging.'

Write-Host 'Release script tests passed.'
