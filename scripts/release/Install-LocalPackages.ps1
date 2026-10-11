<#
.SYNOPSIS
Installs locally packed NuGet packages into a reusable user-local feed and cache.

.DESCRIPTION
Copies each package from the supplied .nupkg files or directories into a local
NuGet feed, registers the feed for the current user, and restores the exact
package versions into the global packages folder. The cache restore uses a
temporary NuGet.Config containing only that local feed and disables NuGet audit.
Missing packages therefore fail the restore without contacting remote sources.
Existing package identities with different bytes are rejected unless
-ReplaceExisting is specified. Replacement backs up the feed files and exact
global-cache entries until the offline restore succeeds, and rolls them back if
the restore fails.

.EXAMPLE
./scripts/release/Install-LocalPackages.ps1 `
    -PackagePath ./artifacts/release-candidates/17.0.11-alpha.2/combined-feed

.EXAMPLE
./scripts/release/Install-LocalPackages.ps1 `
    -PackagePath ./artifacts/nupkgs/Musoq.Evaluator.17.0.11-alpha.2.nupkg

.EXAMPLE
./scripts/release/Install-LocalPackages.ps1 `
    -PackagePath ./artifacts/release-candidates/17.0.11-alpha.2/all-packages `
    -ReplaceExisting
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [string[]] $PackagePath,

    [string] $FeedPath,

    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9_.-]*$')]
    [string] $SourceName = 'musoq-local',

    [switch] $ReplaceExisting
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Release.Common.ps1')

function Get-PackageIdentityFromArchive {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    Add-Type -AssemblyName System.IO.Compression
    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $nuspecEntries = @($archive.Entries | Where-Object { $_.FullName -match '(?i)(^|/)[^/]+\.nuspec$' })
        if ($nuspecEntries.Count -ne 1) {
            throw "Expected one .nuspec file in package '$Path'."
        }

        $reader = [System.IO.StreamReader]::new($nuspecEntries[0].Open())
        try {
            [xml] $nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $idNode = $nuspec.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='id']")
        $versionNode = $nuspec.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='version']")
        if ($null -eq $idNode -or $null -eq $versionNode) {
            throw "Package '$Path' does not declare both an id and version in its .nuspec."
        }

        $packageId = $idNode.InnerText.Trim()
        $version = $versionNode.InnerText.Trim()
        if ($packageId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') {
            throw "Package '$Path' has an invalid NuGet package id."
        }

        if ($version -notmatch '^[0-9][A-Za-z0-9.+-]*$') {
            throw "Package '$Path' has an invalid NuGet package version."
        }

        return [PSCustomObject]@{
            PackageId = $packageId
            Version = $version
            Path = $Path
            Sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Get-RegisteredNuGetSources {
    $output = @(& dotnet nuget list source 2>&1 | ForEach-Object { [string] $_ })
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to inspect registered NuGet sources. $($output -join [Environment]::NewLine)"
    }

    $sources = [System.Collections.Generic.List[object]]::new()
    for ($index = 0; $index -lt $output.Count; $index++) {
        if ($output[$index] -notmatch '^\s*\d+\.\s+(?<Name>.+?)\s+\[(?<State>Enabled|Disabled)\]\s*$') {
            continue
        }

        $sourceName = $Matches.Name
        $sourceState = $Matches.State
        if ($index + 1 -ge $output.Count -or [string]::IsNullOrWhiteSpace($output[$index + 1])) {
            throw "NuGet source '$sourceName' has no configured location."
        }

        $sources.Add([PSCustomObject]@{
            Name = $sourceName
            State = $sourceState
            Location = $output[$index + 1].Trim()
        })
        $index++
    }

    return @($sources.ToArray())
}

function ConvertTo-NuGetSourceLocationKey {
    param(
        [Parameter(Mandatory)]
        [string] $Location
    )

    if ($Location -match '^[A-Za-z][A-Za-z0-9+.-]*://') {
        return $Location.TrimEnd('/')
    }

    return [System.IO.Path]::GetFullPath($Location)
}

function Ensure-RegisteredNuGetSource {
    param(
        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [string] $Location
    )

    $matchingSources = @(Get-RegisteredNuGetSources | Where-Object { $_.Name -ieq $Name })
    if ($matchingSources.Count -gt 1) {
        throw "More than one NuGet source is registered as '$Name'."
    }

    if ($matchingSources.Count -eq 1) {
        $existingLocation = ConvertTo-NuGetSourceLocationKey -Location $matchingSources[0].Location
        $requestedLocation = ConvertTo-NuGetSourceLocationKey -Location $Location
        if (-not [string]::Equals($existingLocation, $requestedLocation, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "NuGet source '$Name' already points to '$($matchingSources[0].Location)', not '$Location'."
        }

        if ($matchingSources[0].State -eq 'Disabled') {
            & dotnet nuget enable source $Name
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to enable NuGet source '$Name'."
            }
        }

        return
    }

    & dotnet nuget add source $Location --name $Name
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to register local NuGet source '$Name'."
    }
}

