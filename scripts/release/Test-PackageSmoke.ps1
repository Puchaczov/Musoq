[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Tag,

    [Parameter(Mandatory)]
    [string] $PackageDirectory
)

. (Join-Path $PSScriptRoot 'Release.Common.ps1')

$release = Resolve-ReleaseTag -Tag $Tag
Test-ReleaseVersionProperties -Release $release

$resolvedPackageDirectory = [System.IO.Path]::GetFullPath($PackageDirectory)
if (-not (Test-Path -LiteralPath $resolvedPackageDirectory)) {
    throw "Package directory was not found."
}

function Get-PackageArchiveEntries {
    param(
        [Parameter(Mandatory)]
        [string] $PackagePath
    )

    Add-Type -AssemblyName System.IO.Compression
    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        return @($archive.Entries | ForEach-Object { $_.FullName })
    }
    finally {
        $archive.Dispose()
    }
}

function Get-PackageArchiveTextEntry {
    param(
        [Parameter(Mandatory)]
        [string] $PackagePath,

        [Parameter(Mandatory)]
        [string] $EntryPath
    )

    Add-Type -AssemblyName System.IO.Compression
    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $entry = $archive.GetEntry($EntryPath)
        if ($null -eq $entry) {
            throw "Package entry '$EntryPath' was not found in '$PackagePath'."
        }

        $reader = [System.IO.StreamReader]::new($entry.Open())
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-EqualPackageEntries {
    param(
        [Parameter(Mandatory)]
        [string[]] $Expected,

        [Parameter(Mandatory)]
        [string[]] $Actual,

        [Parameter(Mandatory)]
        [string] $Description
    )

    $difference = Compare-Object -ReferenceObject @($Expected | Sort-Object) -DifferenceObject @($Actual | Sort-Object)
    if ($null -ne $difference) {
        $formattedDifference = ($difference | ForEach-Object { "$($_.SideIndicator) $($_.InputObject)" }) -join [Environment]::NewLine
        throw "$Description did not match the expected package contents.$([Environment]::NewLine)$formattedDifference"
    }
}

function Test-ConverterPackageContents {
    param(
        [Parameter(Mandatory)]
        [pscustomobject] $Release,

        [Parameter(Mandatory)]
        [string] $PackageDirectory
    )

    $converterPackage = @($Release.Packages | Where-Object { $_.PackageId -eq 'Musoq.Converter' })
    if ($converterPackage.Count -eq 0) {
        return
    }

    $nupkgPath = Join-Path $PackageDirectory "Musoq.Converter.$($Release.Version).nupkg"
    $snupkgPath = Join-Path $PackageDirectory "Musoq.Converter.$($Release.Version).snupkg"
    if (-not (Test-Path -LiteralPath $nupkgPath) -or -not (Test-Path -LiteralPath $snupkgPath)) {
        throw 'Converter package or symbol package was not found.'
    }

    $expectedTargetAssemblies = @(
        'Musoq.Targets.Abstractions',
        'Musoq.Targets.Execution',
        'Musoq.Targets.Execution.Analysis',
        'Musoq.Targets.CSharpClr'
    )
    $expectedNupkgDlls = @('lib/net10.0/Musoq.Converter.dll') + @($expectedTargetAssemblies | ForEach-Object { "lib/net10.0/$_.dll" })
    $expectedTargetDocumentation = @($expectedTargetAssemblies | ForEach-Object { "lib/net10.0/$_.xml" })
    $nupkgEntries = Get-PackageArchiveEntries -PackagePath $nupkgPath
    $actualNupkgDlls = @($nupkgEntries | Where-Object { $_ -match '^lib/net10\.0/[^/]+\.dll$' })
    Assert-EqualPackageEntries -Expected $expectedNupkgDlls -Actual $actualNupkgDlls -Description 'Converter lib/net10.0 assemblies'

    foreach ($documentationPath in $expectedTargetDocumentation) {
        if ($nupkgEntries -notcontains $documentationPath) {
            throw "Bundled target documentation '$documentationPath' was not found."
        }
    }

    if (-not ($nupkgEntries -contains 'README.md')) {
        throw 'Converter package README was not found.'
    }

    if (@($nupkgEntries | Where-Object { $_ -match 'Musoq\.Targets\.TestPortable' }).Count -gt 0) {
        throw 'Test-only portable target files must not be included in the converter package.'
    }

    $nuspec = Get-PackageArchiveTextEntry -PackagePath $nupkgPath -EntryPath 'Musoq.Converter.nuspec'
    if ($nuspec -match '<dependency id="Musoq\.Targets\.') {
        throw 'Converter package must bundle internal target assemblies instead of declaring Musoq.Targets package dependencies.'
    }

    if ($nuspec -notmatch '<repository [^>]*commit="[0-9a-f]{40}"') {
        throw 'Converter package nuspec must contain the source repository commit.'
    }

    $expectedSnupkgPdbs = @('lib/net10.0/Musoq.Converter.pdb') + @($expectedTargetAssemblies | ForEach-Object { "lib/net10.0/$_.pdb" })
    $snupkgEntries = Get-PackageArchiveEntries -PackagePath $snupkgPath
    $actualSnupkgPdbs = @($snupkgEntries | Where-Object { $_ -match '^lib/net10\.0/[^/]+\.pdb$' })
    Assert-EqualPackageEntries -Expected $expectedSnupkgPdbs -Actual $actualSnupkgPdbs -Description 'Converter symbol assemblies'
}

function Test-EvaluatorPackageContents {
    param(
        [Parameter(Mandatory)]
        [pscustomobject] $Release,

        [Parameter(Mandatory)]
        [string] $PackageDirectory
    )

    $evaluatorPackage = @($Release.Packages | Where-Object { $_.PackageId -eq 'Musoq.Evaluator' })
    if ($evaluatorPackage.Count -eq 0) {
        return
    }

    $nupkgPath = Join-Path $PackageDirectory "Musoq.Evaluator.$($Release.Version).nupkg"
    $snupkgPath = Join-Path $PackageDirectory "Musoq.Evaluator.$($Release.Version).snupkg"
    if (-not (Test-Path -LiteralPath $nupkgPath) -or -not (Test-Path -LiteralPath $snupkgPath)) {
        throw 'Evaluator package or symbol package was not found.'
    }

    $nupkgEntries = Get-PackageArchiveEntries -PackagePath $nupkgPath
    Assert-EqualPackageEntries -Expected @(
        'lib/net10.0/Musoq.Evaluator.dll',
        'lib/net10.0/Musoq.Targets.Abstractions.dll'
    ) -Actual @($nupkgEntries | Where-Object { $_ -match '^lib/net10\.0/[^/]+\.dll$' }) -Description 'Evaluator lib/net10.0 assemblies'

    if ($nupkgEntries -notcontains 'lib/net10.0/Musoq.Targets.Abstractions.xml') {
        throw 'Bundled target abstractions documentation was not found in the evaluator package.'
    }

    if (@($nupkgEntries | Where-Object { $_ -match 'Musoq\.Targets\.TestPortable' }).Count -gt 0) {
        throw 'Test-only portable target files must not be included in the evaluator package.'
    }

    $nuspec = Get-PackageArchiveTextEntry -PackagePath $nupkgPath -EntryPath 'Musoq.Evaluator.nuspec'
    if ($nuspec -match '<dependency id="Musoq\.Targets\.') {
        throw 'Evaluator package must bundle target abstractions instead of declaring a Musoq.Targets package dependency.'
    }

    $snupkgEntries = Get-PackageArchiveEntries -PackagePath $snupkgPath
    Assert-EqualPackageEntries -Expected @(
        'lib/net10.0/Musoq.Evaluator.pdb',
        'lib/net10.0/Musoq.Targets.Abstractions.pdb'
    ) -Actual @($snupkgEntries | Where-Object { $_ -match '^lib/net10\.0/[^/]+\.pdb$' }) -Description 'Evaluator symbol assemblies'
}

function Get-SmokeProgram {
    param(
        [Parameter(Mandatory)]
        [pscustomobject] $Release
    )

    $usesTypedQuery = $Release.Mode -eq 'FullTrain' -or
        ($Release.Packages.Count -eq 1 -and $Release.Packages[0].SmokeTestMode -eq 'typed-query')

    if ($usesTypedQuery) {
        return @'
using System;
using System.Linq;
using System.Threading;
using MusoqApi = Musoq.Converter.Musoq;

public sealed record Person(string Name, int Age);
public sealed record NameDto(string Name);

public static class Program
{
    public static void Main()
    {
        var rows = MusoqApi
            .Query("select p.Name as Name from #A.entities() p where p.Age > 30")
            .Source("#A", "entities", new[] { new[] { new Person("Alice", 35), new Person("Bob", 20) } })
            .CompileAndRun<NameDto>(CancellationToken.None)
            .ToArray();

        if (rows.Length != 1 || rows[0].Name != "Alice")
        {
            throw new InvalidOperationException("Unexpected Musoq package smoke test result.");
        }

        Console.WriteLine("SMOKE_OK");
    }
}
'@
    }

    switch ($Release.Packages[0].Slug) {
        'parser' {
            return @'
using System;

Console.WriteLine(typeof(Musoq.Parser.Parser).Assembly.GetName().Name);
Console.WriteLine("SMOKE_OK");
'@
        }
        'plugins' {
            return @'
using System;

Console.WriteLine(typeof(Musoq.Plugins.CountAllAggregateKernel).Assembly.GetName().Name);
Console.WriteLine("SMOKE_OK");
'@
        }
        'schema' {
            return @'
using System;

Console.WriteLine(typeof(Musoq.Schema.SingleRowSchemaTable).Assembly.GetName().Name);
Console.WriteLine("SMOKE_OK");
'@
        }
        'evaluator' {
            return @'
using System;

Console.WriteLine(typeof(Musoq.Evaluator.CompilationOptions).Assembly.GetName().Name);
Console.WriteLine("SMOKE_OK");
'@
        }
        default {
            throw "Unsupported smoke test package."
        }
    }
}

function Get-PackageReferences {
    param(
        [Parameter(Mandatory)]
        [pscustomobject] $Release
    )

    if ($Release.Mode -eq 'FullTrain') {
        return @([PSCustomObject]@{ PackageId = 'Musoq.Converter'; Version = $Release.Version })
    }

    return @([PSCustomObject]@{
        PackageId = $Release.Packages[0].PackageId
        Version = $Release.Version
    })
}

$tempRoot = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetTempPath()) "musoq-release-smoke-$([Guid]::NewGuid().ToString('N'))"))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

