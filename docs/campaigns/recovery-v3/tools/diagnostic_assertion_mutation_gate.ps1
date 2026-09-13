[CmdletBinding()]
param(
    [string] $EvidencePath = (Join-Path $PSScriptRoot '..\evidence\REC-088-mutation-results.json'),
    [string] $ScopeId = 'REC-088',
    [int] $TimeoutSeconds = 300
)

$ErrorActionPreference = 'Stop'

if ($TimeoutSeconds -lt 10) {
    throw 'TimeoutSeconds must be at least 10 seconds so normal focused test startup is observable.'
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('musoq-diagnostic-mutations-' + [Guid]::NewGuid().ToString('N'))
$evidenceFile = if ([System.IO.Path]::IsPathRooted($EvidencePath)) {
    [System.IO.Path]::GetFullPath($EvidencePath)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $EvidencePath))
}

$mutations = @(
    [ordered]@{
        Id = 'REC-088-M01'
        Name = 'wrong-code'
        Kind = 'critical'
        Layer = 'analysis-contract'
        File = 'src/dotnet/Musoq.Evaluator.Tests/DiagnosticContractTestAssertions.cs'
        Search = 'Assert.AreEqual(expectedCode, diagnostic.Code, Format(diagnostics));'
        Replacement = 'Assert.AreEqual(DiagnosticCode.MQ4006_InvalidFieldConstraint, diagnostic.Code, Format(diagnostics));'
        Occurrence = 1
        Project = 'src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj'
        Filter = 'FullyQualifiedName~DiagnosticBinary042BitAlignmentValidationTests.BinarySchema_UnknownCheckReference_ShouldReportExactStructuredMq2030'
        IntendedAssertion = 'DiagnosticContractTestAssertions.AssertSingleError rejects an incorrect diagnostic code.'
    }
    [ordered]@{
        Id = 'REC-088-M02'
        Name = 'wrong-phase'
        Kind = 'critical'
        Layer = 'analysis-contract'
        File = 'src/dotnet/Musoq.Evaluator.Tests/DiagnosticContractTestAssertions.cs'
        Search = 'Assert.AreEqual(DiagnosticPhaseMapping.FromCode(expectedCode), diagnostic.Phase);'
        Replacement = 'Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);'
        Occurrence = 1
        Project = 'src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj'
        Filter = 'FullyQualifiedName~DiagnosticBinary042BitAlignmentValidationTests.BinarySchema_NonBooleanCheck_ShouldReportExactStructuredMq4006'
        IntendedAssertion = 'DiagnosticContractTestAssertions.AssertSingleError rejects a phase that does not match the catalog.'
    }
    [ordered]@{
        Id = 'REC-088-M03'
        Name = 'wrong-source-kind'
        Kind = 'critical'
        Layer = 'analysis-contract'
        File = 'src/dotnet/Musoq.Evaluator.Tests/DiagnosticContractTestAssertions.cs'
        Search = 'Assert.AreEqual(ExpectedSourceKind(expectedCode), diagnostic.SourceKind);'
        Replacement = 'Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);'
        Occurrence = 1
        Project = 'src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj'
        Filter = 'FullyQualifiedName~DiagnosticBinary042BitAlignmentValidationTests.BinarySchema_NonBooleanCheck_ShouldReportExactStructuredMq4006'
        IntendedAssertion = 'DiagnosticContractTestAssertions.AssertSingleError rejects a source kind that does not match the diagnostic domain.'
    }
    [ordered]@{
        Id = 'REC-088-M04'
        Name = 'missing-symbol-argument'
        Kind = 'critical'
        Layer = 'analysis-symbol-facts'
        File = 'src/dotnet/Musoq.Evaluator.Tests/DiagnosticContractOracleTests.cs'
        Search = 'Assert.IsTrue(diagnostic.Arguments.ContainsKey("callable"),'
        Replacement = 'Assert.IsTrue(diagnostic.Arguments.ContainsKey("missing"),'
        Occurrence = 1
        Project = 'src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj'
        Filter = 'FullyQualifiedName~DiagnosticContractOracleTests.UnknownCallable_HasOneStructuredBindRoot'
        IntendedAssertion = 'The oracle rejects removal of the required callable symbol argument.'
    }
    [ordered]@{
        Id = 'REC-088-M05'
        Name = 'shifted-span-endpoint'
        Kind = 'critical'
        Layer = 'analysis-location'
        File = 'src/dotnet/Musoq.Evaluator.Tests/DiagnosticContractOracleTests.cs'
        Search = 'Assert.AreEqual(diagnostic.Location.Offset + diagnostic.Span.Length, diagnostic.EndLocation.Offset);'
        Replacement = 'Assert.AreEqual(diagnostic.Location.Offset + diagnostic.Span.Length + 1, diagnostic.EndLocation.Offset);'
        Occurrence = 1
        Project = 'src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj'
        Filter = 'FullyQualifiedName~DiagnosticContractOracleTests.UnknownCallable_HasOneStructuredBindRoot'
        IntendedAssertion = 'The oracle rejects an endpoint shifted one character beyond the diagnostic span.'
    }
    [ordered]@{
        Id = 'REC-088-M06'
        Name = 'fabricated-zero-offset'
        Kind = 'critical'
        Layer = 'generated-source-location'
        File = 'src/dotnet/Musoq.Converter/ExecutionTargets/TargetDiagnosticReporter.cs'
        Search = 'return (SourceLocation.None, SourceLocation.None);'
        Replacement = 'return (new SourceLocation(0, 1, 1, "<generated>"), new SourceLocation(0, 1, 1, "<generated>"));'
        Occurrence = 1
        Project = 'src/dotnet/Musoq.Converter.Tests/Musoq.Converter.Tests.csproj'
        Filter = 'FullyQualifiedName~TargetDiagnosticReporterTests.Report_WhenTargetHasNoRange_ShouldKeepTheLocationUnknownInsteadOfUsingSqlOffsetZero'
        IntendedAssertion = 'The no-range reporter test rejects a fabricated SQL-looking zero location.'
    }
    [ordered]@{
        Id = 'REC-088-M07'
        Name = 'removed-guidance'
        Kind = 'critical'
        Layer = 'diagnostic-guidance'
        File = 'src/dotnet/Musoq.Parser/Diagnostics/MusoqErrorEnvelope.cs'
        Search = 'return fixes.ToArray();'
        Replacement = 'return [];'
        Occurrence = 1
        Project = 'src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj'
        Filter = 'FullyQualifiedName~MalformedQueryErrorTests.WhenDescFunctionsOnNonExistentSchema_ShouldThrowError'
        IntendedAssertion = 'The shared envelope guidance assertion rejects a metadata-backed diagnostic with no suggested fixes.'
    }
    [ordered]@{
        Id = 'REC-088-M08'
        Name = 'parenthesized-diagnostic-list'
        Kind = 'equivalent'
        Layer = 'classification-control'
        File = 'src/dotnet/Musoq.Evaluator.Tests/DiagnosticContractTestAssertions.cs'
        Search = 'var diagnostics = result.Errors.ToList();'
        Replacement = 'var diagnostics = (result.Errors.ToList());'
        Occurrence = 1
        Project = 'src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj'
        Filter = 'FullyQualifiedName~DiagnosticBinary042BitAlignmentValidationTests.BinarySchema_UnknownCheckReference_ShouldReportExactStructuredMq2030'
        IntendedAssertion = 'A parenthesized pure expression must not be counted as a killed diagnostic mutant.'
    }
    [ordered]@{
        Id = 'REC-088-M09'
        Name = 'invalid-assertion-api'
        Kind = 'compilation-invalid'
        Layer = 'classification-control'
        File = 'src/dotnet/Musoq.Evaluator.Tests/DiagnosticContractOracleTests.cs'
        Search = 'Assert.IsTrue(diagnostic.Arguments.ContainsKey("callable"),'
        Replacement = 'Assert.DoesNotExist(diagnostic.Arguments.ContainsKey("callable"),'
        Occurrence = 1
        Project = 'src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj'
        Filter = 'FullyQualifiedName~DiagnosticContractOracleTests.UnknownCallable_HasOneStructuredBindRoot'
        IntendedAssertion = 'An invalid assertion API must be recorded as compilation-invalid, never as a killed mutant.'
    }
)

