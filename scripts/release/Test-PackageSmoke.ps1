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
