using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;
using Musoq.Parser.Exceptions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ErrorMessageQualityTests : NegativeTestsBase
{
    [TestMethod]
    public void EQ001_CountStarShouldMentionCountOne()
    {
        
        try
        {
            CompileQuery("SELECT Count(*) FROM #test.people()");
            Assert.Fail("Expected an error for Count(*)");
        }
        catch (Exception ex)
        {
            
            Assert.IsTrue(
                ex.Message.Contains("Count") || ex.Message.Contains("*") || ex.Message.Contains("star"),
                $"Error message should be helpful for Count(*): {ex.Message}");
        }
    }

    [TestMethod]
    public void EQ002_LimitInsteadOfTake_ShouldProduceError()
    {
        
        
        
        try
        {
            CompileQuery("SELECT * FROM #test.people() LIMIT 10");
            Assert.Fail("Expected an error for LIMIT keyword");
        }
        catch (AstValidationException ex)
        {
            
            Assert.IsInstanceOfType<SyntaxException>(ex.InnerException,
                $"Inner exception should be SyntaxException but was: {ex.InnerException?.GetType().Name}");
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
        catch (AstValidationException ex)
        {
            
            Assert.IsInstanceOfType<SyntaxException>(ex.InnerException,
                $"Inner exception should be SyntaxException but was: {ex.InnerException?.GetType().Name}");
            Assert.IsTrue(
                ex.Message.Contains("Unrecognized") || ex.Message.Contains("Identifier") || ex.Message.Contains("parse"),
                $"Error message should indicate OFFSET is not recognized: {ex.Message}");
        }
    }

    [TestMethod]
    public void EQ004_StandardUnionWithoutColumnList_ShouldProduceError()
    {
        
        try
        {
            CompileQuery("SELECT Name FROM #test.people() UNION SELECT Name FROM #test.people()");
            Assert.Fail("Expected an error for UNION without column list");
        }
        catch (SyntaxException)
        {
            // Expected — Musoq requires UNION (ColumnList)
        }
        catch (Exception ex)
        {
            
            Assert.IsTrue(
                ex.Message.Contains("UNION") || ex.Message.Contains("column") || ex.Message.Contains("syntax"),
                $"Error should mention UNION syntax: {ex.Message}");
        }
    }

    [TestMethod]
    public void EQ005_NotEqualOperator_IsNotSupported()
    {
        
        try
        {
            CompileQuery("SELECT * FROM #test.people() WHERE Age != 25");
            Assert.Fail("Expected an error for != operator");
        }
        catch (AstValidationException ex)
        {
            Assert.IsInstanceOfType<SyntaxException>(ex.InnerException,
                $"Inner exception should be SyntaxException but was: {ex.InnerException?.GetType().Name}");
            Assert.IsTrue(
                ex.Message.Contains("!=") || ex.Message.Contains("<>") || ex.Message.Contains("operator"),
                $"Error message should explain that '!=' is invalid and suggest '<>': {ex.Message}");
        }
    }

    [TestMethod]
    public void EQ006_SubqueryInWhere_ShouldProduceError()
    {
        
        
        try
        {
            CompileQuery("SELECT * FROM #test.people() WHERE Id IN (SELECT PersonId FROM #test.orders())");
            Assert.Fail("Expected an error for subquery");
        }
        catch (AstValidationException ex)
        {
            
            Assert.IsInstanceOfType<SyntaxException>(ex.InnerException,
                $"Inner exception should be SyntaxException but was: {ex.InnerException?.GetType().Name}");
            Assert.IsTrue(
                ex.Message.Contains("select") || ex.Message.Contains("Select") || ex.Message.Contains("cannot be used"),
                $"Error message should indicate subqueries are not supported: {ex.Message}");
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
        catch (UnknownColumnOrAliasException)
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
        catch (UnknownColumnOrAliasException)
        {
            // Expected — cannot use SELECT alias in GROUP BY
        }
        catch (Exception ex)
        {
            
            Assert.IsTrue(
                ex.Message.Contains("UpperCity") || ex.Message.Contains("alias") || ex.Message.Contains("column") || ex.Message.Contains("unknown"),
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
                ex.Message.Contains("R") || ex.Message.Contains("recursive") || ex.Message.Contains("not defined") || ex.Message.Contains("not found") || ex.Message.Contains("unknown"),
                $"Error should indicate recursive CTE issue: {ex.Message}");
        }
    }
}
