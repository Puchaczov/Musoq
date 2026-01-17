using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ArrayAccessEdgeCaseTests : BasicEntityTestBase
{
    [TestMethod]
    public void ArrayAccess_ValidIndex_ShouldReturnValue()
    {
        var query = @"select Self.Array[2] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, (int)table[0].Values[0]);
    }

    [TestMethod] 
    public void ArrayAccess_OutOfBounds_ShouldReturnDefault()
    {
        var query = @"select Self.Array[10] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual(0, (int)table[0].Values[0]);
    }

    [TestMethod]
    public void ArrayAccess_NegativeIndex_ShouldReturnLastElement()
    {
        var query = @"select Self.Array[-1] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual(2, (int)table[0].Values[0]);
    }

    [TestMethod]
    public void StringCharacterAccess_ValidIndex_ShouldReturnChar()
    {
        var query = @"select Name[0] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("david")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual('d', (char)table[0].Values[0]);
    }

    [TestMethod]
    public void StringCharacterAccess_OutOfBounds_ShouldReturnDefaultChar()
    {
        var query = @"select Name[100] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("david")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual('\0', (char)table[0].Values[0]);
    }

    [TestMethod]
    public void StringCharacterAccess_NullString_ShouldReturnDefaultChar()
    {
        var query = @"select Name[0] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity() { Name = null }]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual('\0', (char)table[0].Values[0]);
    }

    [TestMethod]
    public void DictionaryAccess_ValidKey_ShouldReturnValue()
    {
        var query = @"select Self.Dictionary['A'] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("B", (string)table[0].Values[0]);
    }

    [TestMethod]
    public void DictionaryAccess_InvalidKey_ShouldReturnNull()
    {
        var query = @"select Self.Dictionary['Z'] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        
        Assert.IsNull(table[0].Values[0]);
    }

    [TestMethod]
    public void StringCharacterAccess_EmptyString_ShouldReturnDefaultChar()
    {
        var query = @"select Name[0] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual('\0', (char)table[0].Values[0]);
    }

    [TestMethod]
    public void ArrayAccess_ZeroLengthArray_ShouldReturnDefault()
    {
        
        
        var query = @"select Self.Array[3] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]} 
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual(0, (int)table[0].Values[0]);
    }

    [TestMethod]
    public void StringCharacterAccess_AliasedColumn_OutOfBounds_ShouldReturnDefaultChar()
    {
        var query = @"select f.Name[100] from #A.Entities() f";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("david")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual('\0', (char)table[0].Values[0]);
    }

    [TestMethod]
    public void ArrayAccess_MultipleDifferentIndices_ShouldHandleCorrectly()
    {
        var query = @"select Self.Array[0], Self.Array[1], Self.Array[2], Self.Array[10] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        
        
        Assert.AreEqual(0, (int)table[0].Values[0]); 
        Assert.AreEqual(1, (int)table[0].Values[1]); 
        Assert.AreEqual(2, (int)table[0].Values[2]); 
        
        
        Assert.AreEqual(0, (int)table[0].Values[3]); 
    }

    [TestMethod]
    public void ArrayAccess_NegativeIndexWrapping_ShouldHandleCorrectly()
    {
        var query = @"select Self.Array[-1], Self.Array[-2], Self.Array[-3], Self.Array[-100] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]} 
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        
        
        Assert.AreEqual(2, (int)table[0].Values[0]); 
        Assert.AreEqual(1, (int)table[0].Values[1]); 
        Assert.AreEqual(0, (int)table[0].Values[2]); 
        
        
        Assert.AreEqual(2, (int)table[0].Values[3]); 
    }

    [TestMethod]
    public void StringCharacterAccess_SingleNegativeIndex_ShouldReturnLastCharacter()
    {
        var query = @"select Name[-1] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("david")]} 
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        
        
        Assert.AreEqual('d', (char)table[0].Values[0]); 
    }

    [TestMethod]
    public void StringCharacterAccess_NegativeIndex_ShouldReturnLastCharacter()
    {
        var query = @"select Name[-1], Name[-2] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("david")]} 
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        
        
        Assert.AreEqual('d', (char)table[0].Values[0]); 
        Assert.AreEqual('i', (char)table[0].Values[1]); 
    }

    public TestContext TestContext { get; set; }
}
