using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive tests for type-related user mistakes in queries.
///     These tests verify that type errors are caught gracefully at the semantic analysis layer
///     (before Roslyn code generation) and provide helpful, readable error messages
///     suitable for LSP and LLM agentic tooling.
/// </summary>
public partial class TypeRelatedMistakesTests
{

    [TestMethod]
    public void Valid_NumericArithmetic()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population * 2 + 100 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_StringConcatenation()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name + ' - ' + City FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_BooleanComparison()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population > 50 AND Name = 'test'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_AggregateOnNumeric()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Sum(Population), Avg(Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_CaseExpression()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT CASE
                WHEN Population > 100 THEN 'Large'
                WHEN Population > 50 THEN 'Medium'
                ELSE 'Small'
            END
            FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_IsNullCheck()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name IS NOT NULL";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_UnionSameTypes()
    {
        // Arrange - explicit key syntax remains valid
        var analyzer = CreateAnalyzer();
        var query = @"
            SELECT Name FROM #A.Entities()
            UNION ALL (Name)
            SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_GroupByWithAggregate()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Country, Count(Name), Sum(Population) FROM #A.Entities() GROUP BY Country";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

}
