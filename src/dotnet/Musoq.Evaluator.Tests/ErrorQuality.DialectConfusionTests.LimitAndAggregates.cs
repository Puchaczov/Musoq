using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityDialectConfusionTests
{
    #region P-LIMIT: LIMIT / OFFSET instead of TAKE / SKIP

    [TestMethod]
    public void P_LIMIT_01_LimitInsteadOfTake()
    {
        // Arrange — LIMIT instead of TAKE (MySQL/PostgreSQL style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() LIMIT 5";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should produce a parser error, ideally suggesting TAKE
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "LIMIT should suggest TAKE");
    }

    [TestMethod]
    public void P_LIMIT_02_OffsetInsteadOfSkip()
    {
        // Arrange — OFFSET instead of SKIP (MySQL/PostgreSQL style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() OFFSET 3";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should produce a parser error, ideally suggesting SKIP
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "OFFSET should suggest SKIP");
    }

    [TestMethod]
    public void P_LIMIT_03_LimitWithOffset_MySqlPostgresStyle()
    {
        // Arrange — LIMIT 5 OFFSET 3 (MySQL/PostgreSQL style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() LIMIT 5 OFFSET 3";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should produce a parser error, ideally suggesting TAKE/SKIP
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2001_UnexpectedToken, "LIMIT/OFFSET should suggest TAKE/SKIP");
    }

    [TestMethod]
    public void P_LIMIT_04_OffsetFetch_SqlServerStyle()
    {
        // Arrange — OFFSET..FETCH (SQL Server style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name OFFSET 3 ROWS FETCH NEXT 5 ROWS ONLY";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should produce a parser error
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2009_InvalidOrderByExpression, "OFFSET..FETCH should suggest TAKE/SKIP");
    }

    [TestMethod]
    public void P_LIMIT_05_Top_SqlServerStyle()
    {
        // Arrange — TOP (SQL Server style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT TOP 5 Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — In Musoq, TOP is not a keyword. The parser treats it as an identifier,
        // so 'SELECT TOP 5 Name' parses as 'SELECT (TOP + 5) AS Name'. This is valid syntax
        // even though it's semantically different from SQL Server's TOP N.
        // Users should use TAKE instead: SELECT Name FROM #A.Entities() TAKE 5
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_LIMIT_06_First_FirebirdStyle()
    {
        // Arrange — FIRST (Firebird style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT FIRST 5 Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — In Musoq, FIRST is not a keyword. The parser treats it as an identifier,
        // so 'SELECT FIRST 5 Name' parses as 'SELECT (FIRST + 5) AS Name'. This is valid syntax
        // even though it's semantically different from Firebird's FIRST N.
        // Users should use TAKE instead: SELECT Name FROM #A.Entities() TAKE 5
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_LIMIT_07_Rownum_OracleStyle()
    {
        // Arrange — ROWNUM (Oracle style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE ROWNUM <= 5";

        // Act — This will parse ROWNUM as a column name, so it goes to semantic analysis
        var result = analyzer.Analyze(query);

        // Assert — Should error at semantic level (unknown column ROWNUM)
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3001_UnknownColumn, "ROWNUM is not a Musoq concept");
    }

    #endregion

    #region P-AGG: COUNT(*) and aggregate syntax

    [TestMethod]
    public void P_AGG_01_CountStar()
    {
        // Arrange - COUNT(*) syntax from standard SQL is supported.
        var analyzer = CreateAnalyzer();
        var query = "SELECT COUNT(*) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        AssertNoErrors(result);
        Assert.IsTrue(result.IsParsed, "COUNT(*) should parse successfully");
    }

    [TestMethod]
    public void P_AGG_02_CountStarWithAlias()
    {
        // Arrange - COUNT(*) AS Total syntax from standard SQL is supported.
        var analyzer = CreateAnalyzer();
        var query = "SELECT COUNT(*) AS Total FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        AssertNoErrors(result);
        Assert.IsTrue(result.IsParsed, "COUNT(*) AS Total should parse successfully");
    }

    [TestMethod]
    public void P_AGG_03_CountStarLowercase()
    {
        // Arrange - count(*) lowercase syntax from standard SQL is supported.
        var analyzer = CreateAnalyzer();
        var query = "SELECT count(*) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert
        AssertNoErrors(result);
        Assert.IsTrue(result.IsParsed, "count(*) should parse successfully");
    }

    [TestMethod]
    public void P_AGG_04_CountDistinct()
    {
        // Arrange — COUNT(DISTINCT column) is now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT COUNT(DISTINCT Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should succeed now that COUNT(DISTINCT) is supported
        Assert.IsFalse(result.HasErrors, "COUNT(DISTINCT) should now be supported");
        Assert.IsTrue(result.IsParsed, "Query should be parsed successfully");
    }

    [TestMethod]
    public void P_AGG_05_SumDistinct()
    {
        // Arrange — SUM(DISTINCT Value) is now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT SUM(DISTINCT Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should succeed now that SUM(DISTINCT) is supported
        Assert.IsFalse(result.HasErrors, "SUM(DISTINCT) should now be supported");
        Assert.IsTrue(result.IsParsed, "Query should be parsed successfully");
    }

    [TestMethod]
    public void P_AGG_06_AvgDistinct()
    {
        // Arrange — AVG(DISTINCT Value) is now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT AVG(DISTINCT Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should succeed now that AVG(DISTINCT) is supported
        Assert.IsFalse(result.HasErrors, "AVG(DISTINCT) should now be supported");
        Assert.IsTrue(result.IsParsed, "Query should be parsed successfully");
    }

    #endregion
}
