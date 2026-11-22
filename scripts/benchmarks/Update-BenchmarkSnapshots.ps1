[CmdletBinding()]
param([switch] $Apply)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$artifactRoot = Join-Path $repoRoot '.artifacts/benchmark-snapshots'
$refreshDirectory = Join-Path $artifactRoot ([DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 7))
$preview = Join-Path $refreshDirectory 'generated'
$target = Join-Path $repoRoot 'src/dotnet/generated-code-samples/benchmarks'

function Get-SnapshotFiles([string] $Directory) {
    $manifest = Join-Path $Directory 'manifest.json'
    if (!(Test-Path -LiteralPath $manifest -PathType Leaf)) { return }
    $entries = (Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json).cases
    foreach ($file in ($entries.file | Sort-Object -Unique)) {
        if ($file -cnotmatch '\A[A-Za-z0-9_-]+\.cs\z') {
            throw "Unexpected snapshot filename '$file' in $manifest."
        }
        $file
    }
    'manifest.json'
}

function Assert-RegularPath([string] $Path) {
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue
    if ($null -ne $item -and $item.Attributes.HasFlag([IO.FileAttributes]::ReparsePoint)) {
        throw "Snapshot regeneration requires a regular path: $Path"
    }
}

function Copy-SnapshotFile([string] $From, [string] $To) {
    $temporary = $To + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
    try {
        [IO.File]::Copy($From, $temporary)
        [IO.File]::Move($temporary, $To, $true)
    }
    finally {
        [IO.File]::Delete($temporary)
    }
}

function Set-SnapshotCollection {
    Assert-RegularPath $target
    $previousFiles = @(Get-SnapshotFiles $target)
    $nextFiles = @(Get-SnapshotFiles $preview)
    if ($nextFiles -notcontains 'manifest.json') { throw 'The regenerated snapshot manifest is missing.' }
    $orphaned = @(Get-ChildItem -LiteralPath $target -Filter '*.cs' -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.DirectoryName -ne $target -or $_.Name -cnotin $previousFiles })
    if ($orphaned.Length -ne 0) {
        throw "Review unexpected existing files before applying the snapshot collection: $($orphaned.FullName -join ', ')"
    }
    $backup = Join-Path $refreshDirectory 'previous'
    New-Item -ItemType Directory -Path $target, $backup -Force | Out-Null
    $changed = @($nextFiles | Where-Object {
        $destination = Join-Path $target $_
        Assert-RegularPath $destination
        !(Test-Path -LiteralPath $destination -PathType Leaf) -or
            (Get-FileHash -LiteralPath $destination).Hash -ne (Get-FileHash -LiteralPath (Join-Path $preview $_)).Hash
    })
    $retired = @($previousFiles | Where-Object { $_ -cnotin $nextFiles })
    $affected = @($changed + $retired)
    foreach ($file in $affected) {
        $source = Join-Path $target $file
        Assert-RegularPath $source
        if (Test-Path -LiteralPath $source -PathType Leaf) {
            [IO.File]::Copy($source, (Join-Path $backup $file))
        }
    }
    try {
        foreach ($file in $changed) {
            Copy-SnapshotFile (Join-Path $preview $file) (Join-Path $target $file)
        }
        foreach ($file in $retired) {
            [IO.File]::Delete((Join-Path $target $file))
        }
    }
    catch {
        foreach ($file in $affected) {
            $savedFile = Join-Path $backup $file
            $destination = Join-Path $target $file
            if (Test-Path -LiteralPath $savedFile -PathType Leaf) {
                Copy-SnapshotFile $savedFile $destination
            }
            else {
                [IO.File]::Delete($destination)
            }
        }
        throw
    }
    Write-Output "Applied $($changed.Count) updated and $($retired.Count) retired files. Previous files: $backup"
}

Push-Location $repoRoot
try {
    New-Item -ItemType Directory -Path $refreshDirectory -Force | Out-Null
    $project = Join-Path $repoRoot 'src/dotnet/Musoq.Evaluator.CodeGeneration.Tests'
    $test = 'Musoq.Evaluator.Tests.BenchmarkSnapshotTests.ActiveCases_WhenRegenerated_ShouldMatchTrackedBenchmarkSnapshots'
    & dotnet test $project -c Release --nologo --verbosity quiet --logger 'console;verbosity=minimal' `
        --filter "FullyQualifiedName=$test" --results-directory (Join-Path $refreshDirectory 'test') `
        --environment "MUSOQ_BENCHMARK_SNAPSHOT_OUTPUT=$preview"
    if ($LASTEXITCODE -ne 0) { throw "Benchmark snapshot regeneration failed ($LASTEXITCODE)." }
    if ($Apply) {
        $lock = [IO.File]::Open((Join-Path $artifactRoot 'apply.lock'), 'OpenOrCreate', 'Write', 'None')
        try {
            Set-SnapshotCollection
        }
        finally {
            $lock.Dispose()
        }
    }
    Write-Output "Generated snapshots: $preview"
}
finally {
    Pop-Location
}
