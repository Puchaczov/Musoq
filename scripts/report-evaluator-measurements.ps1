[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $Path,

    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
$resolvedRoot = (Resolve-Path -LiteralPath $Path).Path

function Get-Median([double[]] $Values) {
    $ordered = @($Values | Sort-Object)
    if ($ordered.Count -eq 0) { return 0.0 }
    $middle = [Math]::Floor($ordered.Count / 2)
    if (($ordered.Count % 2) -eq 1) { return [double]$ordered[$middle] }
    return ([double]$ordered[$middle - 1] + [double]$ordered[$middle]) / 2.0
}

function Get-Percentile([double[]] $Values, [double] $Percentile) {
    $ordered = @($Values | Sort-Object)
    if ($ordered.Count -eq 0) { return 0.0 }
    $index = [Math]::Ceiling(($Percentile / 100.0) * $ordered.Count) - 1
    $index = [Math]::Max(0, [Math]::Min($ordered.Count - 1, $index))
    return [double]$ordered[$index]
}

function Read-JsonLines([string] $FileName) {
    $files = @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Filter $FileName -File -ErrorAction SilentlyContinue)
    $mergedPath = [System.IO.Path]::GetFullPath((Join-Path $resolvedRoot $FileName))
    $files = @($files | Where-Object { [System.IO.Path]::GetFullPath($_.FullName) -ne $mergedPath })
    $items = [System.Collections.Generic.List[object]]::new()
    foreach ($file in $files) {
        foreach ($line in @(Get-Content -LiteralPath $file.FullName)) {
            if ([string]::IsNullOrWhiteSpace($line)) { continue }
            try {
                $items.Add([pscustomobject]@{ File = $file.FullName; Value = ($line | ConvertFrom-Json) })
            }
            catch { }
        }
    }
    return @($items)
}

function Write-MergedJsonLines([string] $FileName, [object[]] $Items) {
    $target = Join-Path $resolvedRoot $FileName
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Force
    }
    foreach ($item in $Items) {
        ($item.Value | ConvertTo-Json -Compress -Depth 12) | Add-Content -LiteralPath $target -Encoding utf8
    }
}

function Get-NormalizedParent([string] $Name) {
    if ([string]::IsNullOrWhiteSpace($Name)) { return '<unnamed>' }
    $index = $Name.IndexOf('(')
    if ($index -gt 0) { return $Name.Substring(0, $index) }
    return $Name
}

function Get-TrxResults {
    $results = [System.Collections.Generic.List[object]]::new()
    $runs = [System.Collections.Generic.List[object]]::new()
    foreach ($trxFile in @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Filter '*.trx' -File)) {
        [xml]$document = Get-Content -LiteralPath $trxFile.FullName -Raw
        $testRun = $document.TestRun
        $runStart = [DateTimeOffset]::Parse([string]$testRun.Times.start)
        $runFinish = [DateTimeOffset]::Parse([string]$testRun.Times.finish)
        $runs.Add([pscustomobject]@{
            File = $trxFile.FullName
            StartedUtc = $runStart
            FinishedUtc = $runFinish
            WallClockSeconds = ($runFinish - $runStart).TotalSeconds
        })
        foreach ($result in @($testRun.Results.UnitTestResult)) {
            $duration = if ([string]::IsNullOrWhiteSpace([string]$result.duration)) { 0.0 } else { [TimeSpan]::Parse([string]$result.duration).TotalSeconds }
            $start = $null
            $finish = $null
            try { $start = [DateTimeOffset]::Parse([string]$result.startTime); $finish = [DateTimeOffset]::Parse([string]$result.endTime) } catch { }
            $span = if ($null -ne $start -and $null -ne $finish) { ($finish - $start).TotalSeconds } else { 0.0 }
            $inflation = if ($duration -gt 0.001) { $span / $duration } else { 0.0 }
            $results.Add([pscustomobject]@{
                File = $trxFile.FullName
                Name = [string]$result.testName
                Parent = Get-NormalizedParent ([string]$result.testName)
                Outcome = [string]$result.outcome
                Seconds = $duration
                StartUtc = $start
                EndUtc = $finish
                TimestampSpanSeconds = $span
                TimestampInflation = $inflation
                TimestampReliable = ($inflation -le 3.0 -or $span -le 1.0)
            })
        }
    }
    return [pscustomobject]@{ Runs = @($runs); Results = @($results) }
}

