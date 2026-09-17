using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.QueryRows;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC105EnumMistakeRecoveryTests : GenericEntityTestBase
{
    private const string LocalEnumPrefix =
        "enum JobStatus : short { Queued = 10s, Running = 20s, Finished = 30s };" +
        "flags enum FileAccess : uint { None = 0ui, Read = 1ui, Write = 2ui, ReadWrite = 3ui };";

    private const string LocalDynamicContract =
        LocalEnumPrefix +
        "table EnumRows { Id: int, Status: JobStatus, Access: FileAccess };" +
        "couple #queryrowsample.rows with table EnumRows as Rows;";

    private const string NativeStatusTypeName = "Musoq.Evaluator.Tests.REC103NativeStatus";
    private const string NativeAccessTypeName = "Musoq.Evaluator.Tests.REC103NativeAccess";

    private static readonly REC105Entity[] Rows =
    [
        new(REC103NativeStatus.Running, REC103NativeAccess.Read | REC103NativeAccess.Write, 7, "Running")
    ];

    public static IEnumerable<object[]> CandidateCases()
    {
        foreach (var candidate in CreateCandidates())
            yield return [candidate];
    }

    [TestMethod]
    [DynamicData(nameof(CandidateCases))]
    public void EnumMistakeCandidates_ShouldHonorFrozenContract(REC105Candidate candidate)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.CaseId));

        switch (candidate.Kind)
        {
            case REC105CandidateKind.MetadataFailure:
                AssertMetadataFailure(candidate.Query, candidate.ExpectedCode!.Value);
                break;
            case REC105CandidateKind.MetadataValid:
                AssertMetadataSuccess(candidate.Query, candidate.ExpectedDisplayName!);
                break;
            case REC105CandidateKind.LocalSemanticFailure:
                AssertLocalSemanticFailure(candidate);
                break;
            case REC105CandidateKind.SemanticFailure:
                AssertSemanticFailure(candidate);
                break;
            default:
                Assert.Fail($"Unhandled candidate kind {candidate.Kind}.");
                break;
        }
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainFortyEightRegisteredCasesAcrossRecoveryFamilies()
    {
        var candidates = CreateCandidates().ToArray();

        Assert.HasCount(48, candidates);
        Assert.AreEqual(candidates.Length, candidates.Select(static candidate => candidate.CaseId)
            .Distinct(StringComparer.Ordinal).Count());
        Assert.HasCount(16, candidates.Where(static candidate => candidate.CaseId.Contains("-N", StringComparison.Ordinal)));
        Assert.HasCount(12, candidates.Where(static candidate => candidate.CaseId.Contains("-M", StringComparison.Ordinal)));
        Assert.HasCount(20, candidates.Where(static candidate => candidate.CaseId.Contains("-R", StringComparison.Ordinal)));
        Assert.HasCount(9, candidates.Where(static candidate => candidate.Kind == REC105CandidateKind.MetadataFailure));
        Assert.HasCount(7, candidates.Where(static candidate => candidate.Kind == REC105CandidateKind.MetadataValid));
        Assert.HasCount(2, candidates.Where(static candidate => candidate.Kind == REC105CandidateKind.LocalSemanticFailure));
        Assert.HasCount(30, candidates.Where(static candidate => candidate.Kind == REC105CandidateKind.SemanticFailure));
    }

    [TestMethod]
    public void LocalEnumNameControl_ShouldAcceptCaseInsensitiveTypesAndPreserveMetadata()
    {
        AssertMetadataSuccess(
            LocalTableContract("jobstatus", "fileaccess"),
            "JobStatus");
        AssertMetadataSuccess(
            LocalTableContract("JOBSTATUS", "FILEACCESS"),
            "JobStatus");
    }

    [TestMethod]
    public void NativeEnumNameControl_ShouldRequireExactQualifiedNames()
    {
        AssertMetadataSuccess(
            NativeTableContract(NativeStatusTypeName, "NATIVE"),
            NativeStatusTypeName);
        AssertMetadataSuccess(
            NativeTableContract(NativeAccessTypeName, "NATIVE"),
            NativeAccessTypeName);
    }

    [TestMethod]
    public void ExactEnumMemberControl_ShouldBindEqualityMembershipAndFlags()
    {
        using var equality = CreateAndRunVirtualMachine(
            "select e.Status from #schema.first() e where e.Status = 'Running'",
            Rows);
        using var membership = CreateAndRunVirtualMachine(
            "select e.Status from #schema.first() e where e.Status in ('Queued', 'Running')",
            Rows);
        using var flags = CreateAndRunVirtualMachine(
            "select HasAllFlags(e.Access, 'Read', 'Write') from #schema.first() e",
            Rows);

        Assert.HasCount(1, equality.Run(TestContext.CancellationToken));
        Assert.HasCount(1, membership.Run(TestContext.CancellationToken));
        Assert.AreEqual(true, flags.Run(TestContext.CancellationToken)[0].Values[0]);
    }

    [TestMethod]
    public void EnumRulesControl_ShouldKeepExplicitBridgesAndPermittedPartitionKeys()
    {
        using var bridges = CreateAndRunVirtualMachine(
            "select EnumValue(e.Status) as Value, EnumName(e.Status) as Name from #schema.first() e",
            Rows);
        using var partition = CreateAndRunVirtualMachine(
            "select RowNumber() over (partition by e.Status order by e.Number) as Position " +
            "from #schema.first() e",
            Rows);
        using var table = bridges.Run(TestContext.CancellationToken);
        using var partitionTable = partition.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.HasCount(1, partitionTable);
        Assert.AreEqual((short)20, table[0].Values[0]);
        Assert.AreEqual("Running", table[0].Values[1]);
    }

    private void AssertSemanticFailure(REC105Candidate candidate)
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(candidate.Query, Rows));

        AssertSingleError(exception, candidate.ExpectedCode!.Value, DiagnosticPhase.Bind);
        AssertHasGuidance(exception);
        Assert.IsFalse(
            exception.PrimaryEnvelope.Code is DiagnosticCode.MQ9001_InternalCompilerError or DiagnosticCode.MQ8001_CodeGenerationFailed,
            candidate.CaseId);

        if (string.IsNullOrWhiteSpace(candidate.ExpectedSpan))
            return;

        var expectedStart = candidate.Query.LastIndexOf(candidate.ExpectedSpan, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, expectedStart, candidate.CaseId);
        Assert.AreEqual(expectedStart, exception.PrimaryEnvelope.Offset, candidate.CaseId);
        Assert.AreEqual(candidate.ExpectedSpan.Length, exception.PrimaryEnvelope.Length, candidate.CaseId);
    }

    private void AssertLocalSemanticFailure(REC105Candidate candidate)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            candidate.Query,
            Guid.NewGuid().ToString(),
            new GeneratedQueryRowSampleSchemaProvider(
                GeneratedQueryRowSampleShape.Enum,
                queryScopedRowsEnabled: true),
            LoggerResolver,
            TestCompilationOptions);

        try
        {
            var errors = result.Errors.ToArray();
            Assert.IsFalse(result.Succeeded, FormatDiagnostics(result));
            Assert.HasCount(1, errors, FormatDiagnostics(result));
            Assert.AreEqual(candidate.ExpectedCode, errors[0].Code, FormatDiagnostics(result));
            Assert.AreEqual(DiagnosticPhase.Bind, errors[0].Phase, FormatDiagnostics(result));
            Assert.IsFalse(errors[0].Code is DiagnosticCode.MQ9001_InternalCompilerError or DiagnosticCode.MQ8001_CodeGenerationFailed);
        }
        finally
        {
            result.CompiledQuery?.Dispose();
        }
    }

    private void AssertMetadataFailure(string query, DiagnosticCode expectedCode)
    {
        var result = CompileMetadata(query);
        try
        {
            var errors = result.Errors.ToArray();
            Assert.IsFalse(result.Succeeded, FormatDiagnostics(result));
            Assert.HasCount(1, errors.Where(error => error.Code == expectedCode), FormatDiagnostics(result));
            Assert.IsTrue(
                errors.All(error => error.Code == expectedCode || error.Code == DiagnosticCode.MQ3001_UnknownColumn),
                FormatDiagnostics(result));
            Assert.AreEqual(
                expectedCode == DiagnosticCode.MQ2012_InvalidSchemaDefinition
                    ? DiagnosticPhase.Parse
                    : DiagnosticPhase.Bind,
                errors[0].Phase,
                FormatDiagnostics(result));
            Assert.IsFalse(errors[0].Code is DiagnosticCode.MQ9001_InternalCompilerError or DiagnosticCode.MQ8001_CodeGenerationFailed);
        }
        finally
        {
            result.CompiledQuery?.Dispose();
        }
    }

    private void AssertMetadataSuccess(string query, string expectedDisplayName)
    {
        var provider = new REC103MetadataSchemaProvider();
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions);

        try
        {
            Assert.IsTrue(result.Succeeded, FormatDiagnostics(result));
            Assert.IsTrue(
                provider.Schema.CapturedColumns
                    .SelectMany(static columns => columns)
                    .Any(column => column.EnumType?.DisplayName == expectedDisplayName),
                $"No enum descriptor '{expectedDisplayName}' was captured. {FormatDiagnostics(result)}");
        }
        finally
        {
            result.CompiledQuery?.Dispose();
        }
    }

    private BuildResult CompileMetadata(string query)
    {
        return InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new REC103MetadataSchemaProvider(),
            LoggerResolver,
            TestCompilationOptions);
    }

    private static string LocalTableContract(string statusType, string accessType)
    {
        return LocalEnumPrefix +
               $"table Jobs {{ Status: {statusType}, Access: {accessType} }};" +
               "couple #rec103.native with table Jobs as Jobs;select Status from Jobs()";
    }

    private static string NativeTableContract(string typeName, string schemaName)
    {
        return $"table Jobs {{ Status: {typeName} }};couple #rec103.{schemaName} with table Jobs as Jobs;select Status from Jobs()";
    }

    private static IEnumerable<REC105Candidate> CreateCandidates()
    {
        yield return Candidate("REC-105-N01", REC105CandidateKind.MetadataFailure,
            LocalTableContract("JobStatu", "FileAccess"), DiagnosticCode.MQ3005_TypeMismatch);
        yield return Candidate("REC-105-N02", REC105CandidateKind.MetadataFailure,
            "enum JobStates : short { Queued = 10s, Running = 20s, Finished = 30s };" +
            "table Jobs { Status: JobStatus };couple #rec103.native with table Jobs as Jobs;select Status from Jobs()",
            DiagnosticCode.MQ3005_TypeMismatch);
        yield return Candidate("REC-105-N03", REC105CandidateKind.MetadataFailure,
            "table Jobs { Status: JobStatus };" + LocalEnumPrefix +
            "couple #rec103.native with table Jobs as Jobs;select Status from Jobs()",
            DiagnosticCode.MQ3107_UnknownEnumType);
        yield return Candidate("REC-105-N04", REC105CandidateKind.MetadataValid,
            LocalTableContract("jobstatus", "FileAccess"), expectedDisplayName: "JobStatus");
        yield return Candidate("REC-105-N05", REC105CandidateKind.MetadataValid,
            LocalTableContract("JOBSTATUS", "FILEACCESS"), expectedDisplayName: "JobStatus");
        yield return Candidate("REC-105-N06", REC105CandidateKind.MetadataFailure,
            LocalTableContract("JobStatu?", "FileAccess"), DiagnosticCode.MQ3005_TypeMismatch);
        yield return Candidate("REC-105-N07", REC105CandidateKind.MetadataFailure,
            LocalTableContract("JobStatus", "FileAcces"), DiagnosticCode.MQ3005_TypeMismatch);
        yield return Candidate("REC-105-N08", REC105CandidateKind.MetadataValid,
            LocalTableContract("JobStatus", "fileaccess"), expectedDisplayName: "JobStatus");
        yield return Candidate("REC-105-N09", REC105CandidateKind.MetadataFailure,
            NativeTableContract("REC103NativeStatus", "native"), DiagnosticCode.MQ3005_TypeMismatch);
        yield return Candidate("REC-105-N10", REC105CandidateKind.MetadataFailure,
            NativeTableContract("Musoq.Evaluator.Tests.REC103NativeStatu", "native"), DiagnosticCode.MQ3005_TypeMismatch);
        yield return Candidate("REC-105-N11", REC105CandidateKind.MetadataFailure,
            NativeTableContract("musoq.Evaluator.Tests.REC103NativeStatus", "native"), DiagnosticCode.MQ3005_TypeMismatch);
        yield return Candidate("REC-105-N12", REC105CandidateKind.MetadataValid,
            NativeTableContract(NativeStatusTypeName, "NATIVE"), expectedDisplayName: NativeStatusTypeName);
        yield return Candidate("REC-105-N13", REC105CandidateKind.MetadataValid,
            $"table Jobs {{ Status: {NativeStatusTypeName}? }};couple #rec103.native with table Jobs as Jobs;select Status from Jobs()",
            expectedDisplayName: NativeStatusTypeName);
        yield return Candidate("REC-105-N14", REC105CandidateKind.MetadataFailure,
            NativeTableContract("Musoq.Evaluator.Tests.REC103NativeAcces", "native"), DiagnosticCode.MQ3005_TypeMismatch);
        yield return Candidate("REC-105-N15", REC105CandidateKind.MetadataValid,
            NativeTableContract(NativeAccessTypeName, "NATIVE"), expectedDisplayName: NativeAccessTypeName);
        yield return Candidate("REC-105-N16", REC105CandidateKind.MetadataValid,
            LocalTableContract("jobstatus?", "FileAccess"), expectedDisplayName: "JobStatus");

        yield return Candidate("REC-105-M01", REC105CandidateKind.SemanticFailure,
            "select e.Status from #schema.first() e where e.Status = 'Runnning'",
            DiagnosticCode.MQ3108_UnknownEnumMember, "'Runnning'");
        yield return Candidate("REC-105-M02", REC105CandidateKind.SemanticFailure,
            "select e.Status from #schema.first() e where e.Status <> 'running'",
            DiagnosticCode.MQ3108_UnknownEnumMember, "'running'");
        yield return Candidate("REC-105-M03", REC105CandidateKind.SemanticFailure,
            "select e.Status from #schema.first() e where 'RUNNING' = e.Status",
            DiagnosticCode.MQ3108_UnknownEnumMember, "'RUNNING'");
        yield return Candidate("REC-105-M04", REC105CandidateKind.SemanticFailure,
            "select e.Status from #schema.first() e where e.Status is distinct from 'queued'",
            DiagnosticCode.MQ3108_UnknownEnumMember, "'queued'");
        yield return Candidate("REC-105-M05", REC105CandidateKind.SemanticFailure,
            "select e.Status from #schema.first() e where e.Status is not distinct from 'FINISHED'",
            DiagnosticCode.MQ3108_UnknownEnumMember, "'FINISHED'");
        yield return Candidate("REC-105-M06", REC105CandidateKind.SemanticFailure,
            "select e.Status from #schema.first() e where e.Status in ('Queued', 'Missing')",
            DiagnosticCode.MQ3108_UnknownEnumMember, "'Missing'");
        yield return Candidate("REC-105-M07", REC105CandidateKind.SemanticFailure,
            "select e.Status from #schema.first() e where e.Status not in ('Queued', 'finishd')",
            DiagnosticCode.MQ3108_UnknownEnumMember, "'finishd'");
        yield return Candidate("REC-105-M08", REC105CandidateKind.SemanticFailure,
            "select case when true then e.Status else 'queueD' end as Value from #schema.first() e",
            DiagnosticCode.MQ3108_UnknownEnumMember, "'queueD'");
        yield return Candidate("REC-105-M09", REC105CandidateKind.SemanticFailure,
            "select e.Access from #schema.first() e where e.Access = 'Readwrite'",
            DiagnosticCode.MQ3108_UnknownEnumMember, "'Readwrite'");
        yield return Candidate("REC-105-M10", REC105CandidateKind.SemanticFailure,
            "select e.Access from #schema.first() e where e.Access <> 'write'",
            DiagnosticCode.MQ3108_UnknownEnumMember, "'write'");
        yield return Candidate("REC-105-M11", REC105CandidateKind.LocalSemanticFailure,
            LocalDynamicContract + "select Status from Rows() where Status = 'running'",
            DiagnosticCode.MQ3108_UnknownEnumMember, "'running'");
        yield return Candidate("REC-105-M12", REC105CandidateKind.LocalSemanticFailure,
            LocalDynamicContract + "select Status from Rows() where Status in ('Queued', 'Paused')",
            DiagnosticCode.MQ3108_UnknownEnumMember, "'Paused'");

        yield return Candidate("REC-105-R01", REC105CandidateKind.SemanticFailure,
            "select 1 from #schema.first() e where e.Status >= 'Running'",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R02", REC105CandidateKind.SemanticFailure,
            "select 1 from #schema.first() e where e.Status < 'Running'",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R03", REC105CandidateKind.SemanticFailure,
            "select 1 from #schema.first() e where e.Status <= 'Running'",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R04", REC105CandidateKind.SemanticFailure,
            "select e.Status - 1 as Value from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R05", REC105CandidateKind.SemanticFailure,
            "select e.Status * 2 as Value from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R06", REC105CandidateKind.SemanticFailure,
            "select e.Status / 2 as Value from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R07", REC105CandidateKind.SemanticFailure,
            "select e.Status % 2 as Value from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R08", REC105CandidateKind.SemanticFailure,
            "select e.Access & 1 as Value from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R09", REC105CandidateKind.SemanticFailure,
            "select e.Access | 1 as Value from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R10", REC105CandidateKind.SemanticFailure,
            "select e.Access ^ 1 as Value from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R11", REC105CandidateKind.SemanticFailure,
            "select e.Access << 1 as Value from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R12", REC105CandidateKind.SemanticFailure,
            "select e.Access >> 1 as Value from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R13", REC105CandidateKind.SemanticFailure,
            "select Sum(e.Status) as Value from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R14", REC105CandidateKind.SemanticFailure,
            "select Avg(e.Status) as Value from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R15", REC105CandidateKind.SemanticFailure,
            "select Min(e.Status) as Value from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R16", REC105CandidateKind.SemanticFailure,
            "select Max(e.Status) as Value from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R17", REC105CandidateKind.SemanticFailure,
            "select 1 from #schema.first() e where e.Status rlike 'R.*'",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R18", REC105CandidateKind.SemanticFailure,
            "select Lag(e.Status) over (order by e.Number) from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R19", REC105CandidateKind.SemanticFailure,
            "select RowNumber() over (order by e.Status) from #schema.first() e",
            DiagnosticCode.MQ3110_UnsupportedEnumOperator);
        yield return Candidate("REC-105-R20", REC105CandidateKind.SemanticFailure,
            "param(states: int[]) select e.Status from #schema.first() e where e.Status in $states",
            DiagnosticCode.MQ3112_UnsupportedEnumScriptParameter);
    }

    private static REC105Candidate Candidate(
        string caseId,
        REC105CandidateKind kind,
        string query,
        DiagnosticCode? expectedCode = null,
        string? expectedSpan = null,
        string? expectedDisplayName = null)
    {
        return new REC105Candidate(
            caseId,
            kind,
            query + $" /* {caseId} */",
            expectedCode,
            expectedSpan,
            expectedDisplayName);
    }

    private static string FormatDiagnostics(BuildResult result)
    {
        return string.Join(
            Environment.NewLine,
            result.Diagnostics.Select(static diagnostic =>
                $"{diagnostic.Code} [{diagnostic.Phase}] {diagnostic.Message}"));
    }
}

public sealed record REC105Candidate(
    string CaseId,
    REC105CandidateKind Kind,
    string Query,
    DiagnosticCode? ExpectedCode = null,
    string? ExpectedSpan = null,
    string? ExpectedDisplayName = null);

public enum REC105CandidateKind
{
    MetadataFailure,
    MetadataValid,
    LocalSemanticFailure,
    SemanticFailure
}

public sealed record REC105Entity(
    REC103NativeStatus Status,
    REC103NativeAccess Access,
    int Number,
    string Label);
