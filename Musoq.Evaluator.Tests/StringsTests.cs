using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class StringsTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenQuoteUsed_MustNotThrow()
    {
        var query = """select '"' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        
        Assert.AreEqual("\"", table[0].Values[0]);
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual("\"", table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenQuotePrecededByTextUsed_MustNotThrow()
    {
        var query = """select 'text "' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
     
        Assert.AreEqual("text \"", table[0].Values[0]);
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual("text \"", table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenQuoteFollowedByTextUsed_MustNotThrow()
    {
        var query = """select '"text' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        
        Assert.AreEqual("\"text", table[0].Values[0]);
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual("\"text", table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenQuoteFollowedAndPrecededByTextUsed_MustNotThrow()
    {
        var query = """select '"text"' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        
        Assert.AreEqual("\"text\"", table[0].Values[0]);
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual("\"text\"", table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenEscapeCharacterUsed_MustNotThrow()
    {
        const string query = """select '\'' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        
        Assert.AreEqual("'", table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenEscapeCharacterUsedInText_MustNotThrow()
    {
        const string query = """select 'text \'' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        
        Assert.AreEqual("text '", table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenMultipleEscapeCharactersUsedInText_MustNotThrow()
    {
        const string query = """select 'lorem\' ipsum dolor\'' from #A.entities()""";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        
        Assert.AreEqual("lorem' ipsum dolor'", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenMultipleEscapeCharactersUsedInTextWithQuote_MustNotThrow()
    {
        const string query = """select 'lorem\' " ipsum dolor\'' from #A.entities()""";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        
        Assert.AreEqual("lorem' \" ipsum dolor'", table[0].Values[0]);
    }
    
    [DataRow('{')]
    [DataRow('}')]
    [DataRow('(')]
    [DataRow(')')]
    [DataRow('-')]
    [DataRow('/')]
    [DataRow('*')]
    [DataRow('+')]
    [DataRow('=')]
    [DataRow('!')]
    [DataRow('<')]
    [DataRow('>')]
    [DataRow('&')]
    [DataRow('|')]
    [DataRow('^')]
    [DataRow('%')]
    [DataRow('~')]
    [DataRow('`')]
    [DataRow('[')]
    [DataRow(']')]
    [DataRow(';')]
    [DataRow(':')]
    [DataRow(',')]
    [DataRow('.')]
    [DataRow('?')]
    [DataRow('@')]
    [DataRow('#')]
    [DataRow('$')]
    [DataRow(' ')]
    [DataRow('"')]
    [DataTestMethod]
    public void WhenSpecialCharacterStartBracketUsedInTextWith_MustNotThrow(char specialCharacter)
    {
        var query = $"select '{specialCharacter}' from #A.entities()";
        
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("test")
                ]
            }
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        
        Assert.AreEqual(specialCharacter.ToString(), table[0].Values[0]);
    }
}

