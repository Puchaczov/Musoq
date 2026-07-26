[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string[]] $Path,

    [int] $Top = 25
)

$resolvedPaths = foreach ($inputPath in $Path) {
    $resolved = (Resolve-Path -LiteralPath $inputPath -ErrorAction Stop).Path
    if ((Get-Item -LiteralPath $resolved).PSIsContainer) {
        Get-ChildItem -LiteralPath $resolved -Filter '*.trx' -File | Select-Object -ExpandProperty FullName
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

$results = foreach ($document in $documents) {
    @($document.TestRun.Results.UnitTestResult)
}

if ($results.Count -eq 0) {
    throw "No UnitTestResult entries were found in '$resolvedPath'."
}

function Get-TestCategory([string] $testName) {
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
Write-Output "Recorded results: $($measured.Count)"
if ($null -ne $wallClock) {
    Write-Output "Wall-clock duration: $($wallClock.ToString())"
}
Write-Output ("Sum of individual durations: {0:N3} seconds" -f (($measured | Measure-Object -Property Seconds -Sum).Sum))
Write-Output ""
Write-Output "Slowest tests:"
$measured |
    Sort-Object Seconds -Descending |
    Select-Object -First $Top Name, Outcome, Category, @{Name = 'Seconds'; Expression = { '{0:N3}' -f $_.Seconds }} |
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
            SumSeconds = '{0:N3}' -f $sum
            SlowestSeconds = '{0:N3}' -f $max
        }
    } |
    Sort-Object Category |
    Format-Table -AutoSize |
    Out-String -Width 160 |
    Write-Output
