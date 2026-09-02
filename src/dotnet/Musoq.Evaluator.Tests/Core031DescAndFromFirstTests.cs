using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core031DescAndFromFirstTests : BasicEntityTestBase
{
    [TestMethod]
    public void DescColumn_PrimitiveTarget_ShouldKeepQuerySpanAndGuidance()
    {
        const string query = "desc #A.entities() column Name";

        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("Ada")));
        var exception = Assert.Throws<ColumnMustBeAnArrayOrImplementIEnumerableException>(
            () => vm.Run(TestContext.CancellationToken));
        var envelope = MusoqErrorEnvelope.FromException(exception, query);

        Assert.AreEqual(DiagnosticCode.MQ3025_ColumnMustBeArray, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(query.IndexOf("Name", StringComparison.Ordinal), envelope.Offset);
        Assert.AreEqual(query.IndexOf("Name", StringComparison.Ordinal) + "Name".Length, envelope.EndOffset);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.AreEqual("Name", envelope.Arguments["column"]);
    }

    [TestMethod]
    public void DescColumn_MissingNestedProperty_ShouldKeepQuerySpanAndGuidance()
    {
        const string query = "desc #A.entities() column Self.MissingProperty";

        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("Ada")));
        var exception = Assert.Throws<UnknownColumnOrAliasException>(
            () => vm.Run(TestContext.CancellationToken));
        var envelope = MusoqErrorEnvelope.FromException(exception, query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(query.IndexOf("MissingProperty", StringComparison.Ordinal), envelope.Offset);
        Assert.AreEqual(
            query.IndexOf("MissingProperty", StringComparison.Ordinal) + "MissingProperty".Length,
            envelope.EndOffset);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.AreEqual("MissingProperty", envelope.Arguments["column"]);
    }

    [TestMethod]
    public void StandardAndFromFirstQueries_ShouldProduceEquivalentResults()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("Warsaw", "Poland", 500),
                new BasicEntity("Berlin", "Germany", 250),
                new BasicEntity("Krakow", "Poland", 300)
            ]
        };

        const string standard =
            "select Country, Sum(Population) as Total from #A.entities() " +
            "where Population > 0 group by Country order by Total desc skip 0 take 1";
        const string fromFirst =
            "from #A.entities() where Population > 0 group by Country " +
            "select Country, Sum(Population) as Total order by Total desc skip 0 take 1";

        var standardVm = CreateAndRunVirtualMachine(standard, sources);
        var fromFirstVm = CreateAndRunVirtualMachine(fromFirst, sources);
        var standardTable = TableMaterializationTestHelper.Materialize(standardVm.Run(TestContext.CancellationToken));
        var fromFirstTable = TableMaterializationTestHelper.Materialize(fromFirstVm.Run(TestContext.CancellationToken));

        TableMaterializationTestHelper.AssertColumns(
            standardTable,
            ("Country", typeof(string)),
            ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertColumns(
            fromFirstTable,
            ("Country", typeof(string)),
            ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(standardTable, ["Poland", 800m]);
        TableMaterializationTestHelper.AssertRowsInOrder(fromFirstTable, ["Poland", 800m]);
    }
}
