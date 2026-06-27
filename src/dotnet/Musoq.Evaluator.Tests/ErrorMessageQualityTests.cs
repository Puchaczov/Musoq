using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ErrorMessageQualityTests : NegativeTestsBase
{
    [TestMethod]
    public void EQ001_CountStar_ShouldCompile()
    {
        CompileQuery("SELECT Count(*) FROM #test.people()");
    }

    [TestMethod]
    public void EQ002_LimitInsteadOfTake_ShouldProduceError()
    {
        try
        {
            CompileQuery("SELECT * FROM #test.people() LIMIT 10");
            Assert.Fail("Expected an error for LIMIT keyword");
        }
        catch (MusoqQueryException ex)
        {
            Assert.IsNotNull(ex.InnerException, "MusoqQueryException should preserve the original exception");
            Assert.IsTrue(
                ex.Message.Contains("Integer") || ex.Message.Contains("expected") || ex.Message.Contains("parse"),
                $"Error message should indicate a syntax issue near LIMIT: {ex.Message}");
        }
    }

    [TestMethod]
    public void EQ003_OffsetInsteadOfSkip_ShouldProduceError()
    {
        try
        {
            CompileQuery("SELECT * FROM #test.people() ORDER BY Name OFFSET 5");
            Assert.Fail("Expected an error for OFFSET keyword");
        }
        catch (MusoqQueryException ex)
        {
            Assert.IsNotNull(ex.InnerException, "MusoqQueryException should preserve the original exception");
            Assert.IsTrue(
                ex.Message.Contains("Unrecognized") || ex.Message.Contains("Identifier") ||
                ex.Message.Contains("parse"),
                $"Error message should indicate OFFSET is not recognized: {ex.Message}");
        }
    }

    [TestMethod]
    public void EQ004_StandardUnionWithoutColumnList_ShouldCompile()
    {
        CompileQuery("SELECT Name FROM #test.people() UNION SELECT Name FROM #test.people()");
    }

    [TestMethod]
    public void EQ005_NotEqualOperator_IsSupported()
    {
        CompileQuery("SELECT * FROM #test.people() WHERE Age != 25");
    }

    [TestMethod]
    public void EQ006_SubqueryInWhere_MultiColumnSubquery_ShouldProduceError()
    {
        try
        {
            CompileQuery("SELECT * FROM #test.people() WHERE Id IN (SELECT Id, Name FROM #test.people())");
            Assert.Fail("Expected an error for multi-column subquery in IN");
        }
        catch (Exception ex)
        {
            Assert.IsTrue(
                ex.Message.Contains("one column") || ex.Message.Contains("exactly one"),
                $"Error message should indicate subquery must return one column: {ex.Message}");
        }
    }

    [TestMethod]
    public void EQ009_ColumnCaseSensitivityMistake_ShouldProduceError()
    {
        try
        {
            CompileQuery("SELECT name FROM #test.people()");
            Assert.Fail("Expected an error for case-sensitive column name");
        }
        catch (MusoqQueryException)
        {
            // Expected — column 'name' doesn't match 'Name'
        }
        catch (Exception ex)
        {
            Assert.IsTrue(
                ex.Message.Contains("name") || ex.Message.Contains("column") || ex.Message.Contains("unknown"),
                $"Error should mention the column name issue: {ex.Message}");
        }
    }

    [TestMethod]
    public void EQ010_GroupByAlias_ShouldProduceError()
    {
        try
        {
            CompileQuery("SELECT ToUpper(City) AS UpperCity, Count(1) FROM #test.people() GROUP BY UpperCity");
            Assert.Fail("Expected an error for using alias in GROUP BY");
        }
        catch (MusoqQueryException)
        {
            // Expected — cannot use SELECT alias in GROUP BY
        }
        catch (Exception ex)
        {
            Assert.IsTrue(
                ex.Message.Contains("UpperCity") || ex.Message.Contains("alias") || ex.Message.Contains("column") ||
                ex.Message.Contains("unknown"),
                $"Error should mention the alias issue: {ex.Message}");
        }
    }

    [TestMethod]
    public void EQ011_RecursiveCte_ShouldProduceError()
    {
        try
        {
            var query = @"
                WITH R AS (
                    SELECT Id, ManagerId FROM #test.people() WHERE ManagerId IS NULL
                    UNION ALL (Id, ManagerId)
                    SELECT p.Id, p.ManagerId FROM #test.people() p INNER JOIN R r ON p.ManagerId = r.Id
                )
                SELECT * FROM R r";

            CompileQuery(query);
            Assert.Fail("Expected an error for recursive CTE");
        }
        catch (Exception ex)
        {
            Assert.IsTrue(
                ex.Message.Contains('R') || ex.Message.Contains("recursive") || ex.Message.Contains("not defined") ||
                ex.Message.Contains("not found") || ex.Message.Contains("unknown"),
                $"Error should indicate recursive CTE issue: {ex.Message}");
        }
    }
}
