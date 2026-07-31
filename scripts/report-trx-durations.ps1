[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string[]] $Path,

    [int] $Top = 25,

    [double] $UnexpectedSlowTestThresholdSeconds = 1.0,

    [string] $SlowTestAllowListPath,

    [switch] $FailOnUnexpectedSlowTests
)

if ([string]::IsNullOrWhiteSpace($SlowTestAllowListPath)) {
    $SlowTestAllowListPath = Join-Path $PSScriptRoot 'evaluator-slow-test-allowlist.txt'
}

$resolvedPaths = foreach ($inputPath in $Path) {
    $resolved = (Resolve-Path -LiteralPath $inputPath -ErrorAction Stop).Path
    if ((Get-Item -LiteralPath $resolved).PSIsContainer) {
        Get-ChildItem -LiteralPath $resolved -Recurse -Filter '*.trx' -File | Select-Object -ExpandProperty FullName
    }
    else {
        $resolved
    }
}

$resolvedPaths = @($resolvedPaths | Sort-Object -Unique)
if ($resolvedPaths.Count -eq 0) {
    throw "No TRX files were found."
}

$documents = foreach ($resolvedPath in $resolvedPaths) {
    [xml](Get-Content -LiteralPath $resolvedPath -Raw)
}

$metricsPaths = foreach ($inputPath in $Path) {
    $resolved = (Resolve-Path -LiteralPath $inputPath -ErrorAction Stop).Path
    if ((Get-Item -LiteralPath $resolved).PSIsContainer) {
        Get-ChildItem -LiteralPath $resolved -Recurse -Filter 'generated-code-sample-timing-*.jsonl' -File |
            Select-Object -ExpandProperty FullName
    }
    else {
        $directory = Split-Path -Parent $resolved
        Get-ChildItem -LiteralPath $directory -Recurse -Filter 'generated-code-sample-timing-*.jsonl' -File |
            Select-Object -ExpandProperty FullName
    }
}

$metrics = foreach ($metricsPath in @($metricsPaths | Sort-Object -Unique)) {
    try {
        Get-Content -LiteralPath $metricsPath |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { $_ | ConvertFrom-Json }
    }
    catch {
        Write-Warning "Could not read generated-code timing telemetry '$metricsPath': $($_.Exception.Message)"
    }
}

$results = foreach ($document in $documents) {
    @($document.TestRun.Results.UnitTestResult)
}

if (@($results).Count -eq 0) {
    throw "No UnitTestResult entries were found in '$resolvedPath'."
}

$invariantCulture = [Globalization.CultureInfo]::InvariantCulture

function Format-Seconds([double] $seconds) {
    return [string]::Format($invariantCulture, '{0:0.000}', $seconds)
}

function Get-TestCategory([string] $testName) {
    if ($testName -match 'Benchmark|Benchmarks') {
        return 'benchmark'
    }

    if ($testName -match 'GeneratedCode|GeneratedSample|Catalog|Corpus|Sample') {
        return 'generated-sample'
    }

    if ($testName -match 'StressTest|Compile|Compilation|Repeated|Repeat') {
        return 'repeated-compilation'
    }

    if ($testName -match 'Integration|EndToEnd|E2E') {
        return 'integration'
    }

    return 'runtime'
}

$measured = foreach ($result in $results) {
    $duration = if ([string]::IsNullOrWhiteSpace([string] $result.duration)) {
        [TimeSpan]::Zero
    }
    else {
        [TimeSpan]::Parse($result.duration, [Globalization.CultureInfo]::InvariantCulture)
    }
    [pscustomobject]@{
        Name = [string] $result.testName
        Duration = $duration
        Seconds = $duration.TotalSeconds
        Category = Get-TestCategory ([string] $result.testName)
        Outcome = [string] $result.outcome
        Start = if ([string]::IsNullOrWhiteSpace([string] $result.startTime)) { $null } else { [DateTimeOffset]::Parse([string] $result.startTime) }
        Finish = if ([string]::IsNullOrWhiteSpace([string] $result.endTime)) { $null } else { [DateTimeOffset]::Parse([string] $result.endTime) }
        SetupOverlapSeconds = 0.0
        AdjustedSeconds = $duration.TotalSeconds
    }
}

$starts = foreach ($document in $documents) {
    if ($document.TestRun.Times.start) {
        [DateTimeOffset]::Parse($document.TestRun.Times.start)
    }
}
$finishes = foreach ($document in $documents) {
    if ($document.TestRun.Times.finish) {
        [DateTimeOffset]::Parse($document.TestRun.Times.finish)
    }
}
$wallClock = $null
if ($starts -and $finishes) {
    $wallClock = (($finishes | Measure-Object -Maximum).Maximum - ($starts | Measure-Object -Minimum).Minimum)
}

