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
    Invoke-ReleaseCommand -FilePath 'dotnet' -Arguments @(
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
