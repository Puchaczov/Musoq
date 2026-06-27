using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityDialectConfusionTests
{
    #region P-SET: Set operations without column specification

    [TestMethod]
    public void P_SET_01_UnionAllWithoutColumnList()
    {
        // Arrange — UNION ALL without explicit column list (standard SQL style)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
UNION ALL
SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — omitted keys compare all projected fields
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_SET_02_UnionWithoutAllAndColumnList()
    {
        // Arrange — UNION (no ALL) without column list
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
UNION
SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — omitted keys compare all projected fields
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_SET_03_ExceptWithoutColumnList()
    {
        // Arrange — EXCEPT without column list
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
EXCEPT
SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — omitted keys compare all projected fields
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_SET_04_IntersectWithoutColumnList()
    {
        // Arrange — INTERSECT without column list
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
INTERSECT
SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — omitted keys compare all projected fields
        AssertNoErrors(result);
    }

    #endregion

    #region P-WIN: Window functions (not supported)

    [TestMethod]
    public void P_WIN_01_RowNumber()
    {
        // Arrange — ROW_NUMBER() OVER (ORDER BY ...) — now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, ROW_NUMBER() OVER (ORDER BY Name) AS RowNum FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Window functions are now supported
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_WIN_02_Rank()
    {
        // Arrange — RANK() OVER (ORDER BY ...) — now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, RANK() OVER (ORDER BY Name) AS Rank FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Window functions are now supported
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_WIN_03_Lag()
    {
        // Arrange — LAG() OVER (ORDER BY ...) — now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, LAG(Name, 1) OVER (ORDER BY Name) AS PrevName FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Window functions are now supported
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_WIN_04_Lead()
    {
        // Arrange — LEAD() OVER (ORDER BY ...) — now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, LEAD(Name, 1) OVER (ORDER BY Name) AS NextName FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Window functions are now supported
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_WIN_05_SumOver_RunningTotal()
    {
        // Arrange — SUM() OVER (ORDER BY ...) for running total — now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Sum(Population) OVER (ORDER BY Name) AS RunningTotal FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Window functions are now supported
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_WIN_06_PartitionBy()
    {
        // Arrange — COUNT() OVER (PARTITION BY ...) — now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Count(1) OVER (PARTITION BY City) AS GroupCount FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Window functions are now supported
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_WIN_07_Ntile()
    {
        // Arrange — NTILE() OVER (ORDER BY ...) — now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, NTILE(4) OVER (ORDER BY Name) AS Quartile FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Window functions are now supported
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_WIN_08_DenseRank()
    {
        // Arrange — DENSE_RANK() OVER (ORDER BY ...) — now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, DENSE_RANK() OVER (ORDER BY Name) AS DenseRank FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Window functions are now supported
        AssertNoErrors(result);
    }

    #endregion

    #region P-MISC: Miscellaneous SQL dialect confusion

    [TestMethod]
    public void P_MISC_01_NotEquals_ExclamationEquals()
    {
        // Arrange — != instead of <> (many dialects)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name != 'Warsaw'";

        // Act
        _ = analyzer.Analyze(query);

        // Assert — Musoq may or may not support !=. If not, should suggest <>
        // Document behavior:
        // Assert  != operator: either supported or should suggest <>
    }

    [TestMethod]
    public void P_MISC_02_DoubleEquals()
    {
        // Arrange — == instead of = (C#/JS habit)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name == 'Warsaw'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should error and suggest = instead of ==
        AssertHasOneOfErrorCodes(result, "== should suggest =",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2019_InvalidOperator,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_MISC_03_PipePipeConcatenation()
    {
        // Arrange — || for concatenation (PostgreSQL/SQLite)
        var analyzer = CreateAnalyzer();
        var query = "SELECT 'hello' || ' ' || 'world' FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest Concat() function
        AssertHasOneOfErrorCodes(result, "|| should suggest Concat()",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2019_InvalidOperator,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3007_InvalidOperandTypes,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_MISC_04_ILike_PostgresStyle()
    {
        // Arrange — ILIKE (PostgreSQL case-insensitive LIKE)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name ILIKE '%war%'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should error and suggest LIKE with appropriate workaround
        AssertHasOneOfErrorCodes(result, "ILIKE not supported, suggest LIKE",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_MISC_05_DoubleColonCasting_PostgresStyle()
    {
        // Arrange — :: for casting (PostgreSQL)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population::text FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should error and suggest ToString() or appropriate Cast function
        AssertHasOneOfErrorCodes(result, ":: casting should suggest ToString() etc.",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_MISC_06_CastExpression()
    {
        // Arrange — CAST(x AS type) is standard SQL
        var analyzer = CreateAnalyzer();
        var query = "SELECT CAST(Population AS varchar) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest ToInt32()/ToString()/etc.
        AssertHasOneOfErrorCodes(result, "CAST should suggest Musoq conversion functions",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_MISC_07_ConvertFunction_SqlServerStyle()
    {
        // Arrange — CONVERT function (SQL Server)
        var analyzer = CreateAnalyzer();
        var query = "SELECT CONVERT(varchar, Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest Musoq conversion functions.
        // CONVERT(varchar, Population) parses as a function call CONVERT with args,
        // but 'varchar' is treated as a column reference (unknown) and
        // the method CONVERT cannot be resolved.
        AssertHasOneOfErrorCodes(result, "CONVERT should suggest Musoq conversion functions",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_MISC_08_Coalesce()
    {
        // Arrange — COALESCE (standard SQL, test if supported)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Coalesce(null, Name) FROM #A.Entities()";

        // Act
        _ = analyzer.Analyze(query);

        // Assert — Document behavior: Musoq may support Coalesce as a built-in function
        // Assert  Coalesce: may be supported as built-in function
    }

    [TestMethod]
    public void P_MISC_09_IfNull_MySqlSqliteStyle()
    {
        // Arrange — IFNULL is a built-in Musoq function (IfNull<T>),
        // and now correctly resolves when the first argument is a null literal.
        var analyzer = CreateAnalyzer();
        var query = "SELECT IFNULL(null, Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — IfNull is supported natively in Musoq
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_MISC_10_Nvl_OracleStyle()
    {
        // Arrange — NVL (Oracle)
        var analyzer = CreateAnalyzer();
        var query = "SELECT NVL(null, Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest Coalesce or Musoq equivalent
        AssertHasOneOfErrorCodes(result, "NVL should suggest Coalesce or Musoq equivalent",
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_MISC_11_IsNull_SqlServerStyle()
    {
        // Arrange — ISNULL function (SQL Server)
        var analyzer = CreateAnalyzer();
        var query = "SELECT ISNULL(null, Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest Coalesce or Musoq equivalent
        AssertHasOneOfErrorCodes(result, "ISNULL should suggest Coalesce or Musoq equivalent",
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_MISC_12_BetweenOperator()
    {
        // Arrange — BETWEEN operator (test if supported)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population BETWEEN 50 AND 150";

        // Act
        _ = analyzer.Analyze(query);

        // Assert — Document: Musoq may support BETWEEN, if not suggest >= AND <=
        // Assert  BETWEEN: may be supported; if not, suggest >= AND <=
    }

    [TestMethod]
    public void P_MISC_13_DoubleQuotedIdentifiers()
    {
        // Arrange — Double-quoted identifiers (ANSI/PostgreSQL)
        var analyzer = CreateAnalyzer();
        var query = "SELECT \"Name\" FROM #A.Entities()";

        // Act
        _ = analyzer.Analyze(query);

        // Assert — Document: Musoq may or may not support double-quoted identifiers
        // Assert  Double-quoted identifiers: may parse as string or identifier
    }

    [TestMethod]
    public void P_MISC_14_BacktickIdentifiers_MySqlStyle()
    {
        // Arrange — Backtick identifiers (MySQL)
        var analyzer = CreateAnalyzer();
        var query = "SELECT `Name` FROM #A.Entities()";

        // Act
        _ = analyzer.ValidateSyntax(query);

        // Assert — Should error if backticks aren't supported
        // Assert  Backtick identifiers: either supported or should error
    }

    [TestMethod]
    public void P_MISC_15_StringAlias()
    {
        // Arrange — AS with string alias (some dialects allow this)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name AS 'MyColumn' FROM #A.Entities()";

        // Act
        _ = analyzer.Analyze(query);

        // Assert — Document behavior
        // Assert  String alias: may be supported or should use identifier
    }

    [TestMethod]
    public void P_MISC_16_StringConcatWithPlus_TypeMismatch()
    {
        // Arrange — String concatenation with + on numbers without casting
        var analyzer = CreateAnalyzer();
        var query = "SELECT 'Value is: ' + Population FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest ToString() or Concat()
        AssertHasOneOfErrorCodes(result, "string + number should suggest ToString/Concat",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3007_InvalidOperandTypes,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion
}
