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
        var table = vm.Run();

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
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        // For int array out of bounds, should return default(int) = 0
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
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        // For negative index -1, should return last element of array [0, 1, 2] = 2
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
        var table = vm.Run();

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
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        // For char out of bounds, should return default(char) = '\0'
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
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        // For null string access, should return default(char) = '\0'
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
        var table = vm.Run();

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
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        // For invalid dictionary key, should return null for reference types
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
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        // For empty string access, should return default(char) = '\0'
        Assert.AreEqual('\0', (char)table[0].Values[0]);
    }

    [TestMethod]
    public void ArrayAccess_ZeroLengthArray_ShouldReturnDefault()
    {
        // This test would need a BasicEntity with an empty array property
        // For now, we'll test with a regular array out-of-bounds case
        var query = @"select Self.Array[3] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]} // Array is [0, 1, 2], so index 3 is out of bounds
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        // For out of bounds array access, should return default(int) = 0
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
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        // For aliased character access out of bounds, should return default(char) = '\0'
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
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        
        // Valid indices should return actual values
        Assert.AreEqual(0, (int)table[0].Values[0]); // Array[0] = 0
        Assert.AreEqual(1, (int)table[0].Values[1]); // Array[1] = 1
        Assert.AreEqual(2, (int)table[0].Values[2]); // Array[2] = 2
        
        // Out of bounds index should return default
        Assert.AreEqual(0, (int)table[0].Values[3]); // Array[10] = default(int) = 0
    }

    [TestMethod]
    public void ArrayAccess_NegativeIndexWrapping_ShouldHandleCorrectly()
    {
        var query = @"select Self.Array[-1], Self.Array[-2], Self.Array[-3], Self.Array[-100] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("001")]} // Array is [0, 1, 2] with length 3
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        
        // Negative indices should wrap around correctly
        Assert.AreEqual(2, (int)table[0].Values[0]); // Array[-1] = last element = 2
        Assert.AreEqual(1, (int)table[0].Values[1]); // Array[-2] = second to last = 1
        Assert.AreEqual(0, (int)table[0].Values[2]); // Array[-3] = third to last = 0
        
        // Large negative index should wrap around: -100 % 3 = -1, (-1 + 3) % 3 = 2, so index 2
        Assert.AreEqual(2, (int)table[0].Values[3]); // Array[-100] wraps to index 2 = 2
    }

    [TestMethod]
    public void StringCharacterAccess_SingleNegativeIndex_ShouldReturnLastCharacter()
    {
        var query = @"select Name[-1] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("david")]} // String is "david" with length 5
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        
        // Negative indices should work for strings too
        Assert.AreEqual('d', (char)table[0].Values[0]); // Name[-1] = last character = 'd'
    }

    [TestMethod]
    public void StringCharacterAccess_NegativeIndex_ShouldReturnLastCharacter()
    {
        var query = @"select Name[-1], Name[-2] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("david")]} // String is "david" with length 5
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        
        // Negative indices should work for strings too
        Assert.AreEqual('d', (char)table[0].Values[0]); // Name[-1] = last character = 'd'
        Assert.AreEqual('i', (char)table[0].Values[1]); // Name[-2] = second to last = 'i'
    }
}
