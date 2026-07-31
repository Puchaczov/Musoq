[CmdletBinding()]
param(
    [int] $RunCount = 3,

    [string] $ResultsRoot = 'TestResults/evaluator-measurement',

    [string] $ProjectPath = 'src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj',

    [string] $Configuration = 'Release',

    [switch] $NoBuild,

    [string] $Filter,

    [string] $RunSettings,

    [switch] $Trace,

    [int] $SampleMilliseconds = 1000
)

$ErrorActionPreference = 'Stop'

if ($RunCount -lt 1) {
    throw 'RunCount must be at least 1.'
}

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$root = Join-Path $repositoryRoot $ResultsRoot
New-Item -ItemType Directory -Path $root -Force | Out-Null

function Add-JsonLine {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [object] $Value
    )

    ($Value | ConvertTo-Json -Compress -Depth 8) | Add-Content -LiteralPath $Path -Encoding utf8
}

function Get-ProcessSnapshot {
    param(
        [Parameter(Mandatory = $true)]
        [int] $RootPid
    )

    $processRows = @(Get-CimInstance Win32_Process | Select-Object ProcessId, ParentProcessId, Name, CommandLine)
    $children = @{}
    foreach ($row in $processRows) {
        $parent = [int]$row.ParentProcessId
        if (-not $children.ContainsKey($parent)) {
            $children[$parent] = [System.Collections.Generic.List[int]]::new()
        }
        $children[$parent].Add([int]$row.ProcessId)
    }

    $processIds = [System.Collections.Generic.List[int]]::new()
    $pending = [System.Collections.Generic.Queue[int]]::new()
    $pending.Enqueue($RootPid)
    while ($pending.Count -gt 0) {
        $processId = $pending.Dequeue()
        if ($processIds.Contains($processId)) { continue }
        $processIds.Add($processId)
        if ($children.ContainsKey($processId)) {
            foreach ($child in $children[$processId]) { $pending.Enqueue($child) }
        }
    }

    $snapshots = [System.Collections.Generic.List[object]]::new()
    foreach ($processId in $processIds) {
        try {
            $process = Get-Process -Id $processId -ErrorAction Stop
            $snapshots.Add([pscustomobject]@{
                ProcessId = $processId
                ParentProcessId = ($processRows | Where-Object ProcessId -eq $processId | Select-Object -First 1).ParentProcessId
                Name = $process.ProcessName
                CPUSeconds = [double]$process.CPU
                WorkingSetBytes = [long]$process.WorkingSet64
                PrivateMemoryBytes = [long]$process.PrivateMemorySize64
                ThreadCount = [int]$process.Threads.Count
                HandleCount = [int]$process.HandleCount
            })
        }
        catch { }
    }
    return @($snapshots)
}