function Get-ProcessSummary {
    param([object[]] $Items)
    $samples = @($Items | ForEach-Object { $_.Value } | Sort-Object Utc)
    if ($samples.Count -eq 0) {
        return [pscustomobject]@{ SampleCount = 0; CPUSeconds = 0; PeakWorkingSetBytes = 0; PeakPrivateMemoryBytes = 0; AverageSystemCpuPercent = 0; LowCpuSamples = 0 }
    }
    # CPUSeconds is an aggregate over a process tree. It resets for every
    # monitored dotnet invocation and can decrease when a child exits. Sum
    # only positive deltas within each root lifetime; never subtract counters
    # from separate runs or infer negative CPU from a changing tree.
    $cpu = 0.0
    foreach ($rootGroup in @($samples | Group-Object RootProcessId)) {
        $rootSamples = @($rootGroup.Group | Sort-Object Utc)
        for ($index = 1; $index -lt $rootSamples.Count; $index++) {
            $delta = [double]$rootSamples[$index].CPUSeconds - [double]$rootSamples[$index - 1].CPUSeconds
            if ($delta -gt 0) { $cpu += $delta }
        }
    }
    $systemCpu = @($samples | ForEach-Object { [double]$_.SystemCpuPercent })
    $private = @($samples | ForEach-Object { [long]$_.PrivateMemoryBytes })
    $working = @($samples | ForEach-Object { [long]$_.WorkingSetBytes })
    $lowCpu = 0
    for ($index = 1; $index -lt $samples.Count; $index++) {
        $cpuDelta = [double]$samples[$index].CPUSeconds - [double]$samples[$index - 1].CPUSeconds
        if ($cpuDelta -lt 0.25 -and [double]$samples[$index].SystemCpuPercent -lt 10) {
            $lowCpu++
        }
    }
    return [pscustomobject]@{
        SampleCount = $samples.Count
        CPUSeconds = $cpu
        PeakWorkingSetBytes = ($working | Measure-Object -Maximum).Maximum
        PeakPrivateMemoryBytes = ($private | Measure-Object -Maximum).Maximum
        AverageSystemCpuPercent = ($systemCpu | Measure-Object -Average).Average
        LowCpuSamples = $lowCpu
    }
}

function Get-ActiveIntervalSummary {
    param([object[]] $Items, [object[]] $Runs)
    $intervals = @($Items | ForEach-Object {
        $value = $_.Value
        try {
            [pscustomobject]@{ Start = [DateTimeOffset]::Parse([string]$value.StartedUtc); End = [DateTimeOffset]::Parse([string]$value.FinishedUtc) }
        } catch { }
    } | Where-Object { $null -ne $_ -and $_.End -gt $_.Start } | Sort-Object Start)
    $merged = [System.Collections.Generic.List[object]]::new()
    foreach ($interval in $intervals) {
        if ($merged.Count -eq 0 -or $interval.Start -gt $merged[$merged.Count - 1].End) {
            $merged.Add([pscustomobject]@{ Start = $interval.Start; End = $interval.End })
        }
        elseif ($interval.End -gt $merged[$merged.Count - 1].End) {
            $merged[$merged.Count - 1].End = $interval.End
        }
    }
    $active = ($merged | ForEach-Object { ($_.End - $_.Start).TotalSeconds } | Measure-Object -Sum).Sum
    $wall = ($Runs | ForEach-Object { $_.WallClockSeconds } | Measure-Object -Sum).Sum
    [pscustomobject]@{
        ExplicitIntervals = $intervals.Count
        MergedIntervals = $merged.Count
        ActiveSeconds = if ($null -eq $active) { 0.0 } else { $active }
        CoveredWallSeconds = if ($null -eq $wall) { 0.0 } else { $wall }
        UnobservedSeconds = [Math]::Max(0.0, $wall - $active)
        Note = 'Only explicitly instrumented test scopes are included; uninstrumented tests are not inferred as idle.'
    }
}

