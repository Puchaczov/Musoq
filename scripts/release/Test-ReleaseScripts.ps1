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
Assert-Equal -Expected 7 -Actual $packages.Count -Message 'The composed release catalog must contain all seven public packages.'
Assert-EqualPackageEntries -Expected @('Musoq.Parser', 'Musoq.Formatter', 'Musoq.Plugins', 'Musoq.Schema', 'Musoq.Evaluator', 'Musoq.Converter', 'Musoq.Benchmarks.Scenarios') `
    -Actual @($packages | ForEach-Object PackageId) -Description 'Composed public release catalog'
$formatterRelease = Resolve-ReleaseTag -Tag 'formatter/v17.0.14-alpha.2'
Assert-Equal -Expected 'Musoq.Formatter' -Actual $formatterRelease.Packages[0].PackageId -Message 'Formatter tags must resolve independently.'
Assert-Equal -Expected 'MusoqFormatterVersion' -Actual $formatterRelease.Packages[0].VersionProperty -Message 'Formatter must have an independent version property.'
$registryRoot = Join-Path ([System.IO.Path]::GetTempPath()) "musoq-release-registry-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $registryRoot | Out-Null
$originalRegistryPath = $script:PackageRegistryPath
try {
    $duplicateRegistry = @{ packages = @($packages[0]); additionalManifests = @('additional.json') }
    $script:PackageRegistryPath = Join-Path $registryRoot 'root.json'
    $duplicateRegistry | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $script:PackageRegistryPath
    @{ packages = @($packages[0]) } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $registryRoot 'additional.json')
    $duplicateRejected = $false
    try {
        Get-ReleasePackages | Out-Null
    }
    catch {
        $duplicateRejected = $_.Exception.Message -match 'duplicate package slug'
    }
    Assert-Equal -Expected $true -Actual $duplicateRejected -Message 'Combined manifests must reject duplicate packages.'
    $duplicateRegistry.additionalManifests = @('../outside.json')
    $duplicateRegistry | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $script:PackageRegistryPath
    $escapeRejected = $false
    try {
        Get-ReleasePackages | Out-Null
    }
    catch {
        $escapeRejected = $_.Exception.Message -match 'filenames within the release directory'
    }
    Assert-Equal -Expected $true -Actual $escapeRejected -Message 'Manifest lookup must stay inside the release directory.'

    $bundledTargetsPath = [System.Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'BundledTargets.targets'))
    $bundledProjectPath = Join-Path $registryRoot 'bundled.proj'
    @'
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <BundledTargetAssemblyName Include="Test.Target" />
  </ItemGroup>
  <Import Project="__TARGETS__" />
  <Target Name="Capture" DependsOnTargets="IncludeBundledTargetAssemblies;IncludeBundledTargetAssemblySymbols">
    <WriteLinesToFile File="$(MSBuildProjectDirectory)/bundled-files.txt" Lines="@(BuildOutputInPackage);@(TfmSpecificDebugSymbolsFile)" Overwrite="true" />
  </Target>
</Project>
'@.Replace('__TARGETS__', $bundledTargetsPath) | Set-Content -LiteralPath $bundledProjectPath

    foreach ($layout in @('bin/Release/net10.0', '.artifacts/build/bin/Test.Project/release')) {
        $targetDirectory = Join-Path $registryRoot $layout
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
        foreach ($extension in @('dll', 'xml', 'pdb')) {
            Set-Content -LiteralPath (Join-Path $targetDirectory "Test.Target.$extension") -Value $layout
        }
        $targetDirectory += [System.IO.Path]::DirectorySeparatorChar
        $resolutionOutput = & dotnet msbuild $bundledProjectPath -nologo -verbosity:quiet -target:Capture "-property:TargetDir=$targetDirectory" 2>&1
        Assert-Equal -Expected 0 -Actual $LASTEXITCODE -Message "Bundled targets must resolve the selected $layout output. $resolutionOutput"
        $bundledFiles = @(Get-Content -LiteralPath (Join-Path $registryRoot 'bundled-files.txt'))
        Assert-Equal -Expected 3 -Actual $bundledFiles.Count -Message 'Bundling must retain the assembly, documentation and symbols.'
        foreach ($file in $bundledFiles) {
            Assert-Equal -Expected $targetDirectory.TrimEnd([System.IO.Path]::DirectorySeparatorChar) `
                -Actual (Split-Path -Parent $file) -Message 'Bundling must not select stale files from another output layout.'
        }
    }
    Remove-Item -LiteralPath (Join-Path $targetDirectory 'Test.Target.pdb')
    $missingSymbolOutput = & dotnet msbuild $bundledProjectPath -nologo -verbosity:quiet -target:Capture "-property:TargetDir=$targetDirectory" 2>&1
    Assert-Equal -Expected $true -Actual ($LASTEXITCODE -ne 0) -Message 'Missing symbols must fail bundling even when another layout contains them.'
    Assert-Equal -Expected $true -Actual (($missingSymbolOutput -join ' ') -match 'Expected bundled target symbols') -Message 'Missing-symbol failures must identify the packaging contract.'
}
finally {
    $script:PackageRegistryPath = $originalRegistryPath
    $resolvedRegistryRoot = [System.IO.Path]::GetFullPath($registryRoot)
    $temporaryParent = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedRegistryRoot.StartsWith($temporaryParent, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Registry test cleanup escaped the temporary directory.'
    }
    Remove-Item -LiteralPath $resolvedRegistryRoot -Recurse -Force
}

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

# The expected native failure above must not become the calling CI shell's exit code.
exit 0
