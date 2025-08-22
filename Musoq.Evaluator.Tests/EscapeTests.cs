using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class EscapeTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenBackslashEscaped_ShouldBePresent()
    {
        const string query = """select '\\' from @A.entities()""";
        
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(@"\", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenDoubleBackslashEscaped_ShouldBeSingleBackslash()
    {
        const string query = """select '\\\\' from @A.entities()""";
        
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(@"\\", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenQuoteEscaped_ShouldBePresent()
    {
        const string query = """select '\'' from @A.entities()""";
        
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("'", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenNewlineEscaped_ShouldBePresent()
    {
        const string query = """select '\n' from @A.entities()""";
        
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("\n", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenTabEscaped_ShouldBePresent()
    {
        const string query = """select '\t' from @A.entities()""";
        
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("\t", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenCarriageReturnEscaped_ShouldBePresent()
    {
        const string query = """select '\r' from @A.entities()""";
        
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("\r", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenUnicodeEscaped_ShouldBePresent()
    {
        const string query = """select '\u0041' from @A.entities()""";
        
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("A", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenHexEscaped_ShouldBePresent()
    {
        const string query = """select '\x41' from @A.entities()""";
        
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("A", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenComplexMixedEscapes_ShouldBePresent()
    {
        const string query = """select 'Hello\nWorld\t\u0394\\test' from @A.entities()""";
        
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("Hello\nWorld\tΔ\\test", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenEscapeAtStartAndEnd_ShouldBePresent()
    {
        const string query = """select '\\test\\' from @A.entities()""";
        
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(@"\test\", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenMultipleConsecutiveBackslashes_ShouldBePresent()
    {
        const string query = """select '\\\\\\\\' from @A.entities()""";
        
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(@"\\\\", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenSpecialCharactersEscaped_ShouldBePresent()
    {
        const string query = """select '\0\b\f\e' from @A.entities()""";
        
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("\0\b\f\u001B", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenUnknownEscapeSequence_ShouldRemoveBackslash()
    {
        const string query = """select '\z\y\x' from @A.entities()""";
        
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("\\z\\y\\x", table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenQuoteWithBackslashCombinations_ShouldBePresent()
    {
        // Testing various combinations of quotes and backslashes
        const string query = """select '\\\'' from @A.entities()""";  // Escaped backslash followed by escaped quote
    
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
    
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(@"\'", table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenInvalidUnicodeSequences_ShouldHandleGracefully()
    {
        // Incomplete or invalid unicode sequences
        const string query = """select '\u123' from @A.entities()""";  // Incomplete unicode
    
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
    
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("\\u123", table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenMultipleEscapedColumns_ShouldAllBeHandledCorrectly()
    {
        const string query = """select '\\', '\n', '\u0041' from @A.entities()""";
    
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
    
        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual(@"\", table[0].Values[0]);
        Assert.AreEqual("\n", table[0].Values[1]);
        Assert.AreEqual("A", table[0].Values[2]);
    }
    
    [TestMethod]
    public void WhenConcatenatingEscapedStrings_ShouldBeHandledCorrectly()
    {
        const string query = """select '\\' + '\n' + '\u0041' from @A.entities()""";
    
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
    
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("\\\nA", table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenUsingExtendedUnicode_ShouldBeHandledCorrectly()
    {
        // Testing surrogate pairs and other special Unicode characters
        const string query = """select '\u0001\uFFFF\u0000' from @A.entities()""";
    
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
    
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual("\u0001\uFFFF\u0000", table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenUsingLongStringWithEscapes_ShouldBeHandledCorrectly()
    {
        var longString = string.Join("", Enumerable.Repeat(@"\\\'\n\t", 1000));
        var query = $"""select '{longString}' from @A.entities()""";
    
        var sources = CreateSource();
        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
    
        Assert.AreEqual(1, table.Columns.Count());
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSource()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "@A", [
                    new BasicEntity("test")
                ]
            }
        };
    }
}