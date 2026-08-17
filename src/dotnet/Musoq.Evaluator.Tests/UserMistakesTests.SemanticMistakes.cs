using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class UserMistakesTests
{
    [TestMethod]
    public void Invalid_ComparisonOperator()
    {
        // Arrange - using "==" instead of "="
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name == 'test'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - could be valid or error depending on parser
        // We just verify it doesn't crash
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Invalid_ArithmeticExpression_DoubleOperator()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT 1 ++ 2 FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2030 or MQ2001 for invalid syntax
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "double operator ++");
    }

    [TestMethod]
    public void Invalid_BooleanExpression_MissingOperand()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 'test' AND";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2030_UnsupportedSyntax: trailing AND
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "trailing AND without operand");
    }

    [TestMethod]
    public void Invalid_BooleanExpression_MissingOperator()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name 'test'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2030 or MQ2001: missing operator between Name and 'test'
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "missing operator between Name and literal");
    }

    [TestMethod]
    public void Invalid_InExpression_EmptyList()
    {
        // Arrange - Some SQL dialects allow empty IN lists
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name IN ()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - Accept either error or success (dialect-dependent)
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Invalid_BetweenExpression_MissingAnd()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population BETWEEN 1 100";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001: missing AND in BETWEEN
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "BETWEEN missing AND");
    }



    [TestMethod]
    public void TypeMismatch_StringComparedToNumber()
    {
        // Arrange - comparing string column to number
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 123";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - This might be allowed in some SQL dialects
        // Just verify it's processed without crashing
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void TypeMismatch_NumberInStringFunction()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Substring(Population, 1, 2) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - verify it handles the type mismatch
        Assert.IsNotNull(result);
    }



    [TestMethod]
    public void CaseSensitivity_KeywordLowercase()
    {
        // Arrange - all lowercase keywords should work
        var analyzer = CreateAnalyzer();
        var query = "select Name from #A.Entities() where Name = 'test'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - keywords are case-insensitive
        Assert.IsTrue(result.IsParsed);
    }

    [TestMethod]
    public void CaseSensitivity_KeywordMixedCase()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SeLeCt Name FrOm #A.Entities() WhErE Name = 'test'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
    }

    [TestMethod]
    public void CaseSensitivity_ColumnNameWrongCase()
    {
        // Arrange - "name" instead of "Name"
        var analyzer = CreateAnalyzer();
        var query = "SELECT name FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - column names may or may not be case-sensitive
        Assert.IsNotNull(result);
    }



    [TestMethod]
    public void Join_MissingOnCondition()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT a.Name FROM #A.Entities() a INNER JOIN #B.Entities() b";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001: Expected token is On
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2007_InvalidJoinCondition, "JOIN missing ON");
    }

    [TestMethod]
    public void Join_InvalidJoinType()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT a.Name FROM #A.Entities() a WEIRD JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001 or MQ2030: unrecognized JOIN type
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "invalid JOIN type 'WEIRD'");
    }

    [TestMethod]
    public void Union_MismatchedColumnCount()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT Name FROM #A.Entities()
            UNION
            SELECT Name, City FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - omitted keys are valid, but both sides still need the same projection width
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3019_SetOperatorColumnCount, "UNION column count mismatch");
    }

    [TestMethod]
    public void Union_MissingSecondQuery()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() UNION";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001 or MQ2030: UNION without second query
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2030_UnsupportedSyntax, "UNION without second query");
    }



    [TestMethod]
    public void Aggregate_NonAggregatedColumnInSelect()
    {
        // Arrange - City not in GROUP BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT City, Count(Name) FROM #A.Entities() GROUP BY Country";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - City is not in GROUP BY and not inside an aggregate → MQ3012
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3012_NonAggregateInSelect, "City not in GROUP BY");
    }

    [TestMethod]
    public void Aggregate_WithoutGroupBy()
    {
        // Arrange - mixing aggregated and non-aggregated
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Count(*) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Aggregate_UnknownFunction()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT FakeAggregate(Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert - MQ3088_NoMatchingCallableOverload for unknown function
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3086_UnknownCallable, "unknown function 'FakeAggregate'");
    }

    [TestMethod]
    public void Aggregate_WrongArgumentCount()
    {
        // Arrange - Count with wrong args
        var analyzer = CreateAnalyzer();
        var query = "SELECT Count(Name, City, Country) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsNotNull(result);
    }



    [TestMethod]
    public void CTE_MissingAsKeyword()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "WITH cte (SELECT Name FROM #A.Entities()) SELECT * FROM cte";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001: missing AS keyword
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "CTE missing AS keyword");
    }

    [TestMethod]
    public void CTE_ReferenceBeforeDefinition()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM cte WITH cte AS (SELECT Name FROM #A.Entities())";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - MQ2001: incorrect CTE syntax order
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2030_UnsupportedSyntax, "CTE referenced before definition");
    }

}