function Copy-PackageWithoutOverwrite {
    param(
        [Parameter(Mandatory)]
        [string] $SourcePath,

        [Parameter(Mandatory)]
        [string] $DestinationPath,

        [switch] $ReplaceExisting
    )

    $sourceHash = (Get-FileHash -LiteralPath $SourcePath -Algorithm SHA256).Hash
    if (Test-Path -LiteralPath $DestinationPath -PathType Leaf) {
        $destinationHash = (Get-FileHash -LiteralPath $DestinationPath -Algorithm SHA256).Hash
        if ($sourceHash -ne $destinationHash) {
            if (-not $ReplaceExisting) {
                throw "Refusing to replace a different package already in the local feed: '$DestinationPath'. Pass -ReplaceExisting only for an unpublished local rebuild."
            }

            Copy-Item -LiteralPath $SourcePath -Destination $DestinationPath -Force
        }
        else {
            return
        }
    }
    else {
        Copy-Item -LiteralPath $SourcePath -Destination $DestinationPath
    }

    $copiedHash = (Get-FileHash -LiteralPath $DestinationPath -Algorithm SHA256).Hash
    if ($sourceHash -ne $copiedHash) {
        throw "The local-feed copy did not match its source package: '$DestinationPath'."
    }
}

function Get-GlobalCachedPackagePath {
    param(
        [Parameter(Mandatory)]
        [string] $GlobalPackagesFolder,

        [Parameter(Mandatory)]
        [pscustomobject] $Package
    )

    $globalRoot = [System.IO.Path]::GetFullPath($GlobalPackagesFolder)
    $globalRootLength = [System.IO.Path]::GetPathRoot($globalRoot).Length
    if ($globalRoot.Length -gt $globalRootLength) {
        $globalRoot = $globalRoot.TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar)
    }

    $idDirectory = [System.IO.Path]::GetFullPath((Join-Path $globalRoot $Package.PackageId.ToLowerInvariant()))
    $versionDirectory = [System.IO.Path]::GetFullPath((Join-Path $idDirectory $Package.Version.ToLowerInvariant()))
    $idDirectory = $idDirectory.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $versionDirectory = $versionDirectory.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    if ([System.IO.Path]::GetDirectoryName($idDirectory) -ine $globalRoot -or
        [System.IO.Path]::GetDirectoryName($versionDirectory) -ine $idDirectory -or
        [System.IO.Path]::GetFileName($versionDirectory) -ine $Package.Version.ToLowerInvariant()) {
        throw "The resolved global package cache path for '$($Package.PackageId)' is outside its expected package/version folder."
    }

    return $versionDirectory
}

if ([string]::IsNullOrWhiteSpace($FeedPath)) {
    $userProfile = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile)
    $FeedPath = Join-Path $userProfile '.nuget\local-feed\musoq'
}

$resolvedFeedPath = [System.IO.Path]::GetFullPath($FeedPath)
$candidatePaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($inputPath in $PackagePath) {
    $resolvedInput = Resolve-Path -LiteralPath $inputPath -ErrorAction Stop
    if (Test-Path -LiteralPath $resolvedInput.Path -PathType Container) {
        foreach ($packageFile in Get-ChildItem -LiteralPath $resolvedInput.Path -File | Where-Object { $_.Extension -ieq '.nupkg' }) {
            [void] $candidatePaths.Add([System.IO.Path]::GetFullPath($packageFile.FullName))
        }
        continue
    }

    if ([System.IO.Path]::GetExtension($resolvedInput.Path) -ieq '.snupkg') {
        Write-Verbose "Ignoring symbol package input '$($resolvedInput.Path)'; its .nupkg should be supplied instead."
        continue
    }

    if ([System.IO.Path]::GetExtension($resolvedInput.Path) -ine '.nupkg') {
        throw "Input '$inputPath' is not a .nupkg file or package directory."
    }

    [void] $candidatePaths.Add([System.IO.Path]::GetFullPath($resolvedInput.Path))
}

if ($candidatePaths.Count -eq 0) {
    throw 'No .nupkg packages were found in the supplied paths.'
}

