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
    public TestContext TestContext { get; set; }

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

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind, "Self");
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
        Assert.AreEqual("Karol", table[0].Values[0]);
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
        Assert.AreEqual("Karol", table[0].Values[0]);
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

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind, "Self");
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

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind, "Self");
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
        Assert.AreEqual('K', table[0].Values[0]);
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
        Assert.AreEqual('K', table[0].Values[0]);
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

        Assert.AreEqual("BB", table[0].Values[0]);
    }
}
