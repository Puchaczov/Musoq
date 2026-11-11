using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class UserMistakesTests
{
    [TestMethod]
    public void Invalid_ComparisonOperator()
    {
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name == 'test'";

        var result = analyzer.ValidateSyntax(query);

        _ = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ2019_InvalidOperator,
            nameof(Invalid_ComparisonOperator)
        );
    }

    [TestMethod]
    public void Invalid_ArithmeticExpression_DoubleOperator()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT 1 ++ 2 FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - the parser identifies the operator's missing left operand.
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2020_MissingOperand, "double operator ++");
    }

    [TestMethod]
    public void Invalid_BooleanExpression_MissingOperand()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 'test' AND";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - trailing AND is a missing right operand.
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2020_MissingOperand, "trailing AND without operand");
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
        AssertHasDiagnosticCode(
            result,
            DiagnosticCode.MQ2001_UnexpectedToken,
            "missing operator between Name and literal"
        );
    }

    [TestMethod]
    public void Invalid_InExpression_EmptyList()
    {
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name IN ()";

        var result = analyzer.ValidateSyntax(query);

        _ = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ2037_EmptyPredicateListNotAllowed,
            nameof(Invalid_InExpression_EmptyList)
        );
    }

    [TestMethod]
    public void Invalid_BetweenExpression_MissingAnd()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population BETWEEN 1 100";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert - adjacent bounds are reported as a missing operator.
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2018_MissingOperator, "BETWEEN missing AND");
    }

    [TestMethod]
    public void TypeMismatch_StringComparedToNumber()
    {
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 123";

        var result = analyzer.Analyze(query);

        Assert.IsTrue(result.IsParsed);
        DiagnosticContractTestAssertions.AssertNoErrors(result, nameof(TypeMismatch_StringComparedToNumber));
    }

    [TestMethod]
    public void TypeMismatch_NumberInStringFunction()
    {
        var analyzer = CreateAnalyzer();
        var query = "SELECT Substring(Population, 1, 2) FROM #A.Entities()";

        var result = analyzer.Analyze(query);

        _ = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ3088_NoMatchingCallableOverload,
            nameof(TypeMismatch_NumberInStringFunction)
        );
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
        var analyzer = CreateAnalyzer();
        var query = "SELECT name FROM #A.Entities()";

        var result = analyzer.Analyze(query);

        _ = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ3001_UnknownColumn,
            nameof(CaseSensitivity_ColumnNameWrongCase)
        );
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
        var query =
            @"
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
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Count(*) FROM #A.Entities()";

        var result = analyzer.Analyze(query);

        _ = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ3012_NonAggregateInSelect,
            nameof(Aggregate_WithoutGroupBy)
        );
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
        var analyzer = CreateAnalyzer();
        var query = "SELECT Count(Name, City, Country) FROM #A.Entities()";

        var result = analyzer.Analyze(query);

        _ = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ3087_InvalidCallableArity,
            nameof(Aggregate_WrongArgumentCount)
        );
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