$trx = Get-TrxResults
$results = @($trx.Results)
$runMetadataPath = Join-Path $resolvedRoot 'runs.json'
$runMetadata = @()
if (Test-Path -LiteralPath $runMetadataPath) {
    $runMetadata = @(Get-Content -LiteralPath $runMetadataPath -Raw | ConvertFrom-Json)
}
$wallRuns = [System.Collections.Generic.List[object]]::new()
if ($runMetadata.Count -gt 0) {
    foreach ($metadata in @($runMetadata | ForEach-Object { $_ })) {
        foreach ($wallClockSeconds in @($metadata.WallClockSeconds)) {
            $wallRuns.Add([pscustomobject]@{ WallClockSeconds = [double]$wallClockSeconds })
        }
    }
}
else {
    foreach ($trxRun in $trx.Runs) {
        $wallRuns.Add([pscustomobject]@{ WallClockSeconds = [double]$trxRun.WallClockSeconds })
    }
}
$timingItems = @(Read-JsonLines 'test-case-events.jsonl')
$compilationItems = @(Read-JsonLines 'compilation-stages.jsonl')
$batchItems = @(Read-JsonLines 'execution-batches.jsonl')
$processItems = @(Read-JsonLines 'process-samples.jsonl')

$groups = foreach ($group in @($results | Group-Object Parent)) {
    $values = @($group.Group | ForEach-Object { [double]$_.Seconds })
    [pscustomobject]@{
        ParentMethod = $group.Name
        ResultCount = $group.Count
        Passed = @($group.Group | Where-Object Outcome -eq 'Passed').Count
        Failed = @($group.Group | Where-Object Outcome -eq 'Failed').Count
        SumSeconds = ($values | Measure-Object -Sum).Sum
        MinSeconds = ($values | Measure-Object -Minimum).Minimum
        MedianSeconds = Get-Median $values
        P95Seconds = Get-Percentile $values 95
        MaxSeconds = ($values | Measure-Object -Maximum).Maximum
        TimestampInflatedResults = @($group.Group | Where-Object { -not $_.TimestampReliable }).Count
    }
}

$compilationValues = @($compilationItems | ForEach-Object { $_.Value })
$batchValues = @($batchItems | ForEach-Object { $_.Value })
$compilationSummary = [pscustomobject]@{
    EventCount = $compilationValues.Count
    TotalSeconds = (($compilationValues | ForEach-Object { [double]$_.totalMilliseconds } | Measure-Object -Sum).Sum / 1000.0)
    CacheHits = @($compilationValues | Where-Object cacheOutcome -eq 'hit').Count
    CacheMisses = @($compilationValues | Where-Object cacheOutcome -eq 'miss').Count
    CacheIneligible = @($compilationValues | Where-Object cacheOutcome -eq 'not-eligible').Count
    ActualEmissionEvents = @($compilationValues | Where-Object { $_.realEmission -eq $true }).Count +
                           @($batchValues | Where-Object { $_.realEmission -eq $true }).Count
    ActualLoadEvents = @($compilationValues | Where-Object { $_.realLoad -eq $true }).Count +
                       @($batchValues | Where-Object { $_.realLoad -eq $true }).Count
    Modes = [ordered]@{}
    PhaseInclusiveSeconds = [ordered]@{}
    PhaseExclusiveSeconds = [ordered]@{}
    PhaseCounts = [ordered]@{}
    PhaseMaxInclusiveSeconds = [ordered]@{}
}
foreach ($mode in @($compilationValues | ForEach-Object { [string]$_.compilationMode } | Where-Object { $_ } | Sort-Object -Unique)) {
    $compilationSummary.Modes[$mode] = @($compilationValues | Where-Object { $_.compilationMode -eq $mode }).Count
}
function Add-PhaseMetric([string] $Name, [double] $Inclusive, [double] $Exclusive, [int] $Count, [double] $Maximum) {
    if (-not $compilationSummary.PhaseInclusiveSeconds.Contains($Name)) {
        $compilationSummary.PhaseInclusiveSeconds[$Name] = 0.0
        $compilationSummary.PhaseExclusiveSeconds[$Name] = 0.0
        $compilationSummary.PhaseCounts[$Name] = 0
        $compilationSummary.PhaseMaxInclusiveSeconds[$Name] = 0.0
    }

    $compilationSummary.PhaseInclusiveSeconds[$Name] += $Inclusive / 1000.0
    $compilationSummary.PhaseExclusiveSeconds[$Name] += $Exclusive / 1000.0
    $compilationSummary.PhaseCounts[$Name] += $Count
    $compilationSummary.PhaseMaxInclusiveSeconds[$Name] = [Math]::Max(
        [double]$compilationSummary.PhaseMaxInclusiveSeconds[$Name],
        $Maximum / 1000.0)
}

