param(
    [switch] $ConfirmReleaseRun
)

$ErrorActionPreference = 'Stop'

if (-not $ConfirmReleaseRun) {
    throw 'This release-only gate creates and mutates a temporary git worktree. Pass -ConfirmReleaseRun intentionally.'
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('musoq-recursive-mutations-' + [Guid]::NewGuid().ToString('N'))
$testProject = 'src/dotnet/Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj'

$mutations = @(
    @{
        Name = 'recursive-member-restriction'
        File = 'src/dotnet/Musoq.Evaluator/Visitors/RecursiveCteShapeAnalyzer.cs'
        Search = 'if (member.Select.IsDistinct)'
        Replacement = 'if (false && member.Select.IsDistinct)'
        Occurrence = 1
        Filter = 'FullyQualifiedName~RecursiveCteDiagnosticCatalogTests.UnsupportedCase_ShouldStopBeforePlanningWithDeclaredDiagnostic'
    },
    @{
        Name = 'keyed-identity-ordinal'
        File = 'src/dotnet/Musoq.Evaluator/IR/Logical/LogicalQueryPlanBuilder.cs'
        Search = 'indexes[keyIndex] = fieldIndex;'
        Replacement = 'indexes[keyIndex] = 0;'
        Occurrence = 1
        Filter = 'FullyQualifiedName~CteTests.RecursiveUnionAllSupportedCase_ShouldReturnDeclaredColumnsAndRows'
    },
    @{
        Name = 'iteration-limit-guard'
        File = 'src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.RecursiveCte.Execution.cs'
        Search = 'DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded'
        Replacement = 'DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded'
        Occurrence = 1
        Filter = 'FullyQualifiedName~CteTests.RecursiveUnionAll_WhenIterationLimitIsReached_ShouldReportMq7007'
    },
    @{
        Name = 'row-limit-guard'
        File = 'src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.RecursiveCte.Append.cs'
        Search = 'yield return CreateRecursiveRowLimitGuard(append);'
        Replacement = '// row-limit guard removed by release mutation gate'
        Occurrence = 0
        Filter = 'FullyQualifiedName~CteTests.RecursiveUnionAll_WhenRowLimitIsReached_ShouldReportMq7008'
    },
    @{
        Name = 'snapshot-limit-guard'
        File = 'src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.RecursiveCte.Execution.cs'
        Search = 'guard.MaxRows,'
        Replacement = 'int.MaxValue,'
        Occurrence = 1
        Filter = 'FullyQualifiedName~RecursiveCteJoinExecutionTests.RecursiveInvariantSource_WhenSnapshotLimitIsReached_ShouldReportMq7009AndDispose'
    },
    @{
        Name = 'empty-anchor-snapshot-laziness'
        File = 'src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.RecursiveCte.Execution.cs'
        Search = 'SyntaxKind.GreaterThanExpression'
        Replacement = 'SyntaxKind.GreaterThanOrEqualExpression'
        Occurrence = 2
        Filter = 'FullyQualifiedName~RecursiveCteJoinExecutionTests.RecursiveInvariantSource_WhenAnchorIsEmpty_ShouldNotBeOpened'
    }
)

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

git -C $repositoryRoot worktree add --detach $temporaryRoot HEAD
if ($LASTEXITCODE -ne 0) {
    throw 'Could not create the temporary mutation worktree.'
}

$survivors = [System.Collections.Generic.List[string]]::new()
try {
    foreach ($mutation in $mutations) {
        git -C $temporaryRoot reset --hard HEAD | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Could not reset the temporary worktree for mutation '$($mutation.Name)'."
        }

        $path = Join-Path $temporaryRoot $mutation.File
        $content = [System.IO.File]::ReadAllText($path)
        $mutated = Replace-Occurrence `
            -Text $content `
            -Search $mutation.Search `
            -Replacement $mutation.Replacement `
            -Occurrence $mutation.Occurrence
        [System.IO.File]::WriteAllText($path, $mutated, [System.Text.UTF8Encoding]::new($false))

        dotnet test (Join-Path $temporaryRoot $testProject) `
            --configuration Release `
            --filter $mutation.Filter `
            --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -eq 0) {
            $survivors.Add($mutation.Name)
        }
    }
}
finally {
    git -C $repositoryRoot worktree remove --force $temporaryRoot
}

if ($survivors.Count -gt 0) {
    throw "Recursive CTE mutations survived: $($survivors -join ', ')"
}

Write-Host "All $($mutations.Count) recursive CTE mutations were detected."
