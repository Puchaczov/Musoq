[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Tag,

    [switch] $Json
)

. (Join-Path $PSScriptRoot 'Release.Common.ps1')

$release = Resolve-ReleaseTag -Tag $Tag
Test-ReleaseVersionProperties -Release $release

$summary = New-ReleaseSummary -Release $release

if ($Json) {
    $summary | ConvertTo-Json -Compress
    return
}

Write-Host "Release tag validated: $($summary.tag)"
Write-Host "Release mode: $($summary.mode)"
Write-Host "Release version: $($summary.version)"
Write-Host "Packages: $($summary.packages -join ', ')"
