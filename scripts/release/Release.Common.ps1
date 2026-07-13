Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:RepositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$script:PackageRegistryPath = Join-Path $PSScriptRoot 'packages.json'

function Get-RepositoryRoot {
    return $script:RepositoryRoot
}

function Resolve-RepositoryPath {
    param(
        [Parameter(Mandatory)]
        [string] $RelativePath
    )

    if ([System.IO.Path]::IsPathRooted($RelativePath)) {
        throw "Release registry paths must be repository-relative."
    }

    if ($RelativePath -match '(^|[\\/])\.\.([\\/]|$)') {
        throw "Release registry paths must not contain parent directory segments."
    }

    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $script:RepositoryRoot $RelativePath))
    $rootWithSeparator = $script:RepositoryRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

    if (-not $fullPath.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Release registry path resolves outside the repository."
    }

    return $fullPath
}

function Get-ReleasePackages {
    if (-not (Test-Path -LiteralPath $script:PackageRegistryPath)) {
        throw "Release package registry was not found."
    }

    $registry = Get-Content -LiteralPath $script:PackageRegistryPath -Raw | ConvertFrom-Json
    if ($null -eq $registry.packages -or $registry.packages.Count -eq 0) {
        throw "Release package registry does not contain any packages."
    }

    $seenSlugs = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $seenPackageIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $packages = @()

    foreach ($package in $registry.packages) {
        if ($package.slug -notmatch '^[a-z][a-z0-9-]*$') {
            throw "Release package registry contains an invalid package slug."
        }

        if (-not $seenSlugs.Add([string]$package.slug)) {
            throw "Release package registry contains a duplicate package slug."
        }

        if ($package.packageId -notmatch '^Musoq\.[A-Za-z0-9.]+$') {
            throw "Release package registry contains an invalid package id."
        }

        if (-not $seenPackageIds.Add([string]$package.packageId)) {
            throw "Release package registry contains a duplicate package id."
        }

        if ($package.versionProperty -notmatch '^Musoq[A-Za-z0-9]+Version$') {
            throw "Release package registry contains an invalid version property."
        }

        if ($package.smokeTestMode -notin @('assembly-load', 'typed-query')) {
            throw "Release package registry contains an invalid smoke test mode."
        }

        $isDatasourceAbi = $package.PSObject.Properties.Name -contains 'datasourceAbi' -and
            [bool]$package.datasourceAbi

        $fullProjectPath = Resolve-RepositoryPath -RelativePath ([string]$package.projectPath)
        if (-not (Test-Path -LiteralPath $fullProjectPath)) {
            throw "Release package project was not found."
        }

        $packages += [PSCustomObject]@{
            Slug = [string]$package.slug
            PackageId = [string]$package.packageId
            ProjectPath = [string]$package.projectPath
            FullProjectPath = $fullProjectPath
            VersionProperty = [string]$package.versionProperty
            SmokeTestMode = [string]$package.smokeTestMode
            IsDatasourceAbi = $isDatasourceAbi
        }
    }

    return $packages
}

function ConvertTo-MusoqSemanticVersion {
    param(
        [Parameter(Mandatory)]
        [string] $Version
    )

    try {
        return [System.Management.Automation.SemanticVersion]::Parse($Version)
    }
    catch {
        throw "Invalid semantic version '$Version'."
    }
}

function Get-PublishedPackageVersions {
    param(
        [Parameter(Mandatory)]
        [string] $PackageId
    )

    $baseUrl = if ([string]::IsNullOrWhiteSpace($env:MUSOQ_NUGET_FLAT_CONTAINER_BASE_URL)) {
        'https://api.nuget.org/v3-flatcontainer'
    }
    else {
        $env:MUSOQ_NUGET_FLAT_CONTAINER_BASE_URL.TrimEnd('/')
    }

    $packageSegment = $PackageId.ToLowerInvariant()
    $indexUrl = "$baseUrl/$packageSegment/index.json"

    try {
        $response = Invoke-RestMethod -Method Get -Uri $indexUrl
    }
    catch {
        throw "Failed to query published versions for '$PackageId' from '$indexUrl'. $($_.Exception.Message)"
    }

    if ($null -eq $response.versions) {
        throw "NuGet version index for '$PackageId' did not contain a versions array."
    }

    return @($response.versions | ForEach-Object { [string]$_ })
}

function Get-DatasourceAbiBaselineVersion {
    param(
        [Parameter(Mandatory)]
        [string] $PackageId,

        [Parameter(Mandatory)]
        [string] $Version,

        [string[]] $AvailableVersions
    )

    $current = ConvertTo-MusoqSemanticVersion -Version $Version
    $publishedVersions = if ($PSBoundParameters.ContainsKey('AvailableVersions')) {
        @($AvailableVersions)
    }
    else {
        @(Get-PublishedPackageVersions -PackageId $PackageId)
    }

    $candidates = foreach ($publishedVersion in $publishedVersions) {
        try {
            $parsed = ConvertTo-MusoqSemanticVersion -Version $publishedVersion
        }
        catch {
            continue
        }

        if ($parsed.Major -ne $current.Major -or $parsed.CompareTo($current) -ge 0) {
            continue
        }

        [PSCustomObject]@{
            Parsed = $parsed
            Raw = $publishedVersion
        }
    }

    $baseline = $candidates | Sort-Object -Property Parsed -Descending | Select-Object -First 1
    if ($null -eq $baseline) {
        return $null
    }

    return [string]$baseline.Raw
}