if ($ScopeId -eq 'REC-145') {
    $mutations += @(
        [ordered]@{
            Id = 'REC-145-M01'
            Name = 'enum-composite-name'
            Kind = 'critical'
            Layer = 'enum-execution-contract'
            File = 'src/dotnet/Musoq.Evaluator.Tests/Diagnostic.REC-102EnumIntrinsicTests.cs'
            Search = 'Assert.AreEqual("ReadWrite", table[0].Values[0]);'
            Replacement = 'Assert.AreEqual("Read", table[0].Values[0]);'
            Occurrence = 1
            Project = 'src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj'
            Filter = 'FullyQualifiedName~DiagnosticREC102EnumIntrinsicTests.CompositeNameControl_ShouldReturnOnlyTheDeclaredCompositeName'
            IntendedAssertion = 'The enum intrinsic challenger rejects a non-declared composite name.'
        }
        [ordered]@{
            Id = 'REC-145-M02'
            Name = 'warning-code'
            Kind = 'critical'
            Layer = 'warning-contract'
            File = 'src/dotnet/Musoq.Converter.Tests/Diagnostic.REC-116AdvisoryPrecisionTests.cs'
            Search = 'Assert.AreEqual(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, rootedWarning.Code);'
            Replacement = 'Assert.AreEqual(DiagnosticCode.MQ5015_SuspiciousRegexEscape, rootedWarning.Code);'
            Occurrence = 1
            Project = 'src/dotnet/Musoq.Converter.Tests/Musoq.Converter.Tests.csproj'
            Filter = 'FullyQualifiedName~DiagnosticREC116AdvisoryPrecisionTests.WarningLocations_ShouldIdentifyTheFirstHazardAndDeduplicatePerLiteral'
            IntendedAssertion = 'The warning challenger rejects a warning code from the wrong advisory family.'
        }
        [ordered]@{
            Id = 'REC-145-M03'
            Name = 'cancellation-token-identity'
            Kind = 'critical'
            Layer = 'cancellation-contract'
            File = 'src/dotnet/Musoq.Converter.Tests/CompileTimeCancellationCacheTests.cs'
            Search = 'Assert.AreEqual(cancellation.Token, exception.CancellationToken);'
            Replacement = 'Assert.AreEqual(CancellationToken.None, exception.CancellationToken);'
            Occurrence = 1
            Project = 'src/dotnet/Musoq.Converter.Tests/Musoq.Converter.Tests.csproj'
            Filter = 'FullyQualifiedName~CompileTimeCancellationCacheTests.SemanticCache_WhenOwnerCancelsBeforeFinalCommit_ShouldNotPublishOrBlockNextOwner'
            IntendedAssertion = 'The cancellation challenger rejects replacing the originating token with CancellationToken.None.'
        }
        [ordered]@{
            Id = 'REC-145-M04'
            Name = 'state-reuse-leakage'
            Kind = 'critical'
            Layer = 'diagnostic-state-isolation'
            File = 'src/dotnet/Musoq.Evaluator.Tests/Diagnostic.REC-125StateReuseTests.cs'
            Search = 'Assert.AreEqual(firstCandidate, secondCandidate, candidate.Id);'
            Replacement = 'Assert.AreNotEqual(firstCandidate, secondCandidate, candidate.Id);'
            Occurrence = 1
            Project = 'src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj'
            Filter = 'FullyQualifiedName~DiagnosticREC125StateReuseTests.EqualQueryTextAcrossSchemaFixtures_ShouldBeDeterministic'
            IntendedAssertion = 'The state-reuse challenger rejects diagnostic differences caused by schema fixture metadata.'
        }
    )
}

