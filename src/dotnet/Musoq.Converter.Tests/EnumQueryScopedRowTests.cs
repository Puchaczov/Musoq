using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Tests;

[TestClass]
[DoNotParallelize]
public sealed class EnumQueryScopedRowTests
{
    private const string DynamicContract =
        "enum JobStatus : int { Queued = 10, Running = 20, Finished = 30 };" +
        "flags enum FileAccess : uint { None = 0ui, Read = 1ui, Write = 2ui, ReadWrite = 3ui };" +
        "table Jobs { Status: JobStatus, Access: FileAccess };" +
        "couple #enumrows.dynamic with table Jobs as Jobs;";

    private readonly TestsLoggerResolver _loggerResolver = new();

    public static IEnumerable<object[]> BackingKinds
    {
        get
        {
            yield return ["byte", "20ub", typeof(byte?), EnumUnderlyingKind.Byte];
            yield return ["sbyte", "20b", typeof(sbyte?), EnumUnderlyingKind.SByte];
            yield return ["short", "20s", typeof(short?), EnumUnderlyingKind.Int16];
            yield return ["ushort", "20us", typeof(ushort?), EnumUnderlyingKind.UInt16];
            yield return ["int", "20i", typeof(int?), EnumUnderlyingKind.Int32];
            yield return ["uint", "20ui", typeof(uint?), EnumUnderlyingKind.UInt32];
            yield return ["long", "20l", typeof(long?), EnumUnderlyingKind.Int64];
            yield return ["ulong", "20ul", typeof(ulong?), EnumUnderlyingKind.UInt64];
        }
    }

    [TestMethod]
    public void QueryLocalEnums_WhenSourceReadsNamesAndNumbers_UsePrimitiveQueryRowsAndPortableMetadata()
    {
        const string query = DynamicContract +
                             " select Status, Access, EnumName(Status) as StatusName from Jobs()";
        var provider = new EnumQueryRowsSchemaProvider();
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            "query-local-enum-source",
            provider,
            _loggerResolver);