foreach ($compilation in $compilationValues) {
    if ($null -eq $compilation.phaseDetails) {
        foreach ($property in @($compilation.phases.psobject.Properties)) {
            Add-PhaseMetric $property.Name ([double]$property.Value) ([double]$property.Value) 1 ([double]$property.Value)
        }
        continue
    }

    foreach ($property in @($compilation.phaseDetails.psobject.Properties)) {
        $details = $property.Value
        Add-PhaseMetric $property.Name `
            ([double]$details.inclusiveMilliseconds) `
            ([double]$details.exclusiveMilliseconds) `
            ([int]$details.count) `
            ([double]$details.maxInclusiveMilliseconds)
    }
}

$inclusiveStageSeconds = ($compilationSummary.PhaseInclusiveSeconds.Values | Measure-Object -Sum).Sum
$exclusiveStageSeconds = ($compilationSummary.PhaseExclusiveSeconds.Values | Measure-Object -Sum).Sum
$compilationSummary | Add-Member -NotePropertyName PhaseAccounting -NotePropertyValue ([ordered]@{
    InclusiveStageSeconds = if ($null -eq $inclusiveStageSeconds) { 0.0 } else { [double]$inclusiveStageSeconds }
    ExclusiveStageSeconds = if ($null -eq $exclusiveStageSeconds) { 0.0 } else { [double]$exclusiveStageSeconds }
    NestedInclusiveOvercountSeconds = if ($null -eq $inclusiveStageSeconds -or $null -eq $exclusiveStageSeconds) { 0.0 } else { [double]($inclusiveStageSeconds - $exclusiveStageSeconds) }
    Rule = 'Use exclusive totals for stage comparison. Inclusive totals include nested child phases and must not be summed as independent work.'
})
$parentChildOverlapEvents = 0
foreach ($compilation in $compilationValues) {
    if ($null -eq $compilation.phaseDetails -or $null -eq $compilation.phaseDetails.build) { continue }
    $build = $compilation.phaseDetails.build
    $childInclusive = 0.0
    foreach ($property in @($compilation.phaseDetails.psobject.Properties)) {
        if ($property.Name -eq 'build') { continue }
        $childInclusive += [double]$property.Value.inclusiveMilliseconds
    }
    if ([double]$build.inclusiveMilliseconds -lt ($childInclusive - 0.5)) {
        $parentChildOverlapEvents++
    }
}
$compilationSummary.PhaseAccounting.ParentStages = @('build')
$compilationSummary.PhaseAccounting.ParentChildOverlapEvents = $parentChildOverlapEvents

$testCaseValues = @($timingItems | ForEach-Object { $_.Value })
$testCaseSummary = foreach ($group in @($testCaseValues | Group-Object ParentMethod)) {
    $elapsed = @($group.Group | ForEach-Object { [double]$_.ElapsedMilliseconds / 1000.0 })
    [pscustomobject]@{
        ParentMethod = $group.Name
        CaseCount = $group.Count
        SumSeconds = ($elapsed | Measure-Object -Sum).Sum
        MinSeconds = ($elapsed | Measure-Object -Minimum).Minimum
        MedianSeconds = Get-Median $elapsed
        P95Seconds = Get-Percentile $elapsed 95
        MaxSeconds = ($elapsed | Measure-Object -Maximum).Maximum
        MaterializationIncomplete = @($group.Group | Where-Object { -not $_.MaterializationCompleted }).Count
    }
}