$packagesByIdentity = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($candidatePath in $candidatePaths) {
    $package = Get-PackageIdentityFromArchive -Path $candidatePath
    $identityKey = "$($package.PackageId)/$($package.Version)"
    if ($packagesByIdentity.ContainsKey($identityKey)) {
        $existingPackage = $packagesByIdentity[$identityKey]
        if ($existingPackage.Sha256 -ne $package.Sha256) {
            throw "The inputs contain different .nupkg files for '$($package.PackageId)' version '$($package.Version)'."
        }
        continue
    }

    $packagesByIdentity.Add($identityKey, $package)
}

$packages = @($packagesByIdentity.Values | Sort-Object PackageId, Version)
$copyPlan = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($package in $packages) {
    $packageFileName = "$($package.PackageId).$($package.Version).nupkg"
    $packageDestination = Join-Path $resolvedFeedPath $packageFileName
    $copyPlan[$packageDestination] = [PSCustomObject]@{
        SourcePath = $package.Path
        DestinationPath = $packageDestination
    }

    $symbolSourcePath = [System.IO.Path]::ChangeExtension($package.Path, '.snupkg')
    if (Test-Path -LiteralPath $symbolSourcePath -PathType Leaf) {
        $symbolDestination = Join-Path $resolvedFeedPath "$($package.PackageId).$($package.Version).snupkg"
        $copyPlan[$symbolDestination] = [PSCustomObject]@{
            SourcePath = $symbolSourcePath
            DestinationPath = $symbolDestination
        }
    }
}

$globalPackagesFolder = Get-NuGetGlobalPackagesFolder
$tempDirectoryName = 'musoq-local-nuget-cache-' + [guid]::NewGuid().ToString('N')
$tempDirectory = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetTempPath()) $tempDirectoryName))
$tempParent = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar)
$actualTempParent = [System.IO.Path]::GetDirectoryName($tempDirectory).TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar)
if ($actualTempParent -ine $tempParent -or $tempDirectoryName -notmatch '^musoq-local-nuget-cache-[0-9a-f]{32}$') {
    throw 'Could not safely resolve the temporary restore directory.'
}

New-Item -ItemType Directory -Path $tempDirectory | Out-Null
$feedState = [System.Collections.Generic.List[object]]::new()
$cacheState = [System.Collections.Generic.List[object]]::new()
try {
    New-Item -ItemType Directory -Path $resolvedFeedPath -Force | Out-Null
    Ensure-RegisteredNuGetSource -Name $SourceName -Location $resolvedFeedPath

    foreach ($copy in $copyPlan.Values) {
        $destinationExisted = Test-Path -LiteralPath $copy.DestinationPath -PathType Leaf
        $backupPath = $null
        if ($destinationExisted) {
            $sourceHash = (Get-FileHash -LiteralPath $copy.SourcePath -Algorithm SHA256).Hash
            $destinationHash = (Get-FileHash -LiteralPath $copy.DestinationPath -Algorithm SHA256).Hash
            if ($sourceHash -ne $destinationHash) {
                if (-not $ReplaceExisting) {
                    throw "Refusing to replace a different package already in the local feed: '$($copy.DestinationPath)'. Pass -ReplaceExisting only for an unpublished local rebuild."
                }

                $backupPath = Join-Path (Join-Path $tempDirectory 'feed-backup') ([System.IO.Path]::GetFileName($copy.DestinationPath))
                New-Item -ItemType Directory -Path (Split-Path -Parent $backupPath) -Force | Out-Null
                Copy-Item -LiteralPath $copy.DestinationPath -Destination $backupPath
            }
        }

        $feedState.Add([PSCustomObject]@{
            DestinationPath = $copy.DestinationPath
            ExistedBefore = $destinationExisted
            BackupPath = $backupPath
        })
        Copy-PackageWithoutOverwrite `
            -SourcePath $copy.SourcePath `
            -DestinationPath $copy.DestinationPath `
            -ReplaceExisting:$ReplaceExisting
    }

    if ($ReplaceExisting) {
        Write-Host 'Refreshing the selected package identities in the global cache.'
        foreach ($package in $packages) {
            $cachedPackagePath = Get-GlobalCachedPackagePath `
                -GlobalPackagesFolder $globalPackagesFolder `
                -Package $package
            $backupPath = Join-Path (Join-Path $tempDirectory 'cache-backup' $package.PackageId.ToLowerInvariant()) $package.Version.ToLowerInvariant()
            $cacheExisted = Test-Path -LiteralPath $cachedPackagePath -PathType Container
            if ($cacheExisted) {
                $cacheItem = Get-Item -LiteralPath $cachedPackagePath
                if (($cacheItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                    throw "Refusing to refresh a reparse-point package cache entry: '$cachedPackagePath'."
                }

                New-Item -ItemType Directory -Path (Split-Path -Parent $backupPath) -Force | Out-Null
                Copy-Item -LiteralPath $cachedPackagePath -Destination $backupPath -Recurse
            }

            $cacheState.Add([PSCustomObject]@{
                CachePath = $cachedPackagePath
                ExistedBefore = $cacheExisted
                BackupPath = if ($cacheExisted) { $backupPath } else { $null }
            })
            if ($cacheExisted) {
                Remove-Item -LiteralPath $cachedPackagePath -Recurse -Force
            }
        }
    }

    $projectReferences = @($packages | ForEach-Object {
        $escapedId = [System.Security.SecurityElement]::Escape($_.PackageId)
        $escapedVersion = [System.Security.SecurityElement]::Escape("[$($_.Version)]")
        "    <PackageReference Include=`"$escapedId`" Version=`"$escapedVersion`" />"
    }) -join [Environment]::NewLine
    $projectPath = Join-Path $tempDirectory 'LocalPackageCacheInstall.csproj'
    $projectContents = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