try {
    Test-EvaluatorPackageContents -Release $release -PackageDirectory $resolvedPackageDirectory
    Test-ConverterPackageContents -Release $release -PackageDirectory $resolvedPackageDirectory

    Invoke-ReleaseCommand -FilePath 'dotnet' -Arguments @(
        'new',
        'console',
        '--framework',
        'net10.0',
        '--no-restore'
    ) -WorkingDirectory $tempRoot

    $projectPath = Get-ChildItem -LiteralPath $tempRoot -Filter '*.csproj' |
        Select-Object -First 1
    if ($null -eq $projectPath) {
        throw "Smoke test project was not created."
    }

    $packageReferences = Get-PackageReferences -Release $release
    $packageReferenceXml = ($packageReferences | ForEach-Object {
        "    <PackageReference Include=""$($_.PackageId)"" Version=""$($_.Version)"" />"
    }) -join [Environment]::NewLine

    $projectXml = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
$packageReferenceXml
  </ItemGroup>
</Project>
"@
    Set-Content -LiteralPath $projectPath.FullName -Value $projectXml -Encoding UTF8

    $escapedPackageDirectory = [System.Security.SecurityElement]::Escape($resolvedPackageDirectory)
    $escapedGlobalPackagesFolder = [System.Security.SecurityElement]::Escape((Join-Path $tempRoot 'packages'))
    $nugetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="globalPackagesFolder" value="$escapedGlobalPackagesFolder" />
  </config>
  <packageSources>
    <clear />
    <add key="local" value="$escapedPackageDirectory" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@
    Set-Content -LiteralPath (Join-Path $tempRoot 'NuGet.config') -Value $nugetConfig -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $tempRoot 'Program.cs') -Value (Get-SmokeProgram -Release $release) -Encoding UTF8

    Invoke-ReleaseCommand -FilePath 'dotnet' -Arguments @('restore', $projectPath.FullName, '--nologo', '--verbosity', 'quiet') -WorkingDirectory $tempRoot
    Invoke-ReleaseCommand -FilePath 'dotnet' -Arguments @('build', $projectPath.FullName, '--configuration', 'Release', '--no-restore', '--nologo', '--verbosity', 'quiet') -WorkingDirectory $tempRoot
    $assetsPath = Join-Path $tempRoot 'obj/project.assets.json'
    if ((Get-Content -LiteralPath $assetsPath -Raw) -match '"Musoq\.Targets\.') {
        throw 'Consumer restore must not resolve internal Musoq.Targets NuGet packages.'
    }

    $consumerOutputPath = Join-Path $tempRoot 'bin/Release/net10.0'
    foreach ($targetAssembly in @('Musoq.Targets.Abstractions.dll', 'Musoq.Targets.Execution.dll', 'Musoq.Targets.Execution.Analysis.dll', 'Musoq.Targets.CSharpClr.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $consumerOutputPath $targetAssembly))) {
            throw "Consumer output did not include bundled target assembly '$targetAssembly'."
        }
    }

    $output = Invoke-ReleaseCommandWithOutput -FilePath 'dotnet' -Arguments @('run', '--project', $projectPath.FullName, '--configuration', 'Release', '--no-build') -WorkingDirectory $tempRoot

    if (-not ($output -contains 'SMOKE_OK')) {
        throw "Package smoke test did not report success."
    }

    Write-Host "Package smoke test passed."
}
finally {
    $tempParent = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($tempRoot.StartsWith($tempParent, [System.StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