$batchSummary = [ordered]@{
    EventCount = $batchValues.Count
    ItemCount = (($batchValues | ForEach-Object { [int]$_.itemCount } | Measure-Object -Sum).Sum)
    SuccessfulBatches = @($batchValues | Where-Object { $_.succeeded -eq $true }).Count
    FailedBatches = @($batchValues | Where-Object { $_.succeeded -ne $true }).Count
    EmittingBatches = @($batchValues | Where-Object { $_.realEmission -eq $true }).Count
    LoadingBatches = @($batchValues | Where-Object { $_.realLoad -eq $true }).Count
    TotalSeconds = (($batchValues | ForEach-Object { [double]$_.totalMilliseconds } | Measure-Object -Sum).Sum / 1000.0)
    Modes = [ordered]@{}
}
$batchOrigins = foreach ($group in @($batchValues | Group-Object origin)) {
    $sizes = @($group.Group | ForEach-Object { [int]$_.itemCount })
    $delays = @($group.Group | Where-Object { $null -ne $_.queueDelayMilliseconds } | ForEach-Object { [double]$_.queueDelayMilliseconds })
    [pscustomobject]@{
        Origin = if ([string]::IsNullOrWhiteSpace($group.Name)) { '<unknown>' } else { $group.Name }
        BatchCount = $group.Count
        ItemCount = ($sizes | Measure-Object -Sum).Sum
        AverageBatchSize = if ($sizes.Count -eq 0) { 0.0 } else { ($sizes | Measure-Object -Average).Average }
        MaxBatchSize = if ($sizes.Count -eq 0) { 0 } else { ($sizes | Measure-Object -Maximum).Maximum }
        AverageQueueDelayMilliseconds = if ($delays.Count -eq 0) { 0.0 } else { ($delays | Measure-Object -Average).Average }
        FallbackCount = @($group.Group | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.fallbackReason) }).Count
    }
}
$batchSummary['Origins'] = @($batchOrigins)
$batchSummary['AverageBatchSize'] = if ($batchValues.Count -eq 0) { 0.0 } else { [double]$batchSummary.ItemCount / $batchValues.Count }

$wall = @($wallRuns | ForEach-Object { [double]$_.WallClockSeconds })
$sum = ($results | ForEach-Object { [double]$_.Seconds } | Measure-Object -Sum).Sum
$processSummary = Get-ProcessSummary $processItems
$activeSummary = Get-ActiveIntervalSummary $timingItems $wallRuns
$processEvents = @(Read-JsonLines 'process-events.jsonl' | ForEach-Object { $_.Value })
$testHostStarts = @($processEvents | Where-Object { $_.Kind -eq 'process-start' -and $_.Name -like '*testhost*' }).Count
$processSummary | Add-Member -NotePropertyName TestHostStarts -NotePropertyValue $testHostStarts
$processSummary | Add-Member -NotePropertyName TestHostRestarts -NotePropertyValue ([Math]::Max(0, $testHostStarts - 1))
$processSummary | Add-Member -NotePropertyName GcCounters -NotePropertyValue 'Not collected; dotnet-counters was unavailable. Use trace.nettrace for GC analysis.'
$activeSummary | Add-Member -NotePropertyName Process -NotePropertyValue $processSummary
$activeSummary | Add-Member -NotePropertyName Classification -NotePropertyValue 'Explicit active intervals are measured only for instrumented cohorts. Remaining wall time is unobserved, not proven idle.'
$summary = [ordered]@{
    Path = $resolvedRoot
    RunCount = @($trx.Runs).Count
    WallClockSeconds = [ordered]@{ Median = Get-Median $wall; Minimum = ($wall | Measure-Object -Minimum).Minimum; Maximum = ($wall | Measure-Object -Maximum).Maximum; Values = $wall }
    Tests = [ordered]@{ Total = $results.Count; Passed = @($results | Where-Object Outcome -eq 'Passed').Count; Failed = @($results | Where-Object Outcome -eq 'Failed').Count; Skipped = @($results | Where-Object { $_.Outcome -in @('Skipped','NotExecuted') }).Count; SumSeconds = $sum }
    TimestampQuality = [ordered]@{ Reliable = @($results | Where-Object TimestampReliable).Count; Inflated = @($results | Where-Object { -not $_.TimestampReliable }).Count; MaximumInflation = ($results | Measure-Object TimestampInflation -Maximum).Maximum }
     Compilation = $compilationSummary
    Batches = $batchSummary
    TestCaseTelemetry = [ordered]@{ EventCount = $testCaseValues.Count; Groups = @($testCaseSummary) }
    Process = $processSummary
    ActiveIntervals = $activeSummary
    SlowestTests = @($results | Sort-Object Seconds -Descending | Select-Object -First 50)
    SlowestGroups = @($groups | Sort-Object SumSeconds -Descending)
}

