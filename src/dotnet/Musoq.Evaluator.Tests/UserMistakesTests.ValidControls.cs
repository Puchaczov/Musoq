using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class UserMistakesTests
{
    [TestMethod]
    public void Valid_SelectWithWhere()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 'test'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "Valid query should parse");
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_SelectWithOrderBy()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name ASC";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "Valid query should parse");
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_SelectWithGroupBy()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Country, Count(Name) FROM #A.Entities() GROUP BY Country";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_SelectStar()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_ArithmeticExpression()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population * 2 FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed, "Valid query should parse");
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
        Assert.IsTrue(result.IsParsed, "Valid query should parse");
        AssertNoErrors(result);
    }

    [TestMethod]
    public void Valid_CTE()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = "WITH cte AS (SELECT Name FROM #A.Entities()) SELECT Name FROM cte";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        Assert.IsTrue(result.IsParsed);
        AssertNoErrors(result);
    }

}
