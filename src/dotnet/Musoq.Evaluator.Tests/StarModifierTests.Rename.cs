using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class StarModifierTests
{
    [TestMethod]
    public void WhenStarRenameSingleColumn_ShouldChangeOnlyOutputName()
    {
        const string query = "select * rename (Name as EntityName) from #A.entities()";

        var vm = CreateAndRunVirtualMachine(
            query,
            CreateSingleEntitySource(new BasicEntity { Name = "january" }));
        var table = vm.Run(TestContext.CancellationToken);

        var columnNames = table.Columns.Select(c => c.ColumnName).ToArray();
        Assert.Contains("EntityName", columnNames);
        Assert.IsFalse(columnNames.Contains("Name"));

        var idx = System.Array.IndexOf(columnNames, "EntityName");
        Assert.AreEqual("january", table[0].Values[idx]);
    }

    [TestMethod]
    public void WhenStarRenameAfterReplace_ShouldRenameReplacedOutput()
    {
        const string query = "select * replace (Population * 2 as Population) rename (Population as Pop2) from #A.entities()";

        var source = new BasicEntity("january", 50m) { Population = 100m };
        var vm = CreateAndRunVirtualMachine(query, CreateSingleEntitySource(source));
        var table = vm.Run(TestContext.CancellationToken);

        var columnNames = table.Columns.Select(c => c.ColumnName).ToArray();
        Assert.Contains("Pop2", columnNames);
        Assert.IsFalse(columnNames.Contains("Population"));

        var idx = System.Array.IndexOf(columnNames, "Pop2");
        Assert.AreEqual(200m, table[0].Values[idx]);
    }

    [TestMethod]
    public void WhenStarLikeExcludeRename_ShouldComposeInOrder()
    {
        const string query = "select * like 'C%' exclude (Country) replace (City + '-x' as City) rename (City as Location) from #A.entities()";

        var source = new BasicEntity("january", 50m) { City = "London", Country = "UK" };
        var vm = CreateAndRunVirtualMachine(query, CreateSingleEntitySource(source));
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Location", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["London-x"]);
    }

    [TestMethod]
    public void WhenAliasedStarRenameQualifiedSource_ShouldChangeOutputName()
    {
        const string query = "select a.* rename (a.Name as EntityName) from #A.entities() a";

        var vm = CreateAndRunVirtualMachine(
            query,
            CreateSingleEntitySource(new BasicEntity { Name = "january" }));
        var table = vm.Run(TestContext.CancellationToken);

        var columnNames = table.Columns.Select(c => c.ColumnName).ToArray();
        Assert.Contains("EntityName", columnNames);
        Assert.IsFalse(columnNames.Contains("a.Name"));
    }

    [TestMethod]
    public void WhenStarRenameDuplicateSource_ShouldThrow()
    {
        const string query = "select * rename (Name as EntityName, name as OtherName) from #A.entities()";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSingleEntitySource(new BasicEntity("january", 50m))));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3068_StarRenameDuplicateSource, DiagnosticPhase.Bind, "name");
    }

    [TestMethod]
    public void WhenStarRenameDuplicateTarget_ShouldThrow()
    {
        const string query = "select * rename (Name as Label, City as Label) from #A.entities()";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSingleEntitySource(new BasicEntity("january", 50m))));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3069_StarRenameDuplicateTarget, DiagnosticPhase.Bind, "Label");
    }

    [TestMethod]
    public void WhenStarRenameTargetsExistingOutputName_ShouldThrow()
    {
        const string query = "select * rename (Name as City) from #A.entities()";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSingleEntitySource(new BasicEntity("january", 50m))));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3069_StarRenameDuplicateTarget, DiagnosticPhase.Bind, "City");
    }

    [TestMethod]
    public void WhenStarRenameUnknownPostFilterColumn_ShouldThrow()
    {
        const string query = "select * exclude (City) rename (City as Location) from #A.entities()";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateSingleEntitySource(new BasicEntity("january", 50m))));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3070_StarRenameColumnNotFound, DiagnosticPhase.Bind, "City");
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSingleEntitySource(BasicEntity entity)
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [entity] }
        };
    }
}
