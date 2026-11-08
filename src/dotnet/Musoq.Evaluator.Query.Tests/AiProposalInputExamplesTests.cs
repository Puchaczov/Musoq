using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>Checks implemented SQL inputs used by the AI proposal; no inference is simulated.</summary>
[TestClass]
public sealed class AiProposalInputExamplesTests : BasicEntityTestBase
{
    [TestMethod]
    public void SuppliedExtractionAndPaymentRows_ShouldJoinWithoutAProvider()
    {
        const string query = "with Invoices as (select VendorName, Total from " +
            "values { (VendorName: 'Example', Total: 120d) } extracted), " +
            "Payments as (select Amount from values { (Amount: -120d), (Amount: -50d) } supplied) " +
            "select i.VendorName, i.Total, p.Amount from Invoices i " +
            "inner join Payments p on Abs(i.Total - Abs(p.Amount)) < 0.01";
        var table = CreateAndRunVirtualMachine(query, EmptySources()).Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertColumns(table,
            ("VendorName", typeof(string)), ("Total", typeof(decimal)), ("Amount", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Example", 120m, -120m]);
    }

    [TestMethod]
    public void LiteralSyslogInput_ShouldConsumeEachAuthoredDelimiterOnce()
    {
        const string query = "text SyslogLine { Timestamp: until ' ', Host: until ' ', " +
            "Process: until ':', _: literal ' ', Message: rest }; " +
            "select log.Timestamp, log.Host, log.Process, log.Message " +
            "from values { (Line: '2024-01-02T03:04:05Z host app: request timeout') } line " +
            "cross apply Parse<SyslogLine>(line.Line) log";
        var table = CreateAndRunVirtualMachine(query, EmptySources()).Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertRowsInOrder(table,
            ["2024-01-02T03:04:05Z", "host", "app", "request timeout"]);
    }

    [TestMethod]
    public void LiteralCodeInputs_ShouldExposeOnlyTheSuppliedBodyAndMetrics()
    {
        const string query = "select m.ClassName, m.Name, m.LinesOfCode, m.CyclomaticComplexity from " +
            "values { (ClassName: 'Repository', Name: 'Load', LinesOfCode: 1, CyclomaticComplexity: 1, " +
            "Body: 'string Load(string sql) { return Execute(sql); }') } m";
        var table = CreateAndRunVirtualMachine(query, EmptySources()).Run(TestContext.CancellationToken);
        _ = table.Count;

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Repository", "Load", 1, 1]);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> EmptySources() => new();
}
