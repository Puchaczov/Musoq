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

function New-TestWorkflowRun {
    param(
        [string] $Sha = 'release-sha',
        [string] $Event = 'push',
        [string] $Branch = 'feature/alpha',
        [string] $Status = 'completed',
        [string] $Conclusion = 'success',
        [int] $Id = 1,
        [string] $UpdatedAt = '2026-09-17T00:00:00Z'
    )

    return [PSCustomObject]@{
        id = $Id
        head_sha = $Sha
        event = $Event
        head_branch = $Branch
        status = $Status
        conclusion = $Conclusion
        updated_at = $UpdatedAt
        html_url = "https://example.test/runs/$Id"
    }
}

$packages = @(Get-ReleasePackages)
$alphaVersionAllowsBreakingChanges = Test-AlphaReleaseVersion -Version '17.0.9-alpha.1'
Assert-Equal -Expected $true -Actual $alphaVersionAllowsBreakingChanges -Message 'All-packages alpha versions must allow intentional breaking changes.'

$previewVersionAllowsBreakingChanges = Test-AlphaReleaseVersion -Version '17.0.9-preview.1'
Assert-Equal -Expected $false -Actual $previewVersionAllowsBreakingChanges -Message 'All-packages preview versions must retain compatibility validation.'

$stableVersionAllowsBreakingChanges = Test-AlphaReleaseVersion -Version '17.0.9'
Assert-Equal -Expected $false -Actual $stableVersionAllowsBreakingChanges -Message 'All-packages stable versions must retain compatibility validation.'

$alphaRelease = Resolve-ReleaseTag -Tag 'v17.0.9-alpha.1'
Assert-Equal -Expected $true -Actual $alphaRelease.AllowBreakingChanges -Message 'Alpha releases must explicitly allow breaking changes.'

$previewRelease = Resolve-ReleaseTag -Tag 'v17.0.9-preview.1'
Assert-Equal -Expected $false -Actual $previewRelease.AllowBreakingChanges -Message 'Preview releases must retain compatibility validation.'

$stableRelease = Resolve-ReleaseTag -Tag 'v17.0.9'
Assert-Equal -Expected $false -Actual $stableRelease.AllowBreakingChanges -Message 'Stable releases must retain compatibility validation.'

$alphaSummary = New-ReleaseSummary -Release $alphaRelease
Assert-Equal -Expected $true -Actual $alphaSummary.isAlpha -Message 'Alpha release summaries must identify alpha releases.'
Assert-Equal -Expected $false -Actual $alphaSummary.requiresReleaseBranch -Message 'Alpha releases must not require release-branch provenance.'

$previewSummary = New-ReleaseSummary -Release $previewRelease
Assert-Equal -Expected $true -Actual $previewSummary.requiresReleaseBranch -Message 'Preview releases must require release-branch provenance.'

$stableSummary = New-ReleaseSummary -Release $stableRelease
Assert-Equal -Expected $true -Actual $stableSummary.requiresReleaseBranch -Message 'Stable releases must require release-branch provenance.'

$alphaDecision = Get-CiQualificationDecision `
    -WorkflowRuns @(
        (New-TestWorkflowRun -Sha 'alpha-sha' -Branch 'feature/alpha' -Id 11),
        (New-TestWorkflowRun -Sha 'alpha-sha' -Event 'pull_request' -Branch 'feature/alpha' -Id 12)
    ) `
    -CommitSha 'alpha-sha'
Assert-Equal -Expected 'Success' -Actual $alphaDecision.Status -Message 'Alpha releases must accept successful push CI from an arbitrary branch.'
Assert-Equal -Expected 'feature/alpha' -Actual $alphaDecision.Run.head_branch -Message 'Alpha qualification must report the accepted branch.'

$pullRequestOnlyDecision = Get-CiQualificationDecision `
    -WorkflowRuns @(
        (New-TestWorkflowRun -Sha 'pr-sha' -Event 'pull_request' -Branch 'feature/alpha' -Id 13)
    ) `
    -CommitSha 'pr-sha'
Assert-Equal -Expected 'Waiting' -Actual $pullRequestOnlyDecision.Status -Message 'Pull-request CI must not qualify a release.'

$wrongShaDecision = Get-CiQualificationDecision `
    -WorkflowRuns @(
        (New-TestWorkflowRun -Sha 'other-sha' -Branch 'feature/alpha' -Id 14)
    ) `
    -CommitSha 'expected-sha'
Assert-Equal -Expected 'Waiting' -Actual $wrongShaDecision.Status -Message 'CI for another SHA must not qualify a release.'

$nonAlphaDecision = Get-CiQualificationDecision `
    -WorkflowRuns @(
        (New-TestWorkflowRun -Sha 'stable-sha' -Branch 'master' -Id 15),
        (New-TestWorkflowRun -Sha 'stable-sha' -Branch 'release/17.0.9' -Id 16)
    ) `
    -CommitSha 'stable-sha' `
    -RequireReleaseBranch
Assert-Equal -Expected 'Success' -Actual $nonAlphaDecision.Status -Message 'Non-alpha releases must accept successful CI from release/**.'
Assert-Equal -Expected 'release/17.0.9' -Actual $nonAlphaDecision.Run.head_branch -Message 'Non-alpha qualification must select release/** evidence.'

$masterOnlyDecision = Get-CiQualificationDecision `
    -WorkflowRuns @(
        (New-TestWorkflowRun -Sha 'stable-sha' -Branch 'master' -Id 17)
    ) `
    -CommitSha 'stable-sha' `
    -RequireReleaseBranch
Assert-Equal -Expected 'Failure' -Actual $masterOnlyDecision.Status -Message 'Non-alpha releases must reject master-only CI evidence.'

$pendingDecision = Get-CiQualificationDecision `
    -WorkflowRuns @(
        (New-TestWorkflowRun -Sha 'pending-sha' -Status 'in_progress' -Conclusion '' -Id 18)
    ) `
    -CommitSha 'pending-sha'
Assert-Equal -Expected 'Waiting' -Actual $pendingDecision.Status -Message 'Pending CI must keep publication waiting.'

$failedDecision = Get-CiQualificationDecision `
    -WorkflowRuns @(
        (New-TestWorkflowRun -Sha 'failed-sha' -Conclusion 'failure' -Id 19),
        (New-TestWorkflowRun -Sha 'failed-sha' -Conclusion 'cancelled' -Id 20)
    ) `
    -CommitSha 'failed-sha'
Assert-Equal -Expected 'Failure' -Actual $failedDecision.Status -Message 'Failed or canceled CI must block publication.'

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