Write-Output "TRX files: $($resolvedPaths -join ', ')"
Write-Output "Recorded results: $(@($measured).Count)"
if ($null -ne $wallClock) {
    Write-Output "Wall-clock duration: $($wallClock.ToString())"
}
Write-Output ("Sum of individual durations: {0} seconds" -f (Format-Seconds (($measured | Measure-Object -Property Seconds -Sum).Sum)))
Write-Output ""
Write-Output "Slowest tests:"
$measured |
    Sort-Object Seconds -Descending |
    Select-Object -First $Top Name, Outcome, Category, @{Name = 'Seconds'; Expression = { Format-Seconds $_.Seconds }} |
    Format-Table -AutoSize |
    Out-String -Width 240 |
    Write-Output

Write-Output "Category summary:"
$measured |
    Group-Object Category |
    ForEach-Object {
        $sum = ($_.Group | Measure-Object -Property Seconds -Sum).Sum
        $max = ($_.Group | Measure-Object -Property Seconds -Maximum).Maximum
        [pscustomobject]@{
            Category = $_.Name
            Tests = $_.Count
            SumSeconds = Format-Seconds $sum
            SlowestSeconds = Format-Seconds $max
        }
    } |
    Sort-Object Category |
    Format-Table -AutoSize |
    Out-String -Width 160 |
    Write-Output

if (@($metrics).Count -gt 0) {
    $timingEvents = @($metrics | Where-Object { $null -ne $_ })
    $generationEvents = @($timingEvents | Where-Object { $_.Kind -eq 'generation' })
    $cacheHitEvents = @($timingEvents | Where-Object { $_.Kind -eq 'cache-hit' })
    $corpusSetupEvents = @($timingEvents | Where-Object { $_.Kind -eq 'corpus-setup' })
    Write-Output "Generated-code setup telemetry:"
    Write-Output ("Telemetry records: {0}" -f @($metrics).Count)
    Write-Output ("Generation events: {0}" -f @($generationEvents).Count)
    Write-Output ("Cache hits: {0}" -f @($cacheHitEvents).Count)
    Write-Output ("Distinct generated samples: {0}" -f @($generationEvents.FileName | Sort-Object -Unique).Count)
    if (@($corpusSetupEvents).Count -gt 0) {
        $setupIntervals = @($corpusSetupEvents | ForEach-Object {
            [pscustomobject]@{
                Start = [DateTimeOffset]::Parse([string] $_.StartedUtc)
                Finish = [DateTimeOffset]::Parse([string] $_.FinishedUtc)
                Seconds = ([DateTimeOffset]::Parse([string] $_.FinishedUtc) - [DateTimeOffset]::Parse([string] $_.StartedUtc)).TotalSeconds
            }
        })
        $setupDuration = ($setupIntervals | Measure-Object -Property Seconds -Sum).Sum
        $setupWallSpan = ($setupIntervals | Measure-Object -Property Seconds -Sum).Sum
        $sampleCount = ($corpusSetupEvents | Select-Object -ExpandProperty SampleCount -First 1)
        $degree = ($corpusSetupEvents | Select-Object -ExpandProperty DegreeOfParallelism -First 1)
        $allocatedBytes = ($corpusSetupEvents | Select-Object -ExpandProperty AllocatedBytes -First 1)
        Write-Output ("Cold corpus setup events: {0}" -f @($corpusSetupEvents).Count)
        Write-Output ("Cold corpus setup wall time total: {0} seconds" -f (Format-Seconds $setupWallSpan))
        Write-Output ("Cold corpus setup duration total: {0} seconds" -f (Format-Seconds $setupDuration))
        Write-Output ("Samples generated during cold setup: {0}" -f $sampleCount)
        Write-Output ("Corpus setup degree of parallelism: {0}" -f $degree)
        Write-Output ("Corpus setup allocated bytes: {0}" -f $allocatedBytes)
        foreach ($test in $measured) {
            if ($null -eq $test.Start -or $null -eq $test.Finish) {
                continue
            }

            $overlapSeconds = 0.0
            foreach ($interval in $setupIntervals) {
                $overlapStart = if ($test.Start -gt $interval.Start) { $test.Start } else { $interval.Start }
                $overlapFinish = if ($test.Finish -lt $interval.Finish) { $test.Finish } else { $interval.Finish }
                if ($overlapFinish -gt $overlapStart) {
                    $overlapSeconds += ($overlapFinish - $overlapStart).TotalSeconds
                }
            }
            if ($overlapSeconds -gt 0) {
                $test.SetupOverlapSeconds = $overlapSeconds
                $test.AdjustedSeconds = [Math]::Max(0.0, $test.Seconds - $overlapSeconds)
            }
        }
    }
    if (@($generationEvents).Count -gt 0) {
        $telemetrySum = ($generationEvents | Measure-Object -Property DurationMilliseconds -Sum).Sum / 1000
        $telemetryStarts = @($generationEvents | ForEach-Object { [DateTimeOffset]::Parse([string] $_.StartedUtc) })
        $telemetryFinishes = @($generationEvents | ForEach-Object { [DateTimeOffset]::Parse([string] $_.FinishedUtc) })
        $telemetrySpan = (($telemetryFinishes | Measure-Object -Maximum).Maximum - ($telemetryStarts | Measure-Object -Minimum).Minimum)
        Write-Output ("Sum of generation durations: {0} seconds" -f (Format-Seconds $telemetrySum))
        Write-Output ("Generation wall span: {0}" -f $telemetrySpan)

        $setupEventsForOverlap = if (@($corpusSetupEvents).Count -gt 0) { $corpusSetupEvents } else { $generationEvents }
        $setupIntervalsForOverlap = @($setupEventsForOverlap | ForEach-Object {
            [pscustomobject]@{
                Start = [DateTimeOffset]::Parse([string] $_.StartedUtc)
                Finish = [DateTimeOffset]::Parse([string] $_.FinishedUtc)
            }
        })
        $overlapping = @($measured | Where-Object {
            $test = $_
            $null -ne $test.Start -and $null -ne $test.Finish -and
            @($setupIntervalsForOverlap | Where-Object {
                $test.Start -lt $_.Finish -and $test.Finish -gt $_.Start
            }).Count -gt 0
        })
        Write-Output ("Tests overlapping generated-code setup: {0}" -f @($overlapping).Count)
        $overlapping |
            Sort-Object Seconds -Descending |
            Select-Object -First $Top Name, @{Name = 'Seconds'; Expression = { Format-Seconds $_.Seconds }}, @{Name = 'SetupOverlap'; Expression = { Format-Seconds $_.SetupOverlapSeconds }}, @{Name = 'Adjusted'; Expression = { Format-Seconds $_.AdjustedSeconds }} |
            Format-Table -AutoSize |
            Out-String -Width 240 |
            Write-Output
    }

    Write-Output "Slow tests above one second after cold corpus setup removal:"
    $measured |
        Where-Object { $_.AdjustedSeconds -gt 1 } |
        Sort-Object AdjustedSeconds -Descending |
        Select-Object -First $Top Name, Outcome, Category, @{Name = 'RawSeconds'; Expression = { Format-Seconds $_.Seconds }}, @{Name = 'Adjusted'; Expression = { Format-Seconds $_.AdjustedSeconds }} |
        Format-Table -AutoSize |
        Out-String -Width 240 |
        Write-Output
    }