$summary | ConvertTo-Json -Depth 12 | Out-File -LiteralPath (Join-Path $resolvedRoot 'measurement-summary.json') -Encoding utf8
$groups | ConvertTo-Json -Depth 8 | Out-File -LiteralPath (Join-Path $resolvedRoot 'test-groups.json') -Encoding utf8
$activeSummary | ConvertTo-Json -Depth 8 | Out-File -LiteralPath (Join-Path $resolvedRoot 'blocking-analysis.json') -Encoding utf8
Write-MergedJsonLines 'compilation-stages.jsonl' $compilationItems
Write-MergedJsonLines 'test-case-events.jsonl' $timingItems
Write-MergedJsonLines 'execution-batches.jsonl' $batchItems

Write-Output "Measurement root: $resolvedRoot"
Write-Output ("Runs: {0}; wall median: {1:0.000}s; min: {2:0.000}s; max: {3:0.000}s" -f $summary.RunCount, $summary.WallClockSeconds.Median, $summary.WallClockSeconds.Minimum, $summary.WallClockSeconds.Maximum)
Write-Output ("Tests: {0} total, {1} passed, {2} failed, {3} skipped; summed durations: {4:0.000}s" -f $summary.Tests.Total, $summary.Tests.Passed, $summary.Tests.Failed, $summary.Tests.Skipped, $summary.Tests.SumSeconds)
Write-Output ("TRX timestamp quality: {0} reliable, {1} inflated; max inflation {2:0.0}x" -f $summary.TimestampQuality.Reliable, $summary.TimestampQuality.Inflated, $summary.TimestampQuality.MaximumInflation)
Write-Output ("Telemetry: {0} test cases, {1} compilations; cache hits {2}, misses {3}, ineligible {4}; real emissions {5}, real loads {6}" -f $summary.TestCaseTelemetry.EventCount, $summary.Compilation.EventCount, $summary.Compilation.CacheHits, $summary.Compilation.CacheMisses, $summary.Compilation.CacheIneligible, $summary.Compilation.ActualEmissionEvents, $summary.Compilation.ActualLoadEvents)
Write-Output ("Compilation phases: {0:0.000}s exclusive, {1:0.000}s inclusive, {2:0.000}s nested overcount" -f $summary.Compilation.PhaseAccounting.ExclusiveStageSeconds, $summary.Compilation.PhaseAccounting.InclusiveStageSeconds, $summary.Compilation.PhaseAccounting.NestedInclusiveOvercountSeconds)
Write-Output ("Batches: {0} events, {1} items, {2} emitting, {3} loading" -f $summary.Batches.EventCount, $summary.Batches.ItemCount, $summary.Batches.EmittingBatches, $summary.Batches.LoadingBatches)
Write-Output ("Process: {0:0.000}s CPU, {1:N0} MB peak private, {2:0.0}% average system CPU, {3} low-CPU samples, {4} testhost starts" -f $summary.Process.CPUSeconds, ($summary.Process.PeakPrivateMemoryBytes / 1MB), $summary.Process.AverageSystemCpuPercent, $summary.Process.LowCpuSamples, $summary.Process.TestHostStarts)
Write-Output ("Explicit active telemetry: {0:0.000}s; unobserved wall time: {1:0.000}s" -f $summary.ActiveIntervals.ActiveSeconds, $summary.ActiveIntervals.UnobservedSeconds)
Write-Output ''
Write-Output 'Slowest aggregate groups:'
$summary.SlowestGroups | Select-Object -First 20 | Format-Table -AutoSize | Out-String -Width 240 | Write-Output
Write-Output 'Slowest individual TRX results:'
$summary.SlowestTests | Select-Object -First 30 | Format-Table Parent,Seconds,TimestampInflation,Outcome -AutoSize | Out-String -Width 240 | Write-Output

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $resolvedRoot $OutputPath }
    $summary | ConvertTo-Json -Depth 12 | Out-File -LiteralPath $resolvedOutput -Encoding utf8
    Write-Output "Summary JSON: $resolvedOutput"
}
