using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BetweenTests : BasicEntityTestBase
{
    [TestMethod]
    public void Between_ValueInRange_ReturnsTrue()
    {
        TestMethodTemplate("5 between 1 and 10", true);
    }

    [TestMethod]
    public void Between_ValueBelowRange_ReturnsFalse()
    {
        TestMethodTemplate("0 between 1 and 10", false);
    }

    [TestMethod]
    public void Between_ValueAboveRange_ReturnsFalse()
    {
        TestMethodTemplate("11 between 1 and 10", false);
    }

    [TestMethod]
    public void Between_ValueEqualsMin_ReturnsTrue()
    {
        TestMethodTemplate("1 between 1 and 10", true);
    }

    [TestMethod]
    public void Between_ValueEqualsMax_ReturnsTrue()
    {
        TestMethodTemplate("10 between 1 and 10", true);
    }

    [TestMethod]
    public void Between_WithDecimalValuesInRange_ReturnsTrue()
    {
        TestMethodTemplate("5.5d between 1.0d and 10.0d", true);
    }

    [TestMethod]
    public void Between_WithDecimalValuesBelowRange_ReturnsFalse()
    {
        TestMethodTemplate("0.5d between 1.0d and 10.0d", false);
    }

    [TestMethod]
    public void Between_WithNegativeNumbers_ReturnsTrue()
    {
        TestMethodTemplate("-5 between -10 and 0", true);
    }

    [TestMethod]
    public void Between_WithNegativeNumbersBelowRange_ReturnsFalse()
    {
        TestMethodTemplate("-15 between -10 and 0", false);
    }

    [TestMethod]
    public void Between_WithMixedSignNumbers_ReturnsTrue()
    {
        TestMethodTemplate("0 between -5 and 5", true);
    }
    [TestMethod]
    public void Between_WithArithmeticExpressions_ReturnsTrue()
    {
        TestMethodTemplate("(2 + 3) between 1 and 10", true);
    }

    [TestMethod]
    public void Between_WithArithmeticExpressionMinMax_ReturnsTrue()
    {
        TestMethodTemplate("5 between (1 + 0) and (5 + 5)", true);
    }

    [TestMethod]
    public void Between_CombinedWithAnd_ReturnsTrueWhenBothTrue()
    {
        TestMethodTemplate("5 between 1 and 10 and 3 between 1 and 5", true);
    }

    [TestMethod]
    public void Between_CombinedWithAnd_ReturnsFalseWhenFirstFalse()
    {
        TestMethodTemplate("15 between 1 and 10 and 3 between 1 and 5", false);
    }

    [TestMethod]
    public void Between_CombinedWithAnd_ReturnsFalseWhenSecondFalse()
    {
        TestMethodTemplate("5 between 1 and 10 and 10 between 1 and 5", false);
    }

    [TestMethod]
    public void Between_CombinedWithOr_ReturnsTrueWhenFirstTrue()
    {
        TestMethodTemplate("5 between 1 and 10 or 15 between 1 and 5", true);
    }

    [TestMethod]
    public void Between_CombinedWithOr_ReturnsTrueWhenSecondTrue()
    {
        TestMethodTemplate("15 between 1 and 10 or 3 between 1 and 5", true);
    }

    [TestMethod]
    public void Between_CombinedWithOr_ReturnsFalseWhenBothFalse()
    {
        TestMethodTemplate("15 between 1 and 10 or 10 between 1 and 5", false);
    }

    [TestMethod]
    public void Between_UppercaseKeyword_Works()
    {
        TestMethodTemplate("5 BETWEEN 1 AND 10", true);
    }

    [TestMethod]
    public void Between_MixedCaseKeyword_Works()
    {
        TestMethodTemplate("5 Between 1 And 10", true);
    }

    [TestMethod]
    public void Between_InWhereClause_FiltersCorrectly()
    {
        var query = "select Name from #A.Entities() where Population between 100000 and 500000";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", new[] 
                { 
                    new BasicEntity("Small") { Population = 50000 },
                    new BasicEntity("Medium") { Population = 200000 },
                    new BasicEntity("Large") { Population = 1000000 },
                    new BasicEntity("Tiny") { Population = 100000 },
                    new BasicEntity("Huge") { Population = 500000 }
                } 
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count); // Medium, Tiny, Huge (inclusive range)
    }

    [TestMethod]
    public void Between_InSelectClause_ReturnsBoolean()
    {
        var query = "select 5 between 1 and 10 as Result from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", new[] { new BasicEntity("test") } }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.IsTrue((bool)table[0][0]);
    }

    [TestMethod]
    public void Between_WithColumnReference_FiltersCorrectly()
    {
        var query = "select Name from #A.Entities() where Population between 100000 and 500000";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", new[] 
                { 
                    new BasicEntity("Small") { Population = 50000 },
                    new BasicEntity("Medium") { Population = 200000 },
                    new BasicEntity("Large") { Population = 1000000 }
                } 
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Medium", table[0][0]);
    }

    [TestMethod]
    public void Between_WithMultipleConditions_FiltersCorrectly()
    {
        var query = "select Name from #A.Entities() where Population between 100000 and 500000 and City = 'Boston'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", new[] 
                { 
                    new BasicEntity("City1") { Population = 200000, City = "Boston" },
                    new BasicEntity("City2") { Population = 200000, City = "Denver" },
                    new BasicEntity("City3") { Population = 50000, City = "Boston" }
                } 
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("City1", table[0][0]);
    }
}