function Get-MsBuildProperty {
    param(
        [Parameter(Mandatory)]
        [string] $ProjectPath,

        [Parameter(Mandatory)]
        [string] $PropertyName
    )

    $output = & dotnet msbuild $ProjectPath "-getProperty:$PropertyName" -nologo -v:quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to evaluate MSBuild property '$PropertyName'. $($output -join [Environment]::NewLine)"
    }

    $value = $output |
        ForEach-Object { [string]$_ } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Last 1

    if ($null -eq $value) {
        return ''
    }

    return $value.Trim()
}

function Resolve-ReleaseTag {
    param(
        [Parameter(Mandatory)]
        [string] $Tag
    )

    if ([string]::IsNullOrWhiteSpace($Tag) -or $Tag -ne $Tag.Trim()) {
        throw "Invalid release tag format."
    }

    if ($Tag -match '[\s;&|`$<>(){}\[\]''"\\]') {
        throw "Invalid release tag format."
    }

    $semVerPattern = '(?<major>0|[1-9][0-9]*)\.(?<minor>0|[1-9][0-9]*)\.(?<patch>0|[1-9][0-9]*)(?:-(?<prerelease>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?'
    $fullTrainPattern = "^v$semVerPattern$"
    $packagePattern = "^(?<slug>[a-z][a-z0-9-]*)/v$semVerPattern$"

    $packages = @(Get-ReleasePackages)
    $mode = $null
    $slug = $null

    if ($Tag -match $fullTrainPattern) {
        $mode = 'FullTrain'
        $selectedPackages = $packages
    }
    elseif ($Tag -match $packagePattern) {
        $mode = 'Package'
        $slug = $Matches.slug
        $selectedPackages = @($packages | Where-Object { $_.Slug -eq $slug })

        if ($selectedPackages.Count -ne 1) {
            throw "Unknown release package slug."
        }
    }
    else {
        throw "Invalid release tag format."
    }

    $major = [int]$Matches.major
    $minor = [int]$Matches.minor
    $patch = [int]$Matches.patch
    $prerelease = if ($Matches.ContainsKey('prerelease')) { [string]$Matches.prerelease } else { '' }
    $version = "$major.$minor.$patch"
    if (-not [string]::IsNullOrWhiteSpace($prerelease)) {
        $version = "$version-$prerelease"
    }

    if ($mode -eq 'Package' -and $minor -eq 0 -and $patch -eq 0) {
        throw "Package-specific major releases are not allowed. Use a full-train tag."
    }

    return [PSCustomObject]@{
        Tag = $Tag
        Mode = $mode
        Slug = $slug
        Version = $version
        Major = $major
        Minor = $minor
        Patch = $patch
        Prerelease = $prerelease
        IsPrerelease = -not [string]::IsNullOrWhiteSpace($prerelease)
        Packages = @($selectedPackages)
    }
}

function Test-ReleaseVersionProperties {
    param(
        [Parameter(Mandatory)]
        [pscustomobject] $Release
    )

    foreach ($package in $Release.Packages) {
        $declaredVersion = Get-MsBuildProperty -ProjectPath $package.FullProjectPath -PropertyName $package.VersionProperty
        $evaluatedVersion = Get-MsBuildProperty -ProjectPath $package.FullProjectPath -PropertyName 'Version'

        if ($declaredVersion -ne $Release.Version) {
            throw "Version property for package '$($package.PackageId)' does not match the release tag."
        }

        if ($evaluatedVersion -ne $Release.Version) {
            throw "Evaluated package version for package '$($package.PackageId)' does not match the release tag."
        }
    }
}

function Invoke-ReleaseCommand {
    param(
        [Parameter(Mandatory)]
        [string] $FilePath,

        [Parameter(Mandatory)]
        [string[]] $Arguments,

        [string] $WorkingDirectory = $script:RepositoryRoot
    )

    Push-Location -LiteralPath $WorkingDirectory
    try {
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed: $FilePath"
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-ReleaseCommandWithOutput {
    param(
        [Parameter(Mandatory)]
        [string] $FilePath,

        [Parameter(Mandatory)]
        [string[]] $Arguments,

        [string] $WorkingDirectory = $script:RepositoryRoot
    )

    Push-Location -LiteralPath $WorkingDirectory
    try {
        $output = & $FilePath @Arguments 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed: $FilePath. $($output -join [Environment]::NewLine)"
        }

        return $output
    }
    finally {
        Pop-Location
    }
}

function New-ReleaseSummary {
    param(
        [Parameter(Mandatory)]
        [pscustomobject] $Release
    )

    return [PSCustomObject]@{
        tag = $Release.Tag
        mode = $Release.Mode
        slug = $Release.Slug
        version = $Release.Version
        isPrerelease = $Release.IsPrerelease
        packages = @($Release.Packages | ForEach-Object { $_.PackageId })
    }
}
