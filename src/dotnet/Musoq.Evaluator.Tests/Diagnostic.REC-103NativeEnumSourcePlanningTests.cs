using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.QueryRows;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC103NativeEnumSourcePlanningTests : GenericEntityTestBase
{
    private const string NativeStatusTypeName =
        "Musoq.Evaluator.Tests.REC103NativeStatus";

    private const string NativeAccessTypeName =
        "Musoq.Evaluator.Tests.REC103NativeAccess";

    private const string DynamicEnumPrefix =
        "enum JobStatus : short { Queued = 10s, Running = 20s, Finished = 30s };" +
        "flags enum FileAccess : uint { None = 0ui, Read = 1ui, Write = 2ui, ReadWrite = 3ui };" +
        "table EnumRows { Id: int, Status: JobStatus, Access: FileAccess };" +
        "couple #queryrowsample.rows with table EnumRows as Rows;";

    private static readonly REC103NativeEntity[] KnownRows =
    [
        new(
            REC103NativeStatus.Running,
            REC103NativeAccess.Read | REC103NativeAccess.Write,
            REC103NativeStatus.Queued,
            REC103NativeAccess.Read,
            7)
    ];

    private static readonly REC103NativeEntity[] OptionalNullRows =
    [
        new(
            REC103NativeStatus.Running,
            REC103NativeAccess.None,
            null,
            null,
            7)
    ];

    private static readonly REC103NativeEntity[] UnknownRows =
    [
        new(
            (REC103NativeStatus)99,
            (REC103NativeAccess)8,
            null,
            null,
            99)
    ];

    private static readonly REC103NativeEntity[] MatchingRows =
    [
        new(
            REC103NativeStatus.Running,
            REC103NativeAccess.Write,
            null,
            null,
            8)
    ];

    public static IEnumerable<object[]> CandidateCases()
    {
        yield return [new REC103Candidate(
            "REC-103-N01",
            REC103CandidateKind.NativeStatus,
            "select e.Status as Status from #schema.first() e")];
        yield return [new REC103Candidate(
            "REC-103-N02",
            REC103CandidateKind.NativeAccess,
            "select e.Access as Access from #schema.first() e")];
        yield return [new REC103Candidate(
            "REC-103-N03",
            REC103CandidateKind.NativeNullable,
            "select e.OptionalStatus as OptionalStatus from #schema.first() e")];
        yield return [new REC103Candidate(
            "REC-103-N04",
            REC103CandidateKind.NativeUnknown,
            "select e.Status as Status, EnumName(e.Status) as StatusName, IsDefined(e.Status) as StatusDefined from #schema.first() e")];
        yield return [new REC103Candidate(
            "REC-103-N05",
            REC103CandidateKind.NativeUnknownFlags,
            "select EnumValue(e.Access) as AccessValue, EnumName(e.Access) as AccessName, IsDefined(e.Access) as AccessDefined, HasAnyFlags(e.Access, 'Read') as HasRead, HasAllFlags(e.Access, 'Read') as HasAllRead from #schema.first() e")];
        yield return [new REC103Candidate(
            "REC-103-N06",
            REC103CandidateKind.NativeCte,
            "with states as (select e.Status as Status from #schema.first() e) select Status from states")];
        yield return [new REC103Candidate(
            "REC-103-N07",
            REC103CandidateKind.NativeJoin,
            "select a.Status as Status from #schema.first() a inner join #schema.second() b on a.Status = b.Status")];
        yield return [new REC103Candidate(
            "REC-103-N08",
            REC103CandidateKind.NativeMixed,
            "select e.Status as Status, e.Number as Number from #schema.first() e")];

        yield return [new REC103Candidate(
            "REC-103-T01",
            REC103CandidateKind.MetadataNativeStatus,
            $"table Jobs {{ Status: {NativeStatusTypeName} }};couple #rec103.native with table Jobs as Jobs;select Status from Jobs()")];
        yield return [new REC103Candidate(
            "REC-103-T02",
            REC103CandidateKind.MetadataNativeNullable,
            $"table Jobs {{ Status: {NativeStatusTypeName}? }};couple #rec103.native with table Jobs as Jobs;select Status from Jobs()")];
        yield return [new REC103Candidate(
            "REC-103-T03",
            REC103CandidateKind.MetadataNativeFlags,
            $"table Jobs {{ Access: {NativeAccessTypeName} }};couple #rec103.native with table Jobs as Jobs;select Access from Jobs()")];
        yield return [new REC103Candidate(
            "REC-103-T04",
            REC103CandidateKind.MetadataQueryLocal,
            "enum JobStatus : short { Queued = 10s, Running = 20s };table Jobs { Status: jobstatus };couple #rec103.native with table Jobs as Jobs;select Status from Jobs()")];
        yield return [new REC103Candidate(
            "REC-103-T05",
            REC103CandidateKind.MetadataSimpleNativeName,
            "table Jobs { Status: REC103NativeStatus };couple #rec103.native with table Jobs as Jobs;select Status from Jobs()",
            DiagnosticCode.MQ3005_TypeMismatch)];
        yield return [new REC103Candidate(
            "REC-103-T06",
            REC103CandidateKind.MetadataUnknownNativeName,
            "table Jobs { Status: Musoq.Evaluator.Tests.MissingREC103Status };couple #rec103.native with table Jobs as Jobs;select Status from Jobs()",
            DiagnosticCode.MQ3005_TypeMismatch)];
        yield return [new REC103Candidate(
            "REC-103-T07",
            REC103CandidateKind.MetadataDuplicateModifier,
            $"table Jobs {{ Status: {NativeStatusTypeName} encoding 'utf-8' encoding 'windows-1250' }};couple #rec103.native with table Jobs as Jobs;select Status from Jobs()",
            DiagnosticCode.MQ2012_InvalidSchemaDefinition)];
        yield return [new REC103Candidate(
            "REC-103-T08",
            REC103CandidateKind.MetadataModifierSeparation,
            $"table Jobs {{ Status: {NativeStatusTypeName} encoding 'utf-8' }};couple #rec103.native with table Jobs as Jobs;select Status from Jobs()")];

        yield return [new REC103Candidate(
            "REC-103-D01",
            REC103CandidateKind.DynamicStatus,
            DynamicEnumPrefix + "select Status as Status from Rows()")];
        yield return [new REC103Candidate(
            "REC-103-D02",
            REC103CandidateKind.DynamicAccess,
            DynamicEnumPrefix + "select Access as Access, EnumName(Access) as AccessName, HasAllFlags(Access, 'Read', 'Write') as CanWrite from Rows()")];
        yield return [new REC103Candidate(
            "REC-103-D03",
            REC103CandidateKind.DynamicNullableCarrier,
            DynamicEnumPrefix + "select Status as Status, Access as Access from Rows()")];
        yield return [new REC103Candidate(
            "REC-103-D04",
            REC103CandidateKind.DynamicCaseInsensitiveType,
            "enum JobStatus : short { Queued = 10s, Running = 20s };flags enum FileAccess : uint { None = 0ui, Read = 1ui, Write = 2ui };table EnumRows { Id: int, Status: jobstatus, Access: FileAccess };couple #queryrowsample.rows with table EnumRows as Rows;select Status from Rows()")];
        yield return [new REC103Candidate(
            "REC-103-D05",
            REC103CandidateKind.DynamicCte,
            DynamicEnumPrefix + "with states as (select Status as Status from Rows()) select Status from states")];
        yield return [new REC103Candidate(
            "REC-103-D06",
            REC103CandidateKind.DynamicFilter,
            DynamicEnumPrefix + "select Status from Rows() where Status in ('Queued', 'Running')")];
        yield return [new REC103Candidate(
            "REC-103-D07",
            REC103CandidateKind.DynamicIntrinsics,
            DynamicEnumPrefix + "select EnumValue(Status) as StatusValue, EnumName(Status) as StatusName, IsDefined(Status) as StatusDefined, HasAnyFlags(Access, 'Read') as HasRead from Rows()")];
        yield return [new REC103Candidate(
            "REC-103-D08",
            REC103CandidateKind.DynamicCapabilityFailure,
            DynamicEnumPrefix + "select Status as Status from Rows()",
            DiagnosticCode.MQ3114_EnumSourceCapabilityRequired)];
    }

    [TestMethod]
    [DynamicData(nameof(CandidateCases))]
    public void NativeAndDynamicSourceCandidates_ShouldHonorFrozenContract(REC103Candidate candidate)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.CaseId));

        switch (candidate.Kind)
        {
            case REC103CandidateKind.NativeStatus:
                AssertNativeStatusProjection(candidate.Query, KnownRows);
                break;
            case REC103CandidateKind.NativeAccess:
                AssertNativeAccessProjection(candidate.Query, KnownRows);
                break;
            case REC103CandidateKind.NativeNullable:
                AssertNativeNullableProjection(candidate.Query);
                break;
            case REC103CandidateKind.NativeUnknown:
                AssertNativeUnknownProjection(candidate.Query);
                break;
            case REC103CandidateKind.NativeUnknownFlags:
                AssertNativeUnknownFlagsProjection(candidate.Query);
                break;
            case REC103CandidateKind.NativeCte:
                AssertNativeCteProjection(candidate.Query);
                break;
            case REC103CandidateKind.NativeJoin:
                AssertNativeJoinProjection(candidate.Query);
                break;
            case REC103CandidateKind.NativeMixed:
                AssertNativeMixedProjection(candidate.Query);
                break;
            case REC103CandidateKind.MetadataNativeStatus:
                AssertNativeMetadata(candidate.Query, "Status", typeof(REC103NativeStatus?), EnumTypeOrigin.NativeClr);
                break;
            case REC103CandidateKind.MetadataNativeNullable:
                AssertNativeMetadata(candidate.Query, "Status", typeof(REC103NativeStatus?), EnumTypeOrigin.NativeClr);
                break;
            case REC103CandidateKind.MetadataNativeFlags:
                AssertNativeMetadata(candidate.Query, "Access", typeof(REC103NativeAccess?), EnumTypeOrigin.NativeClr);
                break;
            case REC103CandidateKind.MetadataQueryLocal:
                AssertQueryLocalMetadata(candidate.Query);
                break;
            case REC103CandidateKind.MetadataSimpleNativeName:
            case REC103CandidateKind.MetadataUnknownNativeName:
            case REC103CandidateKind.MetadataDuplicateModifier:
                AssertMetadataFailure(candidate.Query, candidate.ExpectedCode!.Value);
                break;
            case REC103CandidateKind.MetadataModifierSeparation:
                AssertModifierSeparation(candidate.Query);
                break;
            case REC103CandidateKind.DynamicStatus:
                AssertDynamicStatus(candidate.Query);
                break;
            case REC103CandidateKind.DynamicAccess:
                AssertDynamicAccess(candidate.Query);
                break;
            case REC103CandidateKind.DynamicNullableCarrier:
                AssertDynamicNullableCarrier(candidate.Query);
                break;
            case REC103CandidateKind.DynamicCaseInsensitiveType:
                AssertDynamicCaseInsensitiveType(candidate.Query);
                break;
            case REC103CandidateKind.DynamicCte:
                AssertDynamicCte(candidate.Query);
                break;
            case REC103CandidateKind.DynamicFilter:
                AssertDynamicFilter(candidate.Query);
                break;
            case REC103CandidateKind.DynamicIntrinsics:
                AssertDynamicIntrinsics(candidate.Query);
                break;
            case REC103CandidateKind.DynamicCapabilityFailure:
                AssertMetadataFailure(candidate.Query, candidate.ExpectedCode!.Value, queryScopedRowsEnabled: false);
                break;
            default:
                Assert.Fail($"Unhandled candidate kind {candidate.Kind}.");
                break;
        }
    }

    [TestMethod]
    public void NativeSourceBoundary_Control_ShouldExposePrimitiveCarrierAndNativeDescriptor()
    {
        var provider = CompileMetadata(
            $"table Jobs {{ Status: {NativeStatusTypeName} }};" +
            "couple #rec103.native with table Jobs as Jobs;select Status from Jobs()");
        var column = FindCapturedColumn(provider, "Status");

        Assert.AreEqual(typeof(short?), column.ColumnType);
        Assert.AreEqual(typeof(REC103NativeStatus?), column.SourceReadType);
        AssertNativeDescriptor(column, typeof(REC103NativeStatus), EnumTypeOrigin.NativeClr);
        Assert.AreEqual(Enum.GetNames<REC103NativeStatus>().Length, column.EnumType!.Members.Count);
        Assert.IsFalse(provider.Schema.RowSourceRequested);
    }

    [TestMethod]
    public void DynamicSourceBoundary_Control_ShouldUsePrimitiveQueryRowsAndFrozenQueryLocalDescriptor()
    {
        using var compiled = CompileDynamic(DynamicEnumPrefix +
            "select Status as Status, Access as Access from Rows()");
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(new object?[] { (short)20, 3u }, table[0].Values);
        Assert.IsFalse(table.SelectMany(static row => row.Values).Any(static value => value is Enum));

        var status = table.Columns.Single(static column => column.ColumnName == "Status");
        var access = table.Columns.Single(static column => column.ColumnName == "Access");
        Assert.AreEqual(typeof(short?), status.ColumnType);
        Assert.AreEqual(typeof(short?), status.SourceReadType);
        Assert.AreEqual(typeof(uint?), access.ColumnType);
        Assert.AreEqual(typeof(uint?), access.SourceReadType);
        AssertQueryLocalDescriptor(status, "JobStatus", EnumUnderlyingKind.Int16, isFlags: false);
        AssertQueryLocalDescriptor(access, "FileAccess", EnumUnderlyingKind.UInt32, isFlags: true);
    }

    [TestMethod]
    public void CapabilityDiagnostic_Control_ShouldRejectDynamicEnumWithoutLogicalScalarReads()
    {
        var result = CompileWithDiagnostics(
            DynamicEnumPrefix + "select Status as Status from Rows()",
            queryScopedRowsEnabled: false);

        Assert.IsFalse(result.Succeeded);
        var diagnostic = result.Errors.Single(static item => item.Code == DiagnosticCode.MQ3114_EnumSourceCapabilityRequired);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.Contains("logical scalar reads required by enum column 'Status'", diagnostic.Message);
        Assert.IsFalse(diagnostic.Message.Contains("object", StringComparison.OrdinalIgnoreCase));
        result.CompiledQuery?.Dispose();
    }

    [TestMethod]
    public void MetadataAndModifier_Control_ShouldKeepEnumIdentitySeparateFromReadModifiers()
    {
        var provider = CompileMetadata(
            $"table Jobs {{ Status: {NativeStatusTypeName} encoding 'utf-8' }};" +
            "couple #rec103.native with table Jobs as Jobs;select Status from Jobs()");
        var column = FindCapturedColumn(provider, "Status");

        Assert.AreEqual("utf-8", column.ReadModifiers[ColumnReadModifiers.Encoding]);
        AssertNativeDescriptor(column, typeof(REC103NativeStatus), EnumTypeOrigin.NativeClr);
        Assert.IsFalse(column.EnumType!.Members.Any(static member =>
            member.Name.Equals("utf-8", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(provider.Schema.RowSourceRequested);
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainTwentyFourRegisteredCasesAcrossNativeTableAndDynamicFamilies()
    {
        var cases = CandidateCases()
            .Select(static values => (REC103Candidate)values[0])
            .ToArray();

        Assert.HasCount(24, cases);
        Assert.AreEqual(cases.Length, cases.Select(static candidate => candidate.CaseId)
            .Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(8, cases.Count(static candidate => candidate.CaseId.Contains("-N", StringComparison.Ordinal)));
        Assert.AreEqual(8, cases.Count(static candidate => candidate.CaseId.Contains("-T", StringComparison.Ordinal)));
        Assert.AreEqual(8, cases.Count(static candidate => candidate.CaseId.Contains("-D", StringComparison.Ordinal)));
        Assert.HasCount(4, cases.Where(static candidate => candidate.ExpectedCode.HasValue));
    }

    private void AssertNativeStatusProjection(string query, REC103NativeEntity[] rows)
    {
        using var compiled = CreateAndRunVirtualMachine(query, rows);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual((short)20, table[0].Values[0]);
        AssertNativeOutputColumn(table, "Status", typeof(short), typeof(REC103NativeStatus));
    }

    private void AssertNativeAccessProjection(string query, REC103NativeEntity[] rows)
    {
        using var compiled = CreateAndRunVirtualMachine(query, rows);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual(3u, table[0].Values[0]);
        AssertNativeOutputColumn(table, "Access", typeof(uint), typeof(REC103NativeAccess));
    }

    private void AssertNativeNullableProjection(string query)
    {
        using var compiled = CreateAndRunVirtualMachine(query, OptionalNullRows);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.IsNull(table[0].Values[0]);
        AssertNativeOutputColumn(table, "OptionalStatus", typeof(short?), typeof(REC103NativeStatus));
    }

    private void AssertNativeUnknownProjection(string query)
    {
        using var compiled = CreateAndRunVirtualMachine(query, UnknownRows);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(new object?[] { (short)99, null, false }, table[0].Values);
        AssertNativeOutputColumn(table, "Status", typeof(short), typeof(REC103NativeStatus));
    }

    private void AssertNativeUnknownFlagsProjection(string query)
    {
        using var compiled = CreateAndRunVirtualMachine(query, UnknownRows);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(new object?[] { 8u, null, false, false, false }, table[0].Values);
    }

    private void AssertNativeCteProjection(string query)
    {
        using var compiled = CreateAndRunVirtualMachine(query, KnownRows);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual((short)20, table[0].Values[0]);
        AssertNativeOutputColumn(table, "Status", typeof(short), typeof(REC103NativeStatus));
    }

    private void AssertNativeJoinProjection(string query)
    {
        using var compiled = CreateAndRunVirtualMachine(query, KnownRows, MatchingRows);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual((short)20, table[0].Values[0]);
        Assert.IsNotNull(table.Columns.Single().EnumType);
    }

    private void AssertNativeMixedProjection(string query)
    {
        using var compiled = CreateAndRunVirtualMachine(query, KnownRows);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(new object?[] { (short)20, 7 }, table[0].Values);
        AssertNativeOutputColumn(table, "Status", typeof(short), typeof(REC103NativeStatus));
        Assert.IsNull(table.Columns.Single(static column => column.ColumnName == "Number").EnumType);
    }

    private void AssertDynamicStatus(string query)
    {
        using var compiled = CompileDynamic(query);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual((short)20, table[0].Values[0]);
        AssertQueryLocalOutputColumn(table, "Status", typeof(short?), "JobStatus");
    }

    private void AssertDynamicAccess(string query)
    {
        using var compiled = CompileDynamic(query);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(new object?[] { 3u, "ReadWrite", true }, table[0].Values);
        AssertQueryLocalOutputColumn(table, "Access", typeof(uint?), "FileAccess");
    }

    private void AssertDynamicNullableCarrier(string query)
    {
        using var compiled = CompileDynamic(query);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(new object?[] { (short)20, 3u }, table[0].Values);
        AssertQueryLocalOutputColumn(table, "Status", typeof(short?), "JobStatus");
        AssertQueryLocalOutputColumn(table, "Access", typeof(uint?), "FileAccess");
    }

    private void AssertDynamicCaseInsensitiveType(string query)
    {
        using var compiled = CompileDynamic(query);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual((short)20, table[0].Values[0]);
        AssertQueryLocalOutputColumn(table, "Status", typeof(short?), "JobStatus");
    }

    private void AssertDynamicCte(string query)
    {
        using var compiled = CompileDynamic(query);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual((short)20, table[0].Values[0]);
        AssertQueryLocalOutputColumn(table, "Status", typeof(short?), "JobStatus");
    }

    private void AssertDynamicFilter(string query)
    {
        using var compiled = CompileDynamic(query);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual((short)20, table[0].Values[0]);
        AssertQueryLocalOutputColumn(table, "Status", typeof(short?), "JobStatus");
    }

    private void AssertDynamicIntrinsics(string query)
    {
        using var compiled = CompileDynamic(query);
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(new object?[] { (short)20, "Running", true, true }, table[0].Values);
    }

    private void AssertNativeMetadata(
        string query,
        string columnName,
        Type sourceReadType,
        EnumTypeOrigin expectedOrigin)
    {
        var provider = CompileMetadata(query);
        var column = FindCapturedColumn(provider, columnName);
        var sourceEnumType = Nullable.GetUnderlyingType(sourceReadType) ?? sourceReadType;
        var carrierType = EnumScalarTypeFacts.GetCarrierType(column.EnumType!.UnderlyingKind);
        var nullableCarrierType = typeof(Nullable<>).MakeGenericType(carrierType);

        Assert.AreEqual(nullableCarrierType, column.ColumnType);
        Assert.AreEqual(sourceReadType, column.SourceReadType);
        AssertNativeDescriptor(column, sourceEnumType, expectedOrigin);
        Assert.IsFalse(provider.Schema.RowSourceRequested);
    }

    private void AssertQueryLocalMetadata(string query)
    {
        var provider = CompileMetadata(query);
        var column = FindCapturedColumn(provider, "Status");

        Assert.AreEqual(typeof(short?), column.ColumnType);
        Assert.AreEqual(typeof(short?), column.SourceReadType);
        AssertQueryLocalDescriptor(column, "JobStatus", EnumUnderlyingKind.Int16, isFlags: false);
        Assert.IsFalse(provider.Schema.RowSourceRequested);
    }

    private void AssertModifierSeparation(string query)
    {
        var provider = CompileMetadata(query);
        var column = FindCapturedColumn(provider, "Status");

        Assert.AreEqual("utf-8", column.ReadModifiers[ColumnReadModifiers.Encoding]);
        AssertNativeDescriptor(column, typeof(REC103NativeStatus), EnumTypeOrigin.NativeClr);
        Assert.IsFalse(provider.Schema.RowSourceRequested);
    }

    private void AssertMetadataFailure(
        string query,
        DiagnosticCode expectedCode,
        bool queryScopedRowsEnabled = true)
    {
        var result = CompileWithDiagnostics(query, queryScopedRowsEnabled);
        try
        {
            Assert.IsFalse(result.Succeeded, FormatDiagnostics(result));
            Assert.HasCount(1, result.Errors);
            var diagnostic = result.Errors.Single();
            Assert.AreEqual(expectedCode, diagnostic.Code, FormatDiagnostics(result));
            var expectedPhase = expectedCode == DiagnosticCode.MQ2012_InvalidSchemaDefinition
                ? DiagnosticPhase.Parse
                : DiagnosticPhase.Bind;
            Assert.AreEqual(expectedPhase, diagnostic.Phase);
        }
        finally
        {
            result.CompiledQuery?.Dispose();
        }
    }

    private CompiledQuery CompileDynamic(string query, bool queryScopedRowsEnabled = true)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new GeneratedQueryRowSampleSchemaProvider(
                GeneratedQueryRowSampleShape.Enum,
                queryScopedRowsEnabled),
            LoggerResolver,
            TestCompilationOptions,
            TestContext.CancellationToken);
    }

    private BuildResult CompileWithDiagnostics(string query, bool queryScopedRowsEnabled)
    {
        return InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new GeneratedQueryRowSampleSchemaProvider(
                GeneratedQueryRowSampleShape.Enum,
                queryScopedRowsEnabled),
            LoggerResolver,
            TestCompilationOptions);
    }

    private REC103MetadataSchemaProvider CompileMetadata(string query)
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
            return provider;
        }
        finally
        {
            result.CompiledQuery?.Dispose();
        }
    }

    private static ISchemaColumn FindCapturedColumn(
        REC103MetadataSchemaProvider provider,
        string columnName)
    {
        var column = provider.Schema.CapturedColumns
            .SelectMany(static columns => columns)
            .LastOrDefault(column => string.Equals(column.ColumnName, columnName, StringComparison.Ordinal));
        return column ?? throw new AssertFailedException(
            $"Metadata provider did not capture column '{columnName}'.");
    }

    private static void AssertNativeOutputColumn(
        Tables.Table table,
        string columnName,
        Type carrierType,
        Type sourceEnumType)
    {
        var column = table.Columns.Single(candidate => candidate.ColumnName == columnName);
        Assert.AreEqual(carrierType, column.ColumnType);
        Assert.AreEqual(carrierType, column.SourceReadType);
        AssertNativeDescriptor(column, sourceEnumType, EnumTypeOrigin.NativeClr);
    }

    private static void AssertQueryLocalOutputColumn(
        Tables.Table table,
        string columnName,
        Type carrierType,
        string displayName)
    {
        var column = table.Columns.Single(candidate => candidate.ColumnName == columnName);
        Assert.AreEqual(carrierType, column.ColumnType);
        Assert.AreEqual(carrierType, column.SourceReadType);
        Assert.IsNotNull(column.EnumType);
        Assert.AreEqual(displayName, column.EnumType.DisplayName);
        Assert.AreEqual(EnumTypeOrigin.QueryLocal, column.EnumType.Origin);
    }

    private static void AssertNativeDescriptor(
        ISchemaColumn column,
        Type sourceEnumType,
        EnumTypeOrigin expectedOrigin)
    {
        Assert.IsNotNull(column.EnumType);
        Assert.AreEqual(sourceEnumType.FullName, column.EnumType.DisplayName);
        Assert.AreEqual(expectedOrigin, column.EnumType.Origin);
        Assert.AreEqual(Enum.GetUnderlyingType(sourceEnumType),
            EnumScalarTypeFacts.GetCarrierType(column.EnumType.UnderlyingKind));
        Assert.IsFalse(string.IsNullOrWhiteSpace(column.EnumType.Fingerprint));
    }

    private static void AssertQueryLocalDescriptor(
        ISchemaColumn column,
        string displayName,
        EnumUnderlyingKind underlyingKind,
        bool isFlags)
    {
        Assert.IsNotNull(column.EnumType);
        Assert.AreEqual(displayName, column.EnumType.DisplayName);
        Assert.AreEqual(EnumTypeOrigin.QueryLocal, column.EnumType.Origin);
        Assert.AreEqual(underlyingKind, column.EnumType.UnderlyingKind);
        Assert.AreEqual(isFlags, column.EnumType.IsFlags);
        Assert.IsFalse(string.IsNullOrWhiteSpace(column.EnumType.Fingerprint));
    }

    private static string FormatDiagnostics(BuildResult result)
    {
        return string.Join(
            Environment.NewLine,
            result.Diagnostics.Select(static diagnostic =>
                $"{diagnostic.Code} [{diagnostic.Phase}] {diagnostic.Message}"));
    }

    public sealed record REC103Candidate(
        string CaseId,
        REC103CandidateKind Kind,
        string Query,
        DiagnosticCode? ExpectedCode = null);

    public enum REC103CandidateKind
    {
        NativeStatus,
        NativeAccess,
        NativeNullable,
        NativeUnknown,
        NativeUnknownFlags,
        NativeCte,
        NativeJoin,
        NativeMixed,
        MetadataNativeStatus,
        MetadataNativeNullable,
        MetadataNativeFlags,
        MetadataQueryLocal,
        MetadataSimpleNativeName,
        MetadataUnknownNativeName,
        MetadataDuplicateModifier,
        MetadataModifierSeparation,
        DynamicStatus,
        DynamicAccess,
        DynamicNullableCarrier,
        DynamicCaseInsensitiveType,
        DynamicCte,
        DynamicFilter,
        DynamicIntrinsics,
        DynamicCapabilityFailure
    }
}