function Invoke-MonitoredTestRun {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments,

        [Parameter(Mandatory = $true)]
        [string] $RunDirectory,

        [Parameter(Mandatory = $true)]
        [string] $OutputPath
    )

    $errorPath = Join-Path $RunDirectory 'test-error.log'
    $samplesPath = Join-Path $RunDirectory 'process-samples.jsonl'
    $eventsPath = Join-Path $RunDirectory 'process-events.jsonl'
    $treePath = Join-Path $RunDirectory 'testhost-tree.json'
    Remove-Item -LiteralPath $samplesPath, $eventsPath -Force -ErrorAction SilentlyContinue

    $startedUtc = [DateTimeOffset]::UtcNow
    $process = Start-Process -FilePath 'dotnet' -ArgumentList $Arguments -WorkingDirectory $repositoryRoot `
        -RedirectStandardOutput $OutputPath -RedirectStandardError $errorPath -PassThru
    $knownPids = @{}
    $traceProcess = $null
    $tracePath = Join-Path $RunDirectory 'trace.nettrace'
    $traceLog = Join-Path $RunDirectory 'trace.log'
    $traceErrorLog = Join-Path $RunDirectory 'trace-error.log'
    $lastSample = $null
    $sampleCount = 0

    Add-JsonLine $eventsPath ([pscustomobject]@{ Kind = 'root-start'; Utc = $startedUtc; ProcessId = $process.Id })
    while (-not $process.HasExited) {
        $now = [DateTimeOffset]::UtcNow
        $snapshot = @(Get-ProcessSnapshot -RootPid $process.Id)
        foreach ($item in $snapshot) {
            if (-not $knownPids.ContainsKey($item.ProcessId)) {
                $knownPids[$item.ProcessId] = $item
                Add-JsonLine $eventsPath ([pscustomobject]@{
                    Kind = 'process-start'
                    Utc = $now
                    ProcessId = $item.ProcessId
                    ParentProcessId = $item.ParentProcessId
                    Name = $item.Name
                })
            }
        }

        if ($Trace -and $null -eq $traceProcess) {
            $testHost = $snapshot | Where-Object {
                $_.Name -like '*testhost*'
            } | Select-Object -First 1
            if ($null -ne $testHost) {
                $traceProcess = Start-Process -FilePath 'dotnet-trace' `
                    -ArgumentList @('collect', '--process-id', $testHost.ProcessId, '--output', $tracePath) `
                    -RedirectStandardOutput $traceLog -RedirectStandardError $traceErrorLog -PassThru
                Add-JsonLine $eventsPath ([pscustomobject]@{
                    Kind = 'trace-start'
                    Utc = $now
                    ProcessId = $testHost.ProcessId
                    Output = $tracePath
                })
            }
        }

        $processor = Get-CimInstance Win32_Processor | Measure-Object -Property LoadPercentage -Average
        $os = Get-CimInstance Win32_OperatingSystem
        $totalCpu = ($snapshot | Measure-Object -Property CPUSeconds -Sum).Sum
        $workingSet = ($snapshot | Measure-Object -Property WorkingSetBytes -Sum).Sum
        $privateBytes = ($snapshot | Measure-Object -Property PrivateMemoryBytes -Sum).Sum
        Add-JsonLine $samplesPath ([pscustomobject]@{
            Utc = $now
            ElapsedSeconds = ($now - $startedUtc).TotalSeconds
            RootProcessId = $process.Id
            ProcessCount = $snapshot.Count
            ProcessIds = @($snapshot.ProcessId)
            CPUSeconds = $totalCpu
            WorkingSetBytes = $workingSet
            PrivateMemoryBytes = $privateBytes
            ThreadCount = ($snapshot | Measure-Object -Property ThreadCount -Sum).Sum
            HandleCount = ($snapshot | Measure-Object -Property HandleCount -Sum).Sum
            SystemCpuPercent = $processor.Average
            AvailableMemoryBytes = [long]$os.FreePhysicalMemory * 1KB
        })
        $sampleCount++
        Start-Sleep -Milliseconds $SampleMilliseconds
        $process.Refresh()
    }

    $process.WaitForExit()
    $process.Refresh()
    $exitCode = try { [int]$process.ExitCode } catch { 1 }
    $finishedUtc = [DateTimeOffset]::UtcNow
    foreach ($processId in @($knownPids.Keys)) {
        Add-JsonLine $eventsPath ([pscustomobject]@{ Kind = 'process-exit-observed'; Utc = $finishedUtc; ProcessId = $processId })
    }
    if ($null -ne $traceProcess) {
        $traceProcess.WaitForExit()
        Add-JsonLine $eventsPath ([pscustomobject]@{ Kind = 'trace-exit'; Utc = [DateTimeOffset]::UtcNow; ProcessId = $traceProcess.Id })
    }
    ([pscustomobject]@{
        StartedUtc = $startedUtc
        FinishedUtc = $finishedUtc
        RootProcessId = $process.Id
        ExitCode = $exitCode
        SampleCount = $sampleCount
        ProcessIds = @($knownPids.Keys)
        TracePath = if ($Trace) { $tracePath } else { $null }
    } | ConvertTo-Json -Depth 6) | Out-File -LiteralPath $treePath -Encoding utf8

    return [pscustomobject]@{
        ExitCode = $exitCode
        Output = @()
        ErrorPath = $errorPath
        TreePath = $treePath
    }
}

function Get-CommandText([string] $FilePath, [string[]] $Arguments) {
    return @($FilePath) + @($Arguments) -join ' '
}

function Get-TrxRunSummary([string] $RunDirectory) {
    $trxFiles = @(Get-ChildItem -LiteralPath $RunDirectory -Recurse -Filter '*.trx' -File -ErrorAction SilentlyContinue)
    if ($trxFiles.Count -eq 0) {
        return [pscustomobject]@{
            IsComplete = $false
            Failure = 'The test process did not produce a TRX result.'
            FileCount = 0
            Total = 0
            Passed = 0
            Failed = 0
            NotExecuted = 0
        }
    }

    $total = 0
    $passed = 0
    $failed = 0
    $notExecuted = 0
    $invalidOutcomes = [System.Collections.Generic.List[string]]::new()
    foreach ($trxFile in $trxFiles) {
        [xml]$document = Get-Content -LiteralPath $trxFile.FullName -Raw
        $summary = $document.TestRun.ResultSummary
        $counters = $summary.Counters
        $total += [int]$counters.total
        $passed += [int]$counters.passed
        $failed += [int]$counters.failed
        $notExecuted += [int]$counters.notExecuted
        if ([string]$summary.outcome -ne 'Completed') {
            $invalidOutcomes.Add("$($trxFile.Name): $($summary.outcome)")
        }
    }

    $failure = if ($invalidOutcomes.Count -gt 0) {
        "TRX outcome was not Completed: $($invalidOutcomes -join '; ')."
    }
    elseif ($failed -gt 0) {
        "$failed test result(s) failed."
    }
    elseif ($total -eq 0) {
        'The test process produced an empty TRX result.'
    }
    else {
        $null
    }

    return [pscustomobject]@{
        IsComplete = $null -eq $failure
        Failure = $failure
        FileCount = $trxFiles.Count
        Total = $total
        Passed = $passed
        Failed = $failed
        NotExecuted = $notExecuted
    }
}

$gitCommit = (& git -C $repositoryRoot rev-parse HEAD).Trim()
$gitBranch = (& git -C $repositoryRoot branch --show-current).Trim()
$gitStatus = @(& git -C $repositoryRoot status --short)
$dotnetVersion = (& dotnet --version).Trim()
$dotnetInfoPath = Join-Path $root 'dotnet-info.txt'
(& dotnet --info) | Out-File -LiteralPath $dotnetInfoPath -Encoding utf8

$os = Get-CimInstance Win32_OperatingSystem
$computer = Get-CimInstance Win32_ComputerSystem
$processor = Get-CimInstance Win32_Processor | Select-Object -First 1
$environment = [ordered]@{
    RecordedUtc = [DateTimeOffset]::UtcNow
    RepositoryRoot = $repositoryRoot
    Commit = $gitCommit
    Branch = $gitBranch
    CleanAtStart = ($gitStatus.Count -eq 0)
    GitStatus = $gitStatus
    ProjectPath = $ProjectPath
    Configuration = $Configuration
    DotnetVersion = $dotnetVersion
    DotnetInfoPath = $dotnetInfoPath
    OS = $os.Caption
    OSVersion = $os.Version
    OSBuild = $os.BuildNumber
    Computer = $computer.Name
    LogicalProcessors = $computer.NumberOfLogicalProcessors
    PhysicalMemoryBytes = $computer.TotalPhysicalMemory
    Processor = $processor.Name
    ProcessorCount = $processor.NumberOfCores
    EnvironmentVariables = [ordered]@{
        MUSOQ_EVALUATOR_CORPUS_DEGREE = $env:MUSOQ_EVALUATOR_CORPUS_DEGREE
        DOTNET_CLI_TELEMETRY_OPTOUT = $env:DOTNET_CLI_TELEMETRY_OPTOUT
    }
}
$environment | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $root 'environment.json') -Encoding utf8

$testArguments = @(
    'test',
    (Join-Path $repositoryRoot $ProjectPath),
    '-c', $Configuration,
    '--nologo',
    '--logger', 'trx',
    '--results-directory', ''
)
if ($NoBuild) {
    $testArguments += '--no-build'
    $testArguments += '--no-restore'
}
if (-not [string]::IsNullOrWhiteSpace($Filter)) {
    $testArguments += '--filter'
    $testArguments += $Filter
}
if (-not [string]::IsNullOrWhiteSpace($RunSettings)) {
    $testArguments += '--settings'
    $testArguments += (Join-Path $repositoryRoot $RunSettings)
}

$runRecords = [System.Collections.Generic.List[object]]::new()
for ($run = 1; $run -le $RunCount; $run++) {
    $runDirectory = Join-Path $root ('run-{0:d2}' -f $run)
    New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
    $timingDirectory = Join-Path $runDirectory 'timing'
    New-Item -ItemType Directory -Path $timingDirectory -Force | Out-Null
    $runTestArguments = @($testArguments)
    $resultsArgumentIndex = [Array]::IndexOf($runTestArguments, '--results-directory') + 1
    $runTestArguments[$resultsArgumentIndex] = $runDirectory
    $commandText = Get-CommandText 'dotnet' $runTestArguments
    $logPath = Join-Path $runDirectory 'test-output.log'

    $previousTimingDirectory = $env:MUSOQ_EVALUATOR_TIMING_DIRECTORY
    $env:MUSOQ_EVALUATOR_TIMING_DIRECTORY = $timingDirectory
    $startedUtc = [DateTimeOffset]::UtcNow
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    try {
        $result = Invoke-MonitoredTestRun -Arguments $runTestArguments -RunDirectory $runDirectory -OutputPath $logPath
    }
    finally {
        $stopwatch.Stop()
        if ($null -eq $previousTimingDirectory) {
            Remove-Item Env:MUSOQ_EVALUATOR_TIMING_DIRECTORY -ErrorAction SilentlyContinue
        }
        else {
            $env:MUSOQ_EVALUATOR_TIMING_DIRECTORY = $previousTimingDirectory
        }
    }
    $finishedUtc = [DateTimeOffset]::UtcNow
    $trxSummary = Get-TrxRunSummary $runDirectory
    $record = [ordered]@{
        Run = $run
        StartedUtc = $startedUtc
        FinishedUtc = $finishedUtc
        WallClockMilliseconds = $stopwatch.Elapsed.TotalMilliseconds
        WallClockSeconds = $stopwatch.Elapsed.TotalSeconds
        ExitCode = $result.ExitCode
        Command = $commandText
        ResultsDirectory = $runDirectory
        OutputLog = $logPath
        TimingDirectory = $timingDirectory
        Trx = $trxSummary
    }
    $record | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $runDirectory 'run-metadata.json') -Encoding utf8
    $runRecords.Add([pscustomobject]$record)
    Write-Output ('Run {0}/{1}: {2:0.000}s, exit {3}' -f $run, $RunCount, $stopwatch.Elapsed.TotalSeconds, $result.ExitCode)
    if ($result.ExitCode -ne 0) {
        throw "Evaluator test run $run failed with exit code $($result.ExitCode). See '$logPath'."
    }
    if (-not $trxSummary.IsComplete) {
        throw "Evaluator test run $run did not complete successfully despite exit code 0: $($trxSummary.Failure) See '$logPath'."
    }
}

$runRecords | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $root 'runs.json') -Encoding utf8
Write-Output "Measurement complete: $root"