        Assert.IsTrue(build.Succeeded, FormatFailure(build));
        var inspection = InstanceCreator.CompileForInspection(
            query,
            "query-local-enum-source-inspection",
            provider,
            _loggerResolver);
        Assert.Contains("reader.Read<int?>(0)", inspection.GeneratedCSharpCode);
        Assert.Contains("reader.Read<uint?>(1)", inspection.GeneratedCSharpCode);
        Assert.Contains("new global::Musoq.Schema.EnumTypeDescriptor(\"JobStatus\"", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("reader.Read<object>", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("System.Enum", inspection.GeneratedCSharpCode);

        using var table = build.CompiledQuery!.Run();
        Assert.AreEqual(3, table.Count);
        CollectionAssert.AreEqual(new object?[] { 20, 3u, "Running" }, table[0].Values);
        CollectionAssert.AreEqual(new object?[] { 10, 1u, "Queued" }, table[1].Values);
        CollectionAssert.AreEqual(new object?[] { 99, 8u, null }, table[2].Values);
        Assert.IsFalse(table.SelectMany(static row => row.Values).Any(static value => value is Enum));

        var status = table.Columns.ElementAt(0);
        Assert.AreEqual(typeof(int?), status.ColumnType);
        Assert.AreEqual(typeof(int?), status.SourceReadType);
        Assert.AreEqual(EnumTypeOrigin.QueryLocal, status.EnumType!.Origin);
        Assert.AreEqual(provider.Schema.FrozenEnumFingerprints["Status"], status.EnumType.Fingerprint);
    }

    [TestMethod]
    public void NativeEnums_WhenQueryScopedSourceIsUsed_ReadExactNativeTypesThenCastToCarriers()
    {
        const string query = "select Status, Access, OptionalStatus from #enumrows.native()";
        var provider = new EnumQueryRowsSchemaProvider();
        var inspection = InstanceCreator.CompileForInspection(
            query,
            "native-enum-source-inspection",
            provider,
            _loggerResolver);
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            "native-enum-source",
            provider,
            _loggerResolver);

        Assert.IsTrue(build.Succeeded, FormatFailure(build));
        StringAssert.Contains(inspection.GeneratedCSharpCode, "reader.Read<Musoq.Converter.Tests.NativeQueryStatus>(0)");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "reader.Read<Musoq.Converter.Tests.NativeQueryAccess>(1)");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "reader.Read<Musoq.Converter.Tests.NativeQueryStatus?>(2)");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "(short)reader.Read<Musoq.Converter.Tests.NativeQueryStatus>(0)");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "(uint)reader.Read<Musoq.Converter.Tests.NativeQueryAccess>(1)");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "(short?)reader.Read<Musoq.Converter.Tests.NativeQueryStatus?>(2)");
        Assert.DoesNotContain("reader.Read<object>", inspection.GeneratedCSharpCode);

        using var table = build.CompiledQuery!.Run();
        CollectionAssert.AreEqual(new object?[] { (short)20, 3u, (short)20 }, table[0].Values);
        CollectionAssert.AreEqual(new object?[] { (short)10, 1u, (short)10 }, table[1].Values);
        CollectionAssert.AreEqual(new object?[] { (short)99, 8u, (short)99 }, table[2].Values);
        Assert.AreEqual(typeof(short), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(short), table.Columns.ElementAt(0).SourceReadType);
        Assert.AreEqual(EnumTypeOrigin.NativeClr, table.Columns.ElementAt(0).EnumType!.Origin);
    }

    [TestMethod]
    [DynamicData(nameof(BackingKinds))]
    public void QueryLocalEnums_AllBackingKinds_ReadDirectlyIntoNullablePrimitiveCarriers(
        string backingName,
        string memberLiteral,
        Type expectedType,
        EnumUnderlyingKind expectedKind)
    {
        var query =
            $"enum StatusEnum : {backingName} {{ Running = {memberLiteral} }};" +
            "table Jobs { Status: StatusEnum };" +
            "couple #enumrows.dynamic with table Jobs as Jobs;" +
            "select Status from Jobs()";
        var provider = new EnumQueryRowsSchemaProvider();
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            $"enum-backing-{backingName}",
            provider,
            _loggerResolver);

        Assert.IsTrue(build.Succeeded, FormatFailure(build));
        using var table = build.CompiledQuery!.Run();
        Assert.AreEqual(expectedType, table.Columns.Single().ColumnType);
        Assert.AreEqual(expectedKind, table.Columns.Single().EnumType!.UnderlyingKind);
        Assert.IsFalse(table.SelectMany(static row => row.Values).Any(static value => value is Enum));
    }

    [TestMethod]
    public void EnumPredicates_WhenSourcePlansThem_PreserveLiteralsNegationNullAndFlagsModes()
    {
        var provider = new EnumQueryRowsSchemaProvider();
        var build = InstanceCreator.CompileWithDiagnostics(
            DynamicContract +
            " select Status from Jobs()" +
            " where Status <> 'Finished'" +
            " and Status not in ('Queued', 'Running')" +
            " and Access is not null" +
            " and HasAllFlags(Access, 'Read', 'Write')",
            "enum-predicate-source-build",
            provider,
            _loggerResolver);
        Assert.IsTrue(build.Succeeded, FormatFailure(build));
        var inspection = InstanceCreator.CompileForInspection(
            DynamicContract +
            " select Status from Jobs()" +
            " where Status <> 'Finished'" +
            " and Status not in ('Queued', 'Running')" +
            " and Access is not null" +
            " and HasAllFlags(Access, 'Read', 'Write')",
            "enum-predicate-source",
            provider,
            _loggerResolver);

        Assert.IsFalse(inspection.Diagnostics.Any(static diagnostic => diagnostic.IsError));
        var predicates = Flatten(provider.Schema.LastPlanRequest!.Predicate).ToArray();
        Assert.IsTrue(predicates.OfType<SourcePredicateComparison>().Any(comparison =>
            comparison.Operator == SourcePredicateComparisonOperator.NotEqual &&
            comparison.Right is SourcePredicateEnumLiteral));
        Assert.IsTrue(predicates.OfType<SourcePredicateIn>().Any(static sourceIn =>
            sourceIn.IsNegated && sourceIn.Values.All(static value => value is SourcePredicateEnumLiteral)));
        Assert.IsTrue(predicates.OfType<SourcePredicateNullCheck>().Any(static check => check.IsNegated));
        Assert.IsTrue(predicates.OfType<SourcePredicateFlags>().Any(flags =>
            flags.MatchMode == SourcePredicateFlagsMatchMode.All &&
            flags.Mask.Value == EnumScalarValue.FromUInt32(3)));
    }

    [TestMethod]
    public void QueryLocalEnum_WhenSourceLacksLogicalScalarReads_ReportsCapabilityDiagnostic()
    {
        var provider = new EnumQueryRowsSchemaProvider(SourceTransferCapabilities.QueryScopedRows);
        var build = InstanceCreator.CompileWithDiagnostics(
            DynamicContract + " select Status from Jobs()",
            "enum-source-capability",
            provider,
            _loggerResolver);

        Assert.IsFalse(build.Succeeded);
        Assert.IsTrue(build.Diagnostics.Any(static diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ3114_EnumSourceCapabilityRequired), FormatFailure(build));
    }

    [TestMethod]
    public void QueryLocalEnum_WhenSourceCorruptsDescriptor_ReportsMismatchDiagnostic()
    {
        var provider = new EnumQueryRowsSchemaProvider(corruptDescriptor: true);
        var build = InstanceCreator.CompileWithDiagnostics(
            DynamicContract + " select Status from Jobs()",
            "enum-source-descriptor",
            provider,
            _loggerResolver);

        Assert.IsFalse(build.Succeeded);
        Assert.IsTrue(build.Diagnostics.Any(static diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ3115_EnumDescriptorMismatch), FormatFailure(build));
    }

    [TestMethod]
    public void QueryLocalEnum_WhenDescriptorDriftsAfterCompilation_RequiresRecompilation()
    {
        var provider = new EnumQueryRowsSchemaProvider();
        var build = InstanceCreator.CompileWithDiagnostics(
            DynamicContract + " select Status from Jobs()",
            "enum-source-drift",
            provider,
            _loggerResolver);
        Assert.IsTrue(build.Succeeded, FormatFailure(build));
        provider.Schema.DriftAtExecution = true;

        using var table = build.CompiledQuery!.Run();
        var exception = Assert.Throws<Exception>(() => _ = table[0]);

        StringAssert.Contains(exception.ToString(), "recompile the query");
    }

    [TestMethod]
    public void QueryLocalEnum_WhenCompiledTwice_ShouldReusePortableExecutionArtifact()
    {
        var salt = Random.Shared.Next(1000, int.MaxValue);
        var query =
            $"enum JobStatus : int {{ Queued = 10, Running = 20, Finished = 30, CacheSalt = {salt} }};" +
            "flags enum FileAccess : uint { None = 0ui, Read = 1ui, Write = 2ui, ReadWrite = 3ui };" +
            "table Jobs { Status: JobStatus, Access: FileAccess };" +
            "couple #enumrows.dynamic with table Jobs as Jobs;" +
            "select Status from Jobs()";
        var first = InstanceCreator.CompileWithDiagnostics(
            query,
            $"enum-cache-first-{Guid.NewGuid():N}",
            new EnumQueryRowsSchemaProvider(),
            _loggerResolver);
        var second = InstanceCreator.CompileWithDiagnostics(
            query,
            $"enum-cache-second-{Guid.NewGuid():N}",
            new EnumQueryRowsSchemaProvider(),
            _loggerResolver);

        try
        {
            Assert.IsTrue(first.Succeeded, FormatFailure(first));
            Assert.IsTrue(second.Succeeded, FormatFailure(second));
            Assert.IsFalse(first.BuildItems!.StopAfterPlanning);
            Assert.IsTrue(
                second.BuildItems!.StopAfterPlanning,
                "An identical portable enum descriptor did not reuse the execution artifact.");
            using var table = second.CompiledQuery!.Run();
            Assert.AreEqual(3, table.Count);
            Assert.IsNotNull(table.Columns.Single().EnumType);
        }
        finally
        {
            first.CompiledQuery?.Dispose();
            second.CompiledQuery?.Dispose();
        }
    }

    [TestMethod]
    public void QueryLocalEnumArtifact_WhenDisposed_ShouldReleaseCollectibleLoadContext()
    {
        var contextReference = CreateAndDisposeEnumArtifact();

        ForceCollection(contextReference);

        Assert.IsFalse(
            contextReference.IsAlive,
            "A compiled query-local enum descriptor retained its collectible generated assembly.");
    }

    [TestMethod]
    public void NativeEnumProjection_WhenTypedOutputMemberIsEnum_ShouldRejectCarrierMapping()
    {
        Assert.Throws<InvalidOperationException>(() =>
            InstanceCreator.CompileForTypedExecution<NativeEnumOutputDto>(
                "select Status from #enumrows.native()",
                $"enum-typed-output-rejected-{Guid.NewGuid():N}",
                new EnumQueryRowsSchemaProvider(),
                _loggerResolver));

        var carrierQuery = InstanceCreator.CompileForTypedExecution<CarrierOutputDto>(
            "select Status from #enumrows.native()",
            $"enum-typed-output-carrier-{Guid.NewGuid():N}",
            new EnumQueryRowsSchemaProvider(),
            _loggerResolver);
        var rows = carrierQuery.Run(CancellationToken.None).ToArray();

        CollectionAssert.AreEqual(
            new short[] { 20, 10, 99 },
            rows.Select(static row => row.Status).ToArray());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private WeakReference CreateAndDisposeEnumArtifact()
    {
        const string query = DynamicContract +
                             " select Status, EnumName(Status) as StatusName from Jobs()";
        var artifactBuild = InstanceCreator.CompileArtifactWithDiagnostics(
            query,
            $"enum-artifact-{Guid.NewGuid():N}",
            new EnumQueryRowsSchemaProvider(),
            _loggerResolver);
        Assert.IsTrue(artifactBuild.Succeeded, FormatFailure(artifactBuild));
        var artifact = artifactBuild.Artifact ??
                       throw new AssertFailedException("Enum artifact compilation returned no artifact.");
        var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            query,
            artifact,
            new EnumQueryRowsSchemaProvider(),
            _loggerResolver);
        Assert.IsTrue(loaded.Succeeded, FormatFailure(loaded));
        var compiledQuery = loaded.CompiledQuery ??
                            throw new AssertFailedException("Enum artifact loading returned no query.");
        var runtimeType = GetGeneratedRuntimeType(compiledQuery);
        var context = AssemblyLoadContext.GetLoadContext(runtimeType.Assembly) ??
                      throw new AssertFailedException("Generated enum artifact has no load context.");
        Assert.IsTrue(context.IsCollectible);
        var reference = new WeakReference(context);

        using (var table = compiledQuery.Run())
        {
            Assert.AreEqual(3, table.Count);
            Assert.IsNotNull(table.Columns.ElementAt(0).EnumType);
        }

        compiledQuery.Dispose();
        return reference;
    }

    private static Type GetGeneratedRuntimeType(global::Musoq.Evaluator.CompiledQuery query)
    {
        var runnableField = typeof(global::Musoq.Evaluator.CompiledQuery).GetField(
            "_runnable",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
                            throw new AssertFailedException("The compiled query runnable was not found.");
        var current = runnableField.GetValue(query) ??
                      throw new AssertFailedException("The compiled query runnable was not initialized.");
        while (FindProperty(current.GetType(), "Inner")?.GetValue(current) is { } inner)
            current = inner;

        return current.GetType();
    }

    private static PropertyInfo? FindProperty(Type type, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.GetProperty(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is { } property)
            {
                return property;
            }
        }

        return null;
    }

    private static void ForceCollection(WeakReference reference)
    {
        for (var attempt = 0; attempt < 20 && reference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private static IEnumerable<SourcePredicateExpression> Flatten(SourcePredicateExpression? predicate)
    {
        if (predicate is SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } logical)
            return Flatten(logical.Left).Concat(Flatten(logical.Right));

        return predicate == null ? [] : [predicate];
    }

    private static string FormatFailure(BuildResult build)
    {
        return $"{build.CaughtException}{Environment.NewLine}{string.Join(Environment.NewLine, build.Diagnostics)}";
    }

    private static string FormatFailure(ArtifactBuildResult build)
    {
        return $"{build.CaughtException}{Environment.NewLine}{string.Join(Environment.NewLine, build.Diagnostics)}";
    }

    public sealed class NativeEnumOutputDto
    {
        public NativeQueryStatus Status { get; set; }
    }

    public sealed class CarrierOutputDto
    {
        public short Status { get; set; }
    }
}
