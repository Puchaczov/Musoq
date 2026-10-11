using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public sealed partial class ScriptParameterTypeCompatibilityTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void NumericStatusFixture_ShouldExposeShortMetadataAndAcceptNumericParameter()
    {
        const string query = "param(minimum: int) select Status from #specstressnative.rows() where Status >= $minimum";
        using var compiled = InstanceCreator.CompileForExecution(
            query, Guid.NewGuid().ToString(), CreateNumericStatusProvider(),
            LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "minimum", 10);

        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual((short)20, result[0].ReadValue(0));
        Assert.AreEqual(typeof(short), result.Columns.Single().ColumnType);
    }

    // P1-0110/P1-0124 fixture repair: the original native provider copied
    // provisional query columns from AllColumns, replacing Status:short with
    // Status:object. Inspection confirmed object metadata and runtime operators.
    // Preserve the exact queries and MQ3005 bind expectations, but supply a fixed
    // short20 source whose metadata cannot be replaced by provisional columns.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0110")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "1")]
    public void Current_P1_0110_TypeMismatchStringParameterIsRejected()
    {
        const string query = "param(minimum: string) select Status from #specstressnative.rows() where Status >= $minimum";
        AssertNumericStatusParameterMismatch(query);
    }
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0124")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "1")]
    public void Current_P1_0124_TypeMismatchBooleanParameterIsRejected()
    {
        const string query = "param(flag: bool) select Status from #specstressnative.rows() where Status >= $flag";
        AssertNumericStatusParameterMismatch(query);
    }
    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-StringParameterAgainstNumericColumnIsRejectedAtBindTime")]
    public void StringParameterAgainstNumericColumnIsRejectedAtBindTime()
    {
        const string query = "param(minimum: string) select 1 from #specstressnative.rows() where Status >= $minimum";

        var exception = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(
            () =>
            {
                using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
                    query,
                    System.Guid.NewGuid().ToString(),
                    CreateNumericStatusProvider(),
                    LoggerResolver,
                    TestCompilationOptions,
                    TestContext.CancellationToken);
            });

        Assert.AreEqual(
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ3005_TypeMismatch,
            exception.PrimaryEnvelope.Code);
        Assert.AreEqual(
            Musoq.Parser.Diagnostics.DiagnosticPhase.Bind,
            exception.PrimaryEnvelope.Phase);
    }
    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-BooleanParameterAgainstNumericGreaterOrEqualIsRejectedAtBindTime")]
    public void BooleanParameterAgainstNumericGreaterOrEqualIsRejectedAtBindTime()
    {
        const string query = "param(flag: bool) select 1 from #specstressnative.rows() where Status >= $flag";

        var exception = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(
            () =>
            {
                using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
                    query,
                    System.Guid.NewGuid().ToString(),
                    CreateNumericStatusProvider(),
                    LoggerResolver,
                    TestCompilationOptions,
                    TestContext.CancellationToken);
            });

        Assert.AreEqual(
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ3005_TypeMismatch,
            exception.PrimaryEnvelope.Code);
        Assert.AreEqual(
            Musoq.Parser.Diagnostics.DiagnosticPhase.Bind,
            exception.PrimaryEnvelope.Phase);
    }
    private void AssertNumericStatusParameterMismatch(string query)
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
        {
            using var compiled = InstanceCreator.CompileForExecution(
                query, Guid.NewGuid().ToString(), CreateNumericStatusProvider(),
                LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);
        });
        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, exception.PrimaryEnvelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, exception.PrimaryEnvelope.Phase);
    }

    private static ISchemaProvider CreateNumericStatusProvider()
    {
        var schema = new GenericSchema<LibraryBase>(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["rows"] = (new GenericEntityTable<NumericStatusEntity>(),
                new GenericChunkSource<NumericStatusEntity>([new NumericStatusEntity { Status = 20 }],
                    GenericEntityTable<NumericStatusEntity>.NameToIndexMap,
                    GenericEntityTable<NumericStatusEntity>.IndexToObjectAccessMap))
        });
        return new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            ["#specstressnative"] = schema,
            ["specstressnative"] = schema
        });
    }

    public sealed class NumericStatusEntity
    {
        public short Status { get; init; }
    }
}
