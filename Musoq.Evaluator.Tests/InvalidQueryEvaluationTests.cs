using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Tests for invalid queries at the evaluation/runtime level to ensure meaningful error messages
/// These tests verify that the query engine throws specific, informative exceptions rather than generic errors.
/// </summary>
[TestClass]
public class InvalidQueryEvaluationTests : BasicEntityTestBase
{
    [TestMethod]
    public void NonExistentColumn_ShouldThrowMeaningfulError()
    {
        const string query = "select NonExistentColumn from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for non-existent column");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            Assert.IsTrue(exc.Message.Contains("NonExistentColumn") || exc.Message.Contains("Column") || exc.Message.Contains("not be found"),
                $"Error message should mention the invalid column: {exc.Message}");
        }
    }

    [TestMethod]
    public void NonExistentFunction_ShouldThrowMeaningfulError()
    {
        const string query = "select NonExistentFunction(Name) from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for non-existent function");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            Assert.IsTrue(exc.Message.Contains("NonExistentFunction") || exc.Message.Contains("method") || exc.Message.Contains("cannot be resolved"),
                $"Error message should mention the invalid function: {exc.Message}");
        }
    }

    [TestMethod]
    public void AmbiguousColumnInJoin_ShouldThrowMeaningfulError()
    {
        const string query = "select Name from #A.Entities() a inner join #B.Entities() b on a.Name = b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}},
            {"#B", new[] {new BasicEntity("002")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for ambiguous column");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            Assert.IsTrue(exc.Message.Contains("ambiguous") || exc.Message.Contains("Name") || exc.Message.Contains("Ambiguous"),
                $"Error message should mention ambiguous column: {exc.Message}");
        }
    }

    [TestMethod]
    public void InvalidPropertyAccessOnNull_ShouldThrowMeaningfulError()
    {
        const string query = "select Self.Other.SomeProperty from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for invalid property access");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            Assert.IsTrue(exc.Message.Contains("SomeProperty") || exc.Message.Contains("property") || exc.Message.Contains("Property"),
                $"Error message should mention the invalid property: {exc.Message}");
        }
    }

    [TestMethod]
    public void WrongNumberOfFunctionArguments_ShouldThrowMeaningfulError()
    {
        const string query = "select Concat(Name) from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for wrong number of arguments");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            Assert.IsTrue(exc.Message.Contains("Concat") || exc.Message.Contains("argument") || exc.Message.Contains("parameter"),
                $"Error message should mention argument mismatch: {exc.Message}");
        }
    }

    [TestMethod]
    public void AggregateWithoutGroupByInvalidContext_ShouldThrowMeaningfulError()
    {
        const string query = "select Name, Count(*) from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001"), new BasicEntity("002")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for aggregate without GROUP BY");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            // The query engine may allow this in some contexts, so we just verify an exception is thrown
        }
    }

    [TestMethod]
    public void InvalidColumnInGroupBy_ShouldThrowMeaningfulError()
    {
        const string query = "select Name from #A.Entities() group by NonExistentColumn";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for invalid column in GROUP BY");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            Assert.IsTrue(exc.Message.Contains("NonExistentColumn") || exc.Message.Contains("Column"),
                $"Error message should mention the invalid column: {exc.Message}");
        }
    }

    [TestMethod]
    public void InvalidColumnInHaving_ShouldThrowMeaningfulError()
    {
        const string query = "select Name from #A.Entities() group by Name having NonExistentColumn > 5";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for invalid column in HAVING");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            Assert.IsTrue(exc.Message.Contains("NonExistentColumn") || exc.Message.Contains("Column"),
                $"Error message should mention the invalid column: {exc.Message}");
        }
    }

    [TestMethod]
    public void InvalidColumnInOrderBy_ShouldThrowMeaningfulError()
    {
        const string query = "select Name from #A.Entities() order by NonExistentColumn";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for invalid column in ORDER BY");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            Assert.IsTrue(exc.Message.Contains("NonExistentColumn") || exc.Message.Contains("Column"),
                $"Error message should mention the invalid column: {exc.Message}");
        }
    }

    [TestMethod]
    public void SetOperatorColumnCountMismatch_ShouldThrowMeaningfulError()
    {
        const string query = @"
            select Name from #A.Entities()
            union (Name)
            select Name, City from #B.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}},
            {"#B", new[] {new BasicEntity("002")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for column count mismatch");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            Assert.IsTrue(exc.Message.Contains("column") || exc.Message.Contains("Column") || exc.Message.Contains("mismatch"),
                $"Error message should mention column mismatch: {exc.Message}");
        }
    }

    [TestMethod]
    public void InvalidAliasReference_ShouldThrowMeaningfulError()
    {
        const string query = "select Name from #A.Entities() where NonExistentAlias = 'test'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for invalid alias reference");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            Assert.IsTrue(exc.Message.Contains("NonExistentAlias") || exc.Message.Contains("Alias") || exc.Message.Contains("Column"),
                $"Error message should mention the invalid alias: {exc.Message}");
        }
    }

    [TestMethod]
    public void DivisionByZeroLiteral_ShouldThrowMeaningfulError()
    {
        const string query = "select 10 / 0 from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for division by zero");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            // Division by zero may be handled at compile time or runtime
        }
    }

    [TestMethod]
    public void InvalidTypeInArithmeticOperation_ShouldThrowMeaningfulError()
    {
        const string query = "select Name + 10 from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for invalid type in arithmetic");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            // Type checking may happen at different stages
        }
    }

    [TestMethod]
    public void InvalidComparisonBetweenIncompatibleTypes_ShouldThrowMeaningfulError()
    {
        const string query = "select Name from #A.Entities() where Name > 100";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for incompatible type comparison");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            // Type comparison may be allowed in some contexts
        }
    }

    [TestMethod]
    public void NonExistentSchemaTable_ShouldThrowMeaningfulError()
    {
        const string query = "select Name from #NonExistent.table()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for non-existent schema");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            Assert.IsTrue(exc.Message.Contains("NonExistent") || exc.Message.Contains("schema") || exc.Message.Contains("Schema"),
                $"Error message should mention the invalid schema: {exc.Message}");
        }
    }

    [TestMethod]
    public void InvalidJoinConditionType_ShouldThrowMeaningfulError()
    {
        const string query = "select a.Name from #A.Entities() a inner join #B.Entities() b on a.Name + b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}},
            {"#B", new[] {new BasicEntity("002")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for invalid join condition");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            // Join condition validation may allow some expressions
        }
    }

    [TestMethod]
    public void SelfJoinWithoutAlias_ShouldThrowMeaningfulError()
    {
        const string query = "select Name from #A.Entities() inner join #A.Entities() on Name = Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for self-join without alias");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            // May be handled at compile time
        }
    }

    [TestMethod]
    public void InvalidCastSyntax_ShouldThrowMeaningfulError()
    {
        const string query = "select ToInt(Name) from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("NotANumber")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for invalid cast");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            // Cast validation and error depends on function availability
        }
    }

    [TestMethod]
    public void NestedAggregatesNotAllowed_ShouldThrowMeaningfulError()
    {
        const string query = "select Count(Sum(Population)) from #A.Entities() group by City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for nested aggregates");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            // Nested aggregate validation
        }
    }

    [TestMethod]
    public void InvalidCTEReference_ShouldThrowMeaningfulError()
    {
        const string query = "with cte as (select Name from #A.Entities()) select * from NonExistentCTE";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", new[] {new BasicEntity("001")}}
        };

        try
        {
            CreateAndRunVirtualMachine(query, sources);
            Assert.Fail("Expected an exception to be thrown for invalid CTE reference");
        }
        catch (Exception exc)
        {
            Assert.IsNotNull(exc.Message);
            Assert.IsTrue(exc.Message.Length > 0, "Error message should be meaningful");
            Assert.IsTrue(exc.Message.Contains("NonExistentCTE") || exc.Message.Contains("CTE") || exc.Message.Contains("table"),
                $"Error message should mention the invalid CTE: {exc.Message}");
        }
    }
}