function Get-OccurrenceCount {
    param(
        [string] $Text,
        [string] $Search
    )

    $count = 0
    $offset = 0
    while ($true) {
        $index = $Text.IndexOf($Search, $offset, [StringComparison]::Ordinal)
        if ($index -lt 0) {
            return $count
        }

        $count++
        $offset = $index + $Search.Length
    }
}

function Replace-Occurrence {
    param(
        [string] $Text,
        [string] $Search,
        [string] $Replacement,
        [int] $Occurrence
    )

    $matches = [System.Text.RegularExpressions.Regex]::Matches(
        $Text,
        [System.Text.RegularExpressions.Regex]::Escape($Search))
    if ($matches.Count -eq 0) {
        throw "Mutation search text was not found: $Search"
    }

    if ($Occurrence -eq 0) {
        return $Text.Replace($Search, $Replacement, [StringComparison]::Ordinal)
    }

    if ($matches.Count -lt $Occurrence) {
        throw "Mutation requested occurrence $Occurrence but found only $($matches.Count): $Search"
    }

    $match = $matches[$Occurrence - 1]
    return $Text.Substring(0, $match.Index) + $Replacement + $Text.Substring($match.Index + $match.Length)
}

function Get-StringHash {
    param([string] $Value)

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    return ([Convert]::ToHexString($hash)).ToLowerInvariant()
}

