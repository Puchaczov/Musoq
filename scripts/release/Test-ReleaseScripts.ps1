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
$alphaRelease = Resolve-ReleaseTag -Tag 'v17.0.9-alpha.1'
Assert-Equal -Expected $true -Actual $alphaRelease.AllowBreakingChanges -Message 'Alpha releases must explicitly allow breaking changes.'

$previewRelease = Resolve-ReleaseTag -Tag 'v17.0.9-preview.1'
Assert-Equal -Expected $false -Actual $previewRelease.AllowBreakingChanges -Message 'Preview releases must retain compatibility validation.'

$stableRelease = Resolve-ReleaseTag -Tag 'v17.0.9'
Assert-Equal -Expected $false -Actual $stableRelease.AllowBreakingChanges -Message 'Stable releases must retain compatibility validation.'

$abiPackages = @($packages | Where-Object IsDatasourceAbi | Select-Object -ExpandProperty PackageId | Sort-Object)
Assert-Equal -Expected 2 -Actual $abiPackages.Count -Message 'Unexpected datasource ABI package count.'
Assert-Equal -Expected 'Musoq.Plugins' -Actual $abiPackages[0] -Message 'Musoq.Plugins must be a datasource ABI package.'
Assert-Equal -Expected 'Musoq.Schema' -Actual $abiPackages[1] -Message 'Musoq.Schema must be a datasource ABI package.'

$converterTargets = @(Get-ExpectedConsumerTargetAssemblies -Packages @(
    [PSCustomObject]@{ PackageId = 'Musoq.Converter' }
))
Assert-Equal -Expected 4 -Actual $converterTargets.Count -Message 'Converter consumer target assembly count changed.'
Assert-Equal -Expected 'Musoq.Targets.Execution.dll' -Actual $converterTargets[1] -Message 'Converter consumer must receive the execution target.'

$evaluatorTargets = @(Get-ExpectedConsumerTargetAssemblies -Packages @(
    [PSCustomObject]@{ PackageId = 'Musoq.Evaluator' }
))
Assert-Equal -Expected 1 -Actual $evaluatorTargets.Count -Message 'Evaluator consumer target assembly count changed.'
Assert-Equal -Expected 'Musoq.Targets.Abstractions.dll' -Actual $evaluatorTargets[0] -Message 'Evaluator consumer must receive only target abstractions.'

$parserTargets = @(Get-ExpectedConsumerTargetAssemblies -Packages @(
    [PSCustomObject]@{ PackageId = 'Musoq.Parser' }
))
Assert-Equal -Expected 0 -Actual $parserTargets.Count -Message 'Parser consumer must not receive target assemblies.'

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
