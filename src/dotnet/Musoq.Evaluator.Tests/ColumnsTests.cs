using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ColumnsTests : BasicEntityTestBase
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0207")]
    public void ContextualExcludeColumn_ShouldAllowItemOutputAlias()
    {
        const string query = "select Exclude as Item from values { (Exclude: 7) } p";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Item", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 7 });
    }

    [TestMethod]
    [TestProperty("CandidateId", "P1-020")]
    public void JoinedQualifiedColumnsUseUnqualifiedOutputNames()
    {
        const string query = "select l.Id, r.Name from values { (Id: 1), (Id: 2) } l inner join values { (Id: 2, Name: 'match'), (Id: 3, Name: 'other') } r on l.Id = r.Id";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [2, "match"]);
        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Name", typeof(string)));
    }

    [TestMethod]
    [DataRow("select v.N from values { (N: 1) } v", "N")]
    [DataRow("select p.Value from values { (Value: 1) } p", "Value")]
    public void QualifiedInlineColumnsUseUnqualifiedOutputNames(string query, string columnName)
    {
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [1]);
        TableMaterializationTestHelper.AssertColumns(table, (columnName, typeof(int)));
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P5-0035")]
    public void QualifiedAndUnqualifiedEntityColumnsHaveTheSameOutputNames()
    {
        const string query = "select e.Name\r\nfrom #A.entities() e";
        const string unqualifiedQuery = "select Name from #A.entities() e";
        var entities = new[] { new BasicEntity { Name = "alpha" } };
        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = entities, ["A"] = entities };

        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var table = compiled.Run(TestContext.CancellationToken);
        using var unqualifiedCompiled = CreateAndRunVirtualMachine(unqualifiedQuery, sources);
        using var unqualifiedTable = unqualifiedCompiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["alpha"]);
        TableMaterializationTestHelper.AssertRowsUnordered(unqualifiedTable, ["alpha"]);
        TableMaterializationTestHelper.AssertColumns(unqualifiedTable, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
    }

    [TestMethod]
    public void WhenComplexObjectAccessNonExistingProperty_ShouldFail()
    {
        const string query = @"select Self.NonExistingProperty from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind, "NonExistingProperty");
    }

    [TestMethod]
    public void WhenComplexObjectAccessOnProperty_ShouldPass()
    {
        const string query = @"select Self.Name from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Karol")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Self.Name", table.Columns.ElementAt(0).ColumnName);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Karol", table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenChainedComplexObjectAccessOnProperty_ShouldPass()
    {
        const string query = @"select Self.Self.Name from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Karol")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Self.Self.Name", table.Columns.ElementAt(0).ColumnName);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Karol", table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenEntityDoesNotImplementIndexer_ShouldFail()
    {
        const string query = @"select Self['NonExistingProperty'] from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3018_NoIndexer, DiagnosticPhase.Bind, "Self");
    }

    [TestMethod]
    public void WhenNestedObjectDoesNotImplementIndexer_ShouldFail()
    {
        const string query = @"select Self.Other['NonExistingProperty'] from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind, "Self");
    }

    [TestMethod]
    public void WhenNestedObjectUsesNotSupportedConstruction_ShouldFail()
    {
        const string query = @"select Self.Other[NonExistingProperty] from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind, "Other[NonExistingProperty]");
    }

    [TestMethod]
    public void WhenObjectIsNotArray_ShouldFail()
    {
        const string query = @"select Self[0] from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3017_ObjectNotArray, DiagnosticPhase.Bind, "Self");
    }

    [TestMethod]
    public void WhenNestedObjectIsNotArray_ShouldFail()
    {
        const string query = @"select Self.Other[0] from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", []
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3017_ObjectNotArray, DiagnosticPhase.Bind, "Other");
    }

    [TestMethod]
    public void WhenNestedObjectMightBeTreatAsArray_ShouldPass()
    {
        const string query = @"select Self.Name[0] from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Karol")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Self.Name[0]", table.Columns.ElementAt(0).ColumnName);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual('K', table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenDoubleNestedObjectMightBeTreatAsArray_ShouldPass()
    {
        const string query = @"select Self.Self.Name[0] from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Karol")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Self.Self.Name[0]", table.Columns.ElementAt(0).ColumnName);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual('K', table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void WhenObjectIsNotArrayAndIndexIsNotNumber_ShouldPass()
    {
        const string query = @"select Self.Dictionary['AA'] from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity()
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Self.Dictionary[AA]", table.Columns.ElementAt(0).ColumnName);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual("BB", table[0].ReadValues()[0]);
    }
}