function Get-BoundedText {
    param(
        [string] $Value,
        [int] $Maximum = 4000
    )

    if ($Value.Length -le $Maximum) {
        return $Value
    }

    return $Value.Substring(0, $Maximum) + "`n...[truncated]"
}

function Invoke-ExternalProcess {
    param(
        [string] $FileName,
        [string[]] $Arguments,
        [string] $WorkingDirectory,
        [int] $Timeout
    )

    $start = [DateTime]::UtcNow
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FileName
    $startInfo.WorkingDirectory = $WorkingDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in $Arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw "Could not start process '$FileName'."
        }

        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $finished = $process.WaitForExit($Timeout * 1000)
        $timedOut = -not $finished
        if ($timedOut) {
            try {
                if (-not $process.HasExited) {
                    $process.Kill($true)
                }
            }
            catch [System.InvalidOperationException] {
            }

            $process.WaitForExit()
        }

        $stdout = $stdoutTask.GetAwaiter().GetResult()
        $stderr = $stderrTask.GetAwaiter().GetResult()
        $exitCode = if ($timedOut) { $null } else { $process.ExitCode }
        return [pscustomobject][ordered]@{
            fileName = $FileName
            arguments = $Arguments
            exitCode = $exitCode
            timedOut = $timedOut
            startedAt = $start.ToString('O')
            finishedAt = [DateTime]::UtcNow.ToString('O')
            stdout = Get-BoundedText $stdout
            stderr = Get-BoundedText $stderr
        }
    }
    finally {
        $process.Dispose()
    }
}

function Test-CompilationFailure {
    param([string] $Output)

    return [System.Text.RegularExpressions.Regex]::IsMatch(
        $Output,
        '(?im)(^|\s)(?:error\s+CS\d+|CSC\s*:\s*error\s+CS\d+|Build FAILED\.)')
}

function Get-ObservedClassification {
    param(
        [string] $ExpectedKind,
        [pscustomobject] $ProcessResult
    )

    if ($ProcessResult.timedOut) {
        return 'timeout'
    }

    $output = $ProcessResult.stdout + "`n" + $ProcessResult.stderr
    if (Test-CompilationFailure $output) {
        return 'compilation_invalid'
    }

    if ($ExpectedKind -eq 'critical') {
        $classification = if ($ProcessResult.exitCode -eq 0) { 'survived' } else { 'killed' }
        return $classification
    }

    if ($ExpectedKind -eq 'equivalent') {
        $classification = if ($ProcessResult.exitCode -eq 0) { 'equivalent' } else { 'unexpected_failure' }
        return $classification
    }

    if ($ExpectedKind -eq 'compilation-invalid') {
        $classification = if ($ProcessResult.exitCode -eq 0) { 'survived' } else { 'unexpected_failure' }
        return $classification
    }

    throw "Unknown expected mutation kind: $ExpectedKind"
}

