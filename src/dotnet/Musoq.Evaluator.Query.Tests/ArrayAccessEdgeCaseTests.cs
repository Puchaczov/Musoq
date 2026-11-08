using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ArrayAccessEdgeCaseTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("select Dictionary['A'] as Value from #A.Entities()", "B")]
    [DataRow("select Dictionary['Z'] as Value from #A.Entities()", null)]
    [DataRow("select e.Dictionary['A'] as Value from #A.Entities() e", "B")]
    [DataRow("select e.Dictionary['Z'] as Value from #A.Entities() e", null)]
    [DataRow("with d as (select Dictionary from #A.Entities()) select d.Dictionary['A'] as Value from d", "B")]
    [DataRow("with d as (select Dictionary from #A.Entities()) select Dictionary['Z'] as Value from d", null)]
    [TestProperty("CandidateId", "P1-069")]
    public void DictionaryColumnAccess_ShouldReturnValueOrNullForMissingKey(string query, string? expected)
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity()]
        };

        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected]);
    }

    [TestMethod]
    public void StringKeyOnNonIndexableColumn_ShouldReportIndexerDiagnostic()
    {
        const string query = "select e.Id['A'] as Value from #A.Entities() e";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity()]
        };

        var failure = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, sources));

        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ3018_NoIndexer,
            failure.PrimaryEnvelope.Code);
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticPhase.Bind, failure.PrimaryEnvelope.Phase);
    }


    [TestMethod]
    public void ArrayAccess_ValidIndex_ShouldReturnValue()
    {
        var query = @"select Self.Array[2] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, (int)table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void ArrayAccess_OutOfBounds_ShouldReturnDefault()
    {
        var query = @"select Self.Array[10] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(0, (int)table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void ArrayAccess_NegativeIndex_ShouldReturnLastElement()
    {
        var query = @"select Self.Array[-1] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(2, (int)table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void StringCharacterAccess_ValidIndex_ShouldReturnChar()
    {
        var query = @"select Name[0] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("david")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual('d', (char)table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void StringCharacterAccess_OutOfBounds_ShouldReturnDefaultChar()
    {
        var query = @"select Name[100] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("david")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual('\0', (char)table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void StringCharacterAccess_NullString_ShouldReturnDefaultChar()
    {
        var query = @"select Name[0] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = null }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual('\0', (char)table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void DictionaryAccess_ValidKey_ShouldReturnValue()
    {
        var query = @"select Self.Dictionary['A'] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("B", (string)table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void DictionaryAccess_InvalidKey_ShouldReturnNull()
    {
        var query = @"select Self.Dictionary['Z'] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.IsNull(table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void StringCharacterAccess_EmptyString_ShouldReturnDefaultChar()
    {
        var query = @"select Name[0] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual('\0', (char)table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void ArrayAccess_ZeroLengthArray_ShouldReturnDefault()
    {
        var query = @"select Self.Array[3] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(0, (int)table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void StringCharacterAccess_AliasedColumn_OutOfBounds_ShouldReturnDefaultChar()
    {
        var query = @"select f.Name[100] from #A.Entities() f";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("david")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual('\0', (char)table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void ArrayAccess_MultipleDifferentIndices_ShouldHandleCorrectly()
    {
        var query = @"select Self.Array[0], Self.Array[1], Self.Array[2], Self.Array[10] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);


        Assert.AreEqual(0, (int)table[0].ReadValues()[0]);
        Assert.AreEqual(1, (int)table[0].ReadValues()[1]);
        Assert.AreEqual(2, (int)table[0].ReadValues()[2]);


        Assert.AreEqual(0, (int)table[0].ReadValues()[3]);
    }

    [TestMethod]
    public void ArrayAccess_NegativeIndexWrapping_ShouldHandleCorrectly()
    {
        var query = @"select Self.Array[-1], Self.Array[-2], Self.Array[-3], Self.Array[-100] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);


        Assert.AreEqual(2, (int)table[0].ReadValues()[0]);
        Assert.AreEqual(1, (int)table[0].ReadValues()[1]);
        Assert.AreEqual(0, (int)table[0].ReadValues()[2]);


        Assert.AreEqual(2, (int)table[0].ReadValues()[3]);
    }

    [TestMethod]
    public void StringCharacterAccess_SingleNegativeIndex_ShouldReturnLastCharacter()
    {
        var query = @"select Name[-1] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("david")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);


        Assert.AreEqual('d', (char)table[0].ReadValues()[0]);
    }

    [TestMethod]
    public void StringCharacterAccess_NegativeIndex_ShouldReturnLastCharacter()
    {
        var query = @"select Name[-1], Name[-2] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("david")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);


        Assert.AreEqual('d', (char)table[0].ReadValues()[0]);
        Assert.AreEqual('i', (char)table[0].ReadValues()[1]);
    }
}