if ($FailOnUnexpectedSlowTests) {
    $allowedSlowCategories = @(
        'generated-sample',
        'benchmark',
        'integration',
        'repeated-compilation')
    $allowedSlowTests = if (Test-Path -LiteralPath $SlowTestAllowListPath) {
        Get-Content -LiteralPath $SlowTestAllowListPath |
            ForEach-Object { $_.Trim() } |
            Where-Object { $_ -and -not $_.StartsWith('#', [StringComparison]::Ordinal) }
    }
    else {
        @()
    }

    $unexpectedSlowTests = @($measured | Where-Object {
        $_.Outcome -eq 'Passed' -and
        $_.AdjustedSeconds -gt $UnexpectedSlowTestThresholdSeconds -and
        $_.Category -notin $allowedSlowCategories -and
        $_.Name -notin $allowedSlowTests
    })

    Write-Output ("Unexpected slow-test threshold: {0} seconds" -f (Format-Seconds $UnexpectedSlowTestThresholdSeconds))
    Write-Output ("Slow-test allowlist: {0}" -f $SlowTestAllowListPath)
    if (@($unexpectedSlowTests).Count -gt 0) {
        $details = $unexpectedSlowTests |
            Sort-Object AdjustedSeconds -Descending |
            ForEach-Object { "{0} ({1} seconds, {2})" -f $_.Name, (Format-Seconds $_.AdjustedSeconds), $_.Category }
        throw "Unexpected evaluator tests exceeded the duration threshold: $($details -join '; ')"
    }

    Write-Output "No unexpected slow tests exceeded the configured threshold."
}