function Assert-ExpectedClassification {
    param(
        [string] $Name,
        [string] $ExpectedKind,
        [string] $Observed
    )

    $expected = switch ($ExpectedKind) {
        'critical' { 'killed' }
        'equivalent' { 'equivalent' }
        'compilation-invalid' { 'compilation_invalid' }
        'timeout' { 'timeout' }
        default { throw "Unknown expected classification kind: $ExpectedKind" }
    }

    if ($Observed -ne $expected) {
        throw "Mutation '$Name' expected '$expected' but observed '$Observed'."
    }
}

$baselineTargets = @($mutations | Where-Object Kind -eq 'critical' | ForEach-Object {
        "$($_.Project)|$($_.Filter)"
    } | Sort-Object -Unique)
$baselineResults = [System.Collections.Generic.List[object]]::new()
$mutationResults = [System.Collections.Generic.List[object]]::new()
$classificationProbe = $null
$worktreeCreated = $false

try {
    & git -C $repositoryRoot worktree add --detach --quiet $temporaryRoot HEAD
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not create the temporary diagnostic mutation worktree.'
    }

    $worktreeCreated = $true
    foreach ($target in $baselineTargets) {
        $parts = $target.Split('|', 2)
        $project = $parts[0]
        $filter = $parts[1]
        $processResult = Invoke-ExternalProcess `
            -FileName 'dotnet' `
            -Arguments @(
                'test',
                (Join-Path $temporaryRoot $project),
                '--configuration', 'Release',
                '--filter', $filter,
                '--nologo',
                '--verbosity', 'minimal',
                '--logger', 'console;verbosity=minimal') `
            -WorkingDirectory $temporaryRoot `
            -Timeout $TimeoutSeconds
        if ($processResult.timedOut -or $processResult.exitCode -ne 0) {
            throw "Baseline diagnostic test target failed or timed out: $filter"
        }

        $baselineResults.Add([pscustomobject][ordered]@{
            project = $project
            filter = $filter
            result = $processResult
        })
    }

    $probeExecutable = if ($PSEdition -eq 'Core') {
        Join-Path $PSHOME 'pwsh.exe'
    }
    else {
        Join-Path $PSHOME 'powershell.exe'
    }
    $classificationProbe = Invoke-ExternalProcess `
        -FileName $probeExecutable `
        -Arguments @('-NoProfile', '-NonInteractive', '-Command', 'Start-Sleep -Seconds 5') `
        -WorkingDirectory $repositoryRoot `
        -Timeout 1
    $probeClassification = if ($classificationProbe.timedOut) { 'timeout' } else { 'unexpected_failure' }
    $classificationProbe | Add-Member -NotePropertyName observedClassification -NotePropertyValue $probeClassification
    Assert-ExpectedClassification -Name 'timeout-classification-probe' -ExpectedKind 'timeout' -Observed $classificationProbe.observedClassification

    foreach ($mutation in $mutations) {
        & git -C $temporaryRoot reset --hard --quiet HEAD
        if ($LASTEXITCODE -ne 0) {
            throw "Could not reset the temporary worktree for mutation '$($mutation.Name)'."
        }

        $path = Join-Path $temporaryRoot $mutation.File
        $content = [System.IO.File]::ReadAllText($path)
        $occurrenceCount = Get-OccurrenceCount -Text $content -Search $mutation.Search
        $mutated = Replace-Occurrence `
            -Text $content `
            -Search $mutation.Search `
            -Replacement $mutation.Replacement `
            -Occurrence $mutation.Occurrence
        [System.IO.File]::WriteAllText($path, $mutated, [System.Text.UTF8Encoding]::new($false))

        $processResult = Invoke-ExternalProcess `
            -FileName 'dotnet' `
            -Arguments @(
                'test',
                (Join-Path $temporaryRoot $mutation.Project),
                '--configuration', 'Release',
                '--filter', $mutation.Filter,
                '--nologo',
                '--verbosity', 'minimal',
                '--logger', 'console;verbosity=minimal') `
            -WorkingDirectory $temporaryRoot `
            -Timeout $TimeoutSeconds
        $observed = Get-ObservedClassification -ExpectedKind $mutation.Kind -ProcessResult $processResult
        Assert-ExpectedClassification -Name $mutation.Name -ExpectedKind $mutation.Kind -Observed $observed

        $mutationResults.Add([pscustomobject][ordered]@{
            id = $mutation.Id
            name = $mutation.Name
            expectedKind = $mutation.Kind
            observedClassification = $observed
            layer = $mutation.Layer
            file = $mutation.File
            project = $mutation.Project
            filter = $mutation.Filter
            occurrence = $mutation.Occurrence
            occurrencesFound = $occurrenceCount
            searchSha256 = Get-StringHash $mutation.Search
            replacementSha256 = Get-StringHash $mutation.Replacement
            intendedAssertion = $mutation.IntendedAssertion
            process = $processResult
        })
    }
}
finally {
    if ($worktreeCreated) {
        & git -C $repositoryRoot worktree remove --force $temporaryRoot
    }
}

