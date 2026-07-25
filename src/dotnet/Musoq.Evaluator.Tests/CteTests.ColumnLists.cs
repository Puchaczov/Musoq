using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    [TestMethod]
    public void CteColumnList_ShouldOverrideInnerAliasesPositionally()
    {
        const string query =
            "with places (Name, Nation) as (select City as InnerCity, Country as InnerCountry from #A.entities()) " +
            "select Nation, Name from places";

        var vm = CreateAndRunVirtualMachine(query, CreateColumnListSources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Nation", typeof(string)),
            ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["POLAND", "WARSAW"],
            ["GERMANY", "BERLIN"]);
    }

    [TestMethod]
    public void CteColumnList_WithSetOperation_ShouldExportNamesFromFirstProjection()
    {
        const string query =
            "with places (Name, Nation) as (" +
            "select City, Country from #A.entities() " +
            "union all select City, Country from #B.entities()) " +
            "select Name, Nation from places order by Name";

        var sources = CreateColumnListSources();
        sources["#B"] = [new BasicEntity("PRAGUE", "CZECHIA", 300)];

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Nation", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["BERLIN", "GERMANY"],
            ["PRAGUE", "CZECHIA"],
            ["WARSAW", "POLAND"]);
    }

    [TestMethod]
    public void CteColumnList_WithWrongCount_ShouldReportMq3077()
    {
        const string query =
            "with places (Name) as (select City, Country from #A.entities()) select Name from places";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateColumnListSources()));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3077_CteColumnListCountMismatch, DiagnosticPhase.Bind);
        StringAssert.Contains(exception.Message, "declares 1 column name(s)");
        StringAssert.Contains(exception.Message, "projects 2 column(s)");
    }

    [TestMethod]
    public void CteColumnList_WithExactDuplicateName_ShouldReportMq3078()
    {
        const string query =
            "with places (Name, Name) as (select City, Country from #A.entities()) select Name from places";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateColumnListSources()));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3078_DuplicateCteColumnName, DiagnosticPhase.Bind);
        StringAssert.Contains(exception.Message, "duplicate column name 'Name'");
    }

    [TestMethod]
    public void CteNamedRecursive_ShouldExecuteAsOrdinaryCte()
    {
        const string query =
            "with recursive as (select City from #A.entities()) select City from recursive order by City";

        var vm = CreateAndRunVirtualMachine(query, CreateColumnListSources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["BERLIN"], ["WARSAW"]);
    }

    [TestMethod]
    public void CteColumnList_WithCaseOnlyDuplicateName_ShouldReportMq3078()
    {
        const string query =
            "with places (Name, name) as (select City, Country from #A.entities()) select Name from places";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateColumnListSources()));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3078_DuplicateCteColumnName, DiagnosticPhase.Bind);
        StringAssert.Contains(exception.Message, "duplicate column name 'name'");
    }

    [TestMethod]
    public void RecursiveCteColumnList_WithCaseOnlyDuplicateName_ShouldReportMq3078()
    {
        const string query =
            "with recursive counter (Id, id) as (" +
            "select A, B from values {{ A: 1, B: 10 }} seed union all " +
            "select c.Id + 1, c.id + 10 from counter c where c.Id < 2) " +
            "select Id, id from counter";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>()));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3078_DuplicateCteColumnName, DiagnosticPhase.Bind);
        StringAssert.Contains(exception.Message, "duplicate column name 'id'");
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateColumnListSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("WARSAW", "POLAND", 500),
                new BasicEntity("BERLIN", "GERMANY", 250)
            ]
        };
    }
}
