using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class InterpretationRepeatLimitsTests : BasicEntityTestBase
{
    private const string BinaryQuery = "binary Rec { X: byte } binary S { Records: Rec repeat until eof } " +
        "select Count(r.X) as N from #test.files() f cross apply Interpret<S>(f.Content) s cross apply s.Records r";
    private const string TextQuery = "text Rec { X: chars[1] } text S { Records: repeat Rec until end } " +
        "select Count(r.X) as N from #test.files() f cross apply Parse<S>(f.Text) s cross apply s.Records r";

    [TestMethod]
    [DataRow(false, 10_000, 10_000)]
    [DataRow(true, 10_000, 10_000)]
    [DataRow(false, 10_001, 10_001)]
    [DataRow(true, 10_001, 10_001)]
    [DataRow(false, 3, 3)]
    [DataRow(true, 3, 3)]
    public void RepetitionAtConfiguredBoundary_ShouldReadEveryRecord(bool text, int records, int maximum)
    {
        using var query = Compile(text ? TextQuery : BinaryQuery, Provider(text, records), maximum);
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(long)records]);
    }

    [TestMethod]
    [DataRow(false, 10_001, 10_000)]
    [DataRow(true, 10_001, 10_000)]
    [DataRow(false, 4, 3)]
    [DataRow(true, 4, 3)]
    public void ExceedingConfiguredBoundary_ShouldPreserveDiagnosticAndFailurePosition(bool text, int records, int maximum)
    {
        using var query = Compile(text ? TextQuery : BinaryQuery, Provider(text, records), maximum);
        var failure = CaptureParseFailure(query);

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, failure.ErrorCode);
        Assert.AreEqual(DiagnosticCode.MQ7109_InterpretationMaxIterationsExceeded, failure.ToDiagnostic().Code);
        Assert.AreEqual(maximum, failure.Position);
        Assert.AreEqual("Records", failure.FieldName);
    }

    [TestMethod]
    public void FixedCountArray_ShouldRemainIndependentOfRepeatLimits()
    {
        var sql = BinaryQuery.Replace("Rec repeat until eof", "Rec[10001]", StringComparison.Ordinal);
        using var query = Compile(sql, Provider(false, 10_001), 1);
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [10_001L]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ZeroProgressProtection_ShouldRemainImmediateWithLargerLimits(bool text)
    {
        const string binary = "binary S { Records: byte[0] repeat until eof } select 1 as N from #test.files() f cross apply Interpret<S>(f.Content) s";
        const string textual = "text Empty { } text S { Records: repeat Empty until end } select 1 as N from #test.files() f cross apply Parse<S>(f.Text) s";
        using var query = Compile(text ? textual : binary, Provider(text, 1), int.MaxValue);
        var failure = CaptureParseFailure(query);

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, failure.ErrorCode);
        Assert.AreEqual(0, failure.Position);
        StringAssert.Contains(failure.Details, "no progress");
    }

    [TestMethod]
    public void ConditionRepeat_ShouldAllowItsTerminatorAtTheBoundary()
    {
        const string sql = "binary S { Records: byte repeat until Records[-1] = 0 } select Count(*) as N " +
            "from #test.files() f cross apply Interpret<S>(f.Content) s cross apply s.Records r";
        using var query = Compile(sql, BinaryProvider([1, 1, 0]), 3);
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [3L]);
    }

    [TestMethod]
    public void ConditionRepeat_ShouldFailBeforeReadingPastItsBoundary()
    {
        const string sql = "binary S { Records: byte repeat until Records[-1] = 0 } select Count(*) as N " +
            "from #test.files() f cross apply Interpret<S>(f.Content) s cross apply s.Records r";
        using var query = Compile(sql, BinaryProvider([1, 1, 1, 0]), 3);
        var failure = CaptureParseFailure(query);

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, failure.ErrorCode);
        Assert.AreEqual(3, failure.Position);
    }

    [TestMethod]
    public void NestedSubstreams_ShouldReceiveTheSelectedLimitWithIndependentCounters()
    {
        const string sql = "binary Inner { Values: byte repeat until eof } " +
            "binary S { A: substream[2] as Inner, B: substream[2] as Inner } " +
            "select s.A.Values.Length as A, s.B.Values.Length as B from #test.files() f cross apply Interpret<S>(f.Content) s";
        using var query = Compile(sql, BinaryProvider([1, 2, 3, 4]), 2);
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [2, 2]);
    }

    [TestMethod]
    public void NestedSubstream_ShouldEnforceTheSelectedLimit()
    {
        const string sql = "binary Inner { Values: byte repeat until eof } binary S { Payload: substream[3] as Inner } " +
            "select 1 as N from #test.files() f cross apply Interpret<S>(f.Content) s";
        using var query = Compile(sql, BinaryProvider([1, 2, 3]), 2);
        var failure = CaptureParseFailure(query);

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, failure.ErrorCode);
        Assert.AreEqual(2, failure.Position);
        Assert.AreEqual("Payload.Values", failure.FieldName);
    }

    [TestMethod]
    public void Cancellation_ShouldRemainObservableWithConfiguredLimits()
    {
        using var query = Compile(BinaryQuery, Provider(false, 3), 20_000);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
        {
            using var table = query.Run(cancellation.Token);
            _ = table.Count;
        });
    }

    private CompiledQuery Compile(string sql, ISchemaProvider provider, int maximum) =>
        InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider, LoggerResolver,
            new CompilationOptions(ParallelizationMode.None, usePrimitiveTypeValidation: false)
                .WithTableResultMaterialization().WithInterpretationLimits(new(maximum)));

    private static ISchemaProvider Provider(bool text, int records) => text
        ? new TextSchemaProvider(new Dictionary<string, IEnumerable<TextEntity>>
            { ["#test"] = [new TextEntity { Text = new string('x', records) }] })
        : BinaryProvider(Enumerable.Repeat((byte)1, records).ToArray());

    private static ISchemaProvider BinaryProvider(byte[] bytes) =>
        new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
            { ["#test"] = [new BinaryEntity { Content = bytes }] });

    private static ParseException CaptureParseFailure(CompiledQuery query)
    {
        try
        {
            using var table = query.Run();
            _ = table.Count;
        }
        catch (Exception exception)
        {
            for (Exception? current = exception; current != null; current = current.InnerException)
                if (current is ParseException failure)
                    return failure;
            throw new AssertFailedException("Expected an interpretation failure.", exception);
        }
        throw new AssertFailedException("Expected the repeat guard to fail.");
    }
}