$summary = [ordered]@{
    critical = @($mutationResults | Where-Object expectedKind -eq 'critical').Count
    killed = @($mutationResults | Where-Object observedClassification -eq 'killed').Count
    survived = @($mutationResults | Where-Object observedClassification -eq 'survived').Count
    equivalent = @($mutationResults | Where-Object observedClassification -eq 'equivalent').Count
    compilationInvalid = @($mutationResults | Where-Object observedClassification -eq 'compilation_invalid').Count
    timeouts = @($mutationResults | Where-Object observedClassification -eq 'timeout').Count
}

if ($summary.critical -ne $summary.killed -or $summary.survived -ne 0 -or $summary.timeouts -ne 0) {
    throw "Diagnostic mutation gate did not satisfy the critical mutation gate: $($summary | ConvertTo-Json -Compress)"
}

$commit = (& git -C $repositoryRoot rev-parse HEAD).Trim()
$branch = (& git -C $repositoryRoot branch --show-current).Trim()
$sdk = (& dotnet --version).Trim()
$evidence = [ordered]@{
    formatVersion = 1
    scopeId = $ScopeId
    outcome = 'passed'
    repositoryIdentity = [ordered]@{
        root = $repositoryRoot
        commit = $commit
        branch = $branch
        configuration = 'Release'
        sdkObserved = $sdk
    }
    baseline = @($baselineResults)
    mutationMap = @($mutations | ForEach-Object {
        [ordered]@{
            id = $_.Id
            name = $_.Name
            expectedKind = $_.Kind
            layer = $_.Layer
            file = $_.File
            project = $_.Project
            filter = $_.Filter
            occurrence = $_.Occurrence
            searchSha256 = Get-StringHash $_.Search
            replacementSha256 = Get-StringHash $_.Replacement
            intendedAssertion = $_.IntendedAssertion
        }
    })
    results = @($mutationResults)
    classificationProbe = $classificationProbe
    summary = $summary
    controls = [ordered]@{
        baselineTargetsPassed = $baselineResults.Count
        unknownRuntimeLocationAcceptedByBaseline = $true
        compileFailuresExcludedFromKilled = $true
        timeoutExcludedFromKilled = $true
        catalogEnumerationClaimedAsQualityCoverage = $false
    }
    startedAt = $baselineResults[0].result.startedAt
    finishedAt = [DateTime]::UtcNow.ToString('O')
}

$parent = Split-Path $evidenceFile -Parent
[System.IO.Directory]::CreateDirectory($parent) | Out-Null
[System.IO.File]::WriteAllText(
    $evidenceFile,
    ($evidence | ConvertTo-Json -Depth 30),
    [System.Text.UTF8Encoding]::new($false))

Write-Host "$ScopeId diagnostic assertion mutation gate passed: $($summary.critical) critical/$($summary.killed) killed, $($summary.equivalent) equivalent, $($summary.compilationInvalid) compilation-invalid, $($summary.timeouts) timeouts."