public sealed class REC103MetadataSchemaProvider : ISchemaProvider
{
    public REC103MetadataSchema Schema { get; } = new();

    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema.TrimStart('#'), "rec103", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(schema);

        return Schema;
    }
}

public sealed class REC103MetadataSchema : SchemaBase
{
    public REC103MetadataSchema()
        : base("rec103", new MethodsAggregator(new MethodsManager()))
    {
    }

    public List<ISchemaColumn[]> CapturedColumns { get; } = [];

    public bool RowSourceRequested { get; private set; }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        var descriptor = base.DescribeSource(name, context, parameters);
        return descriptor with
        {
            TransferCapabilities = SourceTransferCapabilities.QueryScopedRows |
                                   SourceTransferCapabilities.LogicalScalarReads
        };
    }

    public override SchemaMethodInfo[] GetRawConstructors(
        string methodName,
        SourceMetadataContext metadataContext)
    {
        return string.Equals(methodName, "native", StringComparison.OrdinalIgnoreCase)
            ? [new SchemaMethodInfo(methodName, SchemaConstructorInfo.Empty())]
            : [];
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        var columns = metadataContext.AllColumns.ToArray();
        CapturedColumns.Add(columns);
        return new REC103MetadataTable(columns);
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        RowSourceRequested = true;
        throw new NotSupportedException("REC-103 metadata-only provider must not execute rows.");
    }
}

public sealed class REC103MetadataTable(ISchemaColumn[] columns) : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = columns;

    public SchemaTableMetadata Metadata { get; } = new(typeof(REC103NativeEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}

public sealed record REC103NativeEntity(
    REC103NativeStatus Status,
    REC103NativeAccess Access,
    REC103NativeStatus? OptionalStatus,
    REC103NativeAccess? OptionalAccess,
    int Number);

public enum REC103NativeStatus : short
{
    Queued = 10,
    Running = 20,
    Finished = 30
}

[Flags]
public enum REC103NativeAccess : uint
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write
}
