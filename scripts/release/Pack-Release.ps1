[CmdletBinding(DefaultParameterSetName = 'Tag')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Tag')]
    [string] $Tag,

    [Parameter(Mandatory, ParameterSetName = 'AllPackages')]
    [switch] $AllPackages,

    [Parameter()]
    [string] $OutputPath = 'artifacts/nupkgs'
)

. (Join-Path $PSScriptRoot 'Release.Common.ps1')

$repositoryRoot = Get-RepositoryRoot
$resolvedOutputPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    [System.IO.Path]::GetFullPath($OutputPath)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))
}

New-Item -ItemType Directory -Force -Path $resolvedOutputPath | Out-Null

if ($PSCmdlet.ParameterSetName -eq 'AllPackages') {
    $packages = @(Get-ReleasePackages)
    $release = [PSCustomObject]@{
        Tag = 'all-packages'
        Mode = 'AllPackages'
        Version = $null
        AllowBreakingChanges = $false
        Packages = $packages
    }
}
else {
    $release = Resolve-ReleaseTag -Tag $Tag
    Test-ReleaseVersionProperties -Release $release
}

$manifestPath = Join-Path $resolvedOutputPath 'release-package-files.txt'
$manifestEntries = [System.Collections.Generic.List[string]]::new()

foreach ($package in $release.Packages) {
    Write-Host "Packing $($package.PackageId)"
    $packArguments = @(
        'pack',
        $package.FullProjectPath,
        '--configuration',
        'Release',
        '--no-build',
        '--output',
        $resolvedOutputPath,
        '--nologo',
        '--verbosity',
        'quiet'
    )
    $restoreArguments = @()

    if ($package.IsDatasourceAbi -and $release.AllowBreakingChanges) {
        Write-Host "Skipping datasource ABI compatibility validation for intentional alpha release $($release.Version)."
    }
    elseif ($package.IsDatasourceAbi) {
        $version = Get-MsBuildProperty -ProjectPath $package.FullProjectPath -PropertyName 'Version'
        $baselineVersion = Get-DatasourceAbiBaselineVersion `
            -PackageId $package.PackageId `
            -Version $version

        if ([string]::IsNullOrWhiteSpace($baselineVersion)) {
            Write-Host "No earlier same-major baseline exists for $($package.PackageId) $version."
        }
        else {
            Write-Host "Validating $($package.PackageId) $version against $baselineVersion."
            $validationArguments = @(
                '-p:EnablePackageValidation=true',
                "-p:PackageValidationBaselineName=$($package.PackageId)",
                "-p:PackageValidationBaselineVersion=$baselineVersion",
                '-p:EnableStrictModeForCompatibleTfms=true',
                '-p:GenerateCompatibilitySuppressionFile=false'
            )
            $restoreArguments = @(
                'restore',
                $package.FullProjectPath,
                '--nologo',
                '--verbosity',
                'quiet'
            ) + $validationArguments
            $packArguments += $validationArguments
        }
    }

    if ($restoreArguments.Count -gt 0) {
        Invoke-ReleaseCommand -FilePath 'dotnet' -Arguments $restoreArguments
    }

    Invoke-ReleaseCommand -FilePath 'dotnet' -Arguments $packArguments

    $version = Get-MsBuildProperty -ProjectPath $package.FullProjectPath -PropertyName 'Version'
    $nupkgPath = Join-Path $resolvedOutputPath "$($package.PackageId).$version.nupkg"
    $snupkgPath = Join-Path $resolvedOutputPath "$($package.PackageId).$version.snupkg"

    if (-not (Test-Path -LiteralPath $nupkgPath)) {
        throw "Expected package was not produced for '$($package.PackageId)'."
    }

    if (-not (Test-Path -LiteralPath $snupkgPath)) {
        throw "Expected symbol package was not produced for '$($package.PackageId)'."
    }

    $manifestEntries.Add($nupkgPath)
    $manifestEntries.Add($snupkgPath)
}

Set-Content -LiteralPath $manifestPath -Value $manifestEntries -Encoding UTF8
Write-Host "Package manifest: $manifestPath"
