using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class InterpretationProviderIndependentExamplesTests : BasicEntityTestBase
{
    private const string Header = "binary Header { Magic: int le, Version: short le }; ";
    private const string Bytes = "let content: byte[] = array { 120ub, 86ub, 52ub, 18ub, 1ub, 0ub }; ";

    [TestMethod]
    public void ExplicitBytes_ShouldRetainHeaderValuesAndTypes()
    {
        var table = CreateAndRunVirtualMachine(Header + Bytes +
            "select h.Magic, h.Version from values { (Seed: 0) } seed " +
            "cross apply Interpret<Header>($content) h", EmptySources()).Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertColumns(table, ("Magic", typeof(int)), ("Version", typeof(short)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [305419896, (short)1]);
    }

    [TestMethod]
    public void SafeExplicitBytes_ShouldPreserveValidAndTruncatedInputRows()
    {
        var table = CreateAndRunVirtualMachine(Header + Bytes +
            "let shortContent: byte[] = array { 1ub }; " +
            "select input.Name, case when h.Magic is not null then 'Valid' else 'Invalid' end as Status " +
            "from values { (Name: 'valid', IsValid: true), (Name: 'truncated', IsValid: false) } input " +
            "outer apply TryInterpret<Header>(case when input.IsValid then $content else $shortContent end) h",
            EmptySources()).Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["valid", "Valid"], ["truncated", "Invalid"]);
    }

    [TestMethod]
    public void ExplicitParentCount_ShouldReadBothNestedElements()
    {
        const string query = "binary Item { Value: byte }; binary Body { Items: Item[parent.Count] }; " +
            "binary Record { Count: byte, Payload: Body }; let content: byte[] = array { 2ub, 10ub, 20ub }; " +
            "select i.Value from values { (Seed: 0) } seed cross apply Interpret<Record>($content) r " +
            "cross apply r.Payload.Items i";
        var table = CreateAndRunVirtualMachine(query, EmptySources()).Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10], [(byte)20]);
    }

    [TestMethod]
    public void PartialExplicitBytes_ShouldReportTheTerminalCursorAndCompletedField()
    {
        var table = CreateAndRunVirtualMachine(Header +
            "let content: byte[] = array { 120ub, 86ub, 52ub, 18ub, 1ub }; " +
            "select p.ParsedFields.Magic, p.ErrorField, p.BytesConsumed, p.BitOffset, p.IsSuccess, p.CompletedFields " +
            "from values { (Seed: 0) } seed cross apply PartialInterpret<Header>($content) p",
            EmptySources()).Run(TestContext.CancellationToken);
        _ = table.Count;

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(305419896, table[0].ReadValue(0));
        Assert.AreEqual("Version", table[0].ReadValue(1));
        Assert.AreEqual(4, table[0].ReadValue(2));
        Assert.AreEqual(0, table[0].ReadValue(3));
        Assert.AreEqual(false, table[0].ReadValue(4));
        CollectionAssert.AreEqual(new[] { "Magic" }, ((IEnumerable<string>)table[0].ReadValue(5)!).ToArray());
    }

    [TestMethod]
    public void LiteralText_ShouldKeepTheMalformedRowUnderOuterApply()
    {
        const string query = "text LogEntry { Timestamp: between '[' ']', _: literal ' ', Level: until ':', " +
            "_: literal ' ', Message: rest }; " +
            "select log.Timestamp, log.Level from values { (Line: '[2024-01-02] INFO: ready'), " +
            "(Line: 'malformed') } lines outer apply TryParse<LogEntry>(lines.Line) log";
        var table = CreateAndRunVirtualMachine(query, EmptySources()).Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["2024-01-02", "INFO"], [null, null]);
    }

    [TestMethod]
    public void RejectedParseProjection_ShouldKeepTheCallableContextDiagnostic()
    {
        const string query = "text LogEntry { Message: rest }; select Parse<LogEntry>(line.Line) " +
            "from values { (Line: 'ready') } line";
        var exception = Assert.ThrowsExactly<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, EmptySources()));

        Assert.AreEqual(DiagnosticCode.MQ3033_InterpretFunctionOutsideApply, exception.PrimaryEnvelope.Code);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> EmptySources() => new();
}
