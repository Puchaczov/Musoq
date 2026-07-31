[CmdletBinding()]
param(
    [string] $OutputPath = 'TestResults/evaluator-measurement/compile-site-inventory.json'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$testRoot = Join-Path $repositoryRoot 'src/dotnet/Musoq.Evaluator.Tests'
$patterns = @(
    'CompileForExecution\s*\(',
    'CompileForTyped',
    'TestMethodTemplate\s*(?:<[^>]+>)?\s*\(',
    'ReadSample\s*\(',
    'ReadAllSamples\s*\(',
    'GeneratedCodeSampleArtifacts\.Generate\s*\(',
    'Guid\.NewGuid\s*\(\)'
)

$inventory = foreach ($file in Get-ChildItem -LiteralPath $testRoot -Recurse -Filter '*.cs' -File) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $matches = foreach ($pattern in $patterns) {
        [pscustomobject]@{
            Pattern = $pattern
            Count = [regex]::Matches($text, $pattern).Count
        }
    }
    $matches = @($matches | Where-Object Count -gt 0)
    if ($matches.Count -gt 0) {
        [pscustomobject]@{
            File = $file.FullName.Substring($repositoryRoot.Length + 1)
            Matches = $matches
        }
    }
}

$summary = [ordered]@{
    RecordedUtc = [DateTimeOffset]::UtcNow
    Root = $testRoot
    Files = @($inventory)
    Totals = @($patterns | ForEach-Object {
        $pattern = $_
        [pscustomobject]@{
            Pattern = $pattern
            Count = (@($inventory | ForEach-Object { $_.Matches | Where-Object Pattern -eq $pattern } | Measure-Object -Property Count -Sum).Sum)
        }
    })
}

$resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $repositoryRoot $OutputPath }
New-Item -ItemType Directory -Path (Split-Path -Parent $resolvedOutput) -Force | Out-Null
$summary | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $resolvedOutput -Encoding utf8
$summary.Totals | Format-Table -AutoSize | Out-String | Write-Output
Write-Output "Inventory JSON: $resolvedOutput"