$projectReferences
  </ItemGroup>
</Project>
"@
    Set-Content -LiteralPath $projectPath -Value $projectContents -Encoding UTF8

    $escapedFeedPath = [System.Security.SecurityElement]::Escape($resolvedFeedPath)
    $nugetConfigPath = Join-Path $tempDirectory 'NuGet.Config'
    $nugetConfigContents = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="$SourceName" value="$escapedFeedPath" />
  </packageSources>
</configuration>
"@
    Set-Content -LiteralPath $nugetConfigPath -Value $nugetConfigContents -Encoding UTF8

    Write-Host "Restoring exact package versions using only local feed '$resolvedFeedPath'."
    $restoreArguments = @(
        'restore',
        $projectPath,
        '--configfile',
        $nugetConfigPath,
        '--force',
        '--nologo',
        '--verbosity',
        'quiet',
        '-p:NuGetAudit=false',
        "-p:RestorePackagesPath=$globalPackagesFolder"
    )
    & dotnet @restoreArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Offline restore failed with exit code $LASTEXITCODE. Missing dependencies were not fetched from remote sources."
    }

    foreach ($package in $packages) {
        $cachedPackagePath = Get-GlobalCachedPackagePath `
            -GlobalPackagesFolder $globalPackagesFolder `
            -Package $package
        if (-not (Test-Path -LiteralPath $cachedPackagePath -PathType Container)) {
            throw "Package '$($package.PackageId)' version '$($package.Version)' was not found in the global package cache after restore."
        }
    }
}
catch {
    foreach ($state in $cacheState) {
        try {
            if (Test-Path -LiteralPath $state.CachePath -PathType Container) {
                Remove-Item -LiteralPath $state.CachePath -Recurse -Force
            }

            if ($null -ne $state.BackupPath -and (Test-Path -LiteralPath $state.BackupPath -PathType Container)) {
                New-Item -ItemType Directory -Path (Split-Path -Parent $state.CachePath) -Force | Out-Null
                Copy-Item -LiteralPath $state.BackupPath -Destination $state.CachePath -Recurse
            }
        }
        catch {
            Write-Warning "Could not roll back cache entry '$($state.CachePath)': $($_.Exception.Message)"
        }
    }

    foreach ($state in $feedState) {
        try {
            if ($null -ne $state.BackupPath -and (Test-Path -LiteralPath $state.BackupPath -PathType Leaf)) {
                Copy-Item -LiteralPath $state.BackupPath -Destination $state.DestinationPath -Force
            }
            elseif (-not $state.ExistedBefore -and (Test-Path -LiteralPath $state.DestinationPath -PathType Leaf)) {
                Remove-Item -LiteralPath $state.DestinationPath -Force
            }
        }
        catch {
            Write-Warning "Could not roll back feed file '$($state.DestinationPath)': $($_.Exception.Message)"
        }
    }

    throw
}
finally {
    if (Test-Path -LiteralPath $tempDirectory -PathType Container) {
        try {
            Remove-Item -LiteralPath $tempDirectory -Recurse -Force
        }
        catch {
            Write-Warning "Could not remove temporary restore directory '$tempDirectory': $($_.Exception.Message)"
        }
    }
}

Write-Host "Installed $($packages.Count) package(s) into the local NuGet feed and global package cache."
Write-Host "Feed: $resolvedFeedPath"
Write-Host "Registered source: $SourceName"
Write-Host "Global package cache: $globalPackagesFolder"
foreach ($package in $packages) {
    Write-Host "  $($package.PackageId) $($package.Version)"
}
