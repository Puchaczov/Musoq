[CmdletBinding()]
param(
    [ValidateSet('Run', 'List', 'Status', 'Verify', 'Latest')][string] $Action = 'Run',
    [ValidateSet('Official', 'Local')][string] $Mode = 'Local',
    [ValidateSet('Short', 'Dry', 'Default')][string] $Job = 'Short',
    [string] $Filter = '*',
    [string] $MachineProfile,
    [string] $Output,
    [string] $Resume,
    [string] $Size,
    [string] $RunDirectory,
    [string] $HistoryRoot,
    [switch] $InProcess,
    [switch] $Json
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runnerPath = Join-Path $repoRoot 'src/dotnet/Musoq.Benchmarks/bin/Release/net10.0/Musoq.Benchmarks.dll'
$inspection = $Action -in @('Status', 'Verify', 'Latest')

if ($inspection -and !(Test-Path -LiteralPath $runnerPath)) {
    [Console]::Error.WriteLine('Build prerequisite: dotnet build src/dotnet/Musoq.Benchmarks -c Release --nologo --verbosity quiet')
    exit 5
}

if (!$inspection) {
    $effectiveMode = $Mode
    if ($Resume) {
        $definitionPath = Join-Path $Resume 'definition.json'
        if (!(Test-Path -LiteralPath $definitionPath)) {
            [Console]::Error.WriteLine('Resume requires an intact schema-v3 definition.json; unsupported formats cannot be resumed.')
            exit 4
        }
        $definition = Get-Content -LiteralPath $definitionPath -Raw | ConvertFrom-Json
        $effectiveMode = $definition.invocation.mode
    }
    $projectName = 'Musoq.Benchmarks'
    if ($effectiveMode -eq 'Official') { $projectName = 'Musoq.Benchmarks.Tests' }
    $projectPath = Join-Path $repoRoot "src/dotnet/$projectName/$projectName.csproj"
    $buildDirectory = Join-Path $repoRoot 'BenchmarkDotNet.Artifacts'
    New-Item -ItemType Directory -Force $buildDirectory | Out-Null
    $buildLog = Join-Path $buildDirectory 'entrypoint-build.log'
    & dotnet build $projectPath -c Release --nologo --verbosity quiet *> $buildLog
    if ($LASTEXITCODE -ne 0) {
        [Console]::Error.WriteLine((Get-Content -LiteralPath $buildLog -Raw))
        exit 1
    }
}

$runnerArguments = @($runnerPath, '--action', $Action)
$values = @{
    Mode = '--mode'; Job = '--job'; Filter = '--filter'; MachineProfile = '--machine-profile'
    Output = '--output'; Resume = '--resume'; Size = '--size'
    RunDirectory = '--run-directory'; HistoryRoot = '--history-root'
}
foreach ($name in $values.Keys | Sort-Object) {
    if ($PSBoundParameters.ContainsKey($name)) {
        $runnerArguments += @($values[$name], [string]$PSBoundParameters[$name])
    }
}
if ($InProcess) { $runnerArguments += '--in-process' }
if ($Json) { $runnerArguments += '--json' }

Push-Location $repoRoot
try {
    & dotnet @runnerArguments
    $resultCode = $LASTEXITCODE
}
finally { Pop-Location }
exit $resultCode
