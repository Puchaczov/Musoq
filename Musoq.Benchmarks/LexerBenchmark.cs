using BenchmarkDotNet.Attributes;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Benchmarks;

/// <summary>
///     Benchmarks for the lexer to measure tokenization performance.
///     To compare against a baseline:
///     1. Run benchmarks and save baseline: dotnet run -c Release -- --filter *Lexer* --exporters json
///     2. Make changes to lexer
///     3. Run again and compare: dotnet run -c Release -- --filter *Lexer* --exporters json
///     4. Compare JSON files in BenchmarkDotNet.Artifacts/results/
///     Or use git-based comparison:
///     1. Run: dotnet run -c Release -- --filter *Lexer* --exporters json
///     2. Rename results folder to "baseline"
///     3. Checkout new version
///     4. Run benchmarks again
///     5. Compare results
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
[JsonExporterAttribute.Full]
[MarkdownExporter]
[RPlotExporter]
public class LexerBenchmark
{
    private const string SimpleQuery = "SELECT a, b, c FROM #test.data()";

    private const string MediumQuery = @"
        SELECT a.Name, b.Value, COUNT(*) as Cnt
        FROM #schema.table() a
        INNER JOIN #other.data() b ON a.Id = b.ParentId
        WHERE a.Active = true AND b.Status IN ('Open', 'Pending')
        GROUP BY a.Name, b.Value
        HAVING COUNT(*) > 5
        ORDER BY Cnt DESC
        TAKE 100";

    private const string ComplexArithmeticQuery = @"
        SELECT 
            (a + b * c - d / e) % f,
            ((1 + 2) * (3 - 4)) / ((5 + 6) - (7 * 8)),
            0xFF + 0b101 - 0o77,
            1.5d * 2.3 + 4.7d
        FROM #calc.numbers()";

    private const string KeywordHeavyQuery = @"
        SELECT DISTINCT a, b
        FROM #test.data()
        WHERE NOT (a IS NULL OR b IS NULL)
        AND a LIKE '%pattern%'
        AND b NOT LIKE '%exclude%'
        AND c RLIKE '^regex.*$'
        AND d CONTAINS 'text'
        AND e IN (1, 2, 3)
        AND f NOT IN (4, 5, 6)
        ORDER BY a ASC, b DESC
        SKIP 10 TAKE 50";

    private const string StringHeavyQuery = @"
        SELECT 
            'Hello World',
            'Escaped \' quote',
            'Unicode \u0041\u0042\u0043',
            'Tab\tNewline\nCarriage\r',
            ''
        FROM #strings.data()";

    private const string LargeColumnListQuery = @"
        SELECT 
            col1, col2, col3, col4, col5, col6, col7, col8, col9, col10,
            col11, col12, col13, col14, col15, col16, col17, col18, col19, col20,
            col21, col22, col23, col24, col25, col26, col27, col28, col29, col30,
            col31, col32, col33, col34, col35, col36, col37, col38, col39, col40,
            col41, col42, col43, col44, col45, col46, col47, col48, col49, col50
        FROM #wide.table()";

    private const string BitwiseOperationsQuery = @"
        SELECT 
            (Flags & 0xFF),
            (Value | 0x0F),
            (Bits ^ 0xAA),
            (Num << 2),
            (Num >> 1)
        FROM #bitwise.data()
        WHERE (Flags & 0x01) = 1";

    /// <summary>
    ///     Benchmark: Simple SELECT query with few columns.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Simple SELECT query")]
    public int Lexer_SimpleQuery()
    {
        return LexAllTokens(SimpleQuery);
    }

    /// <summary>
    ///     Benchmark: Medium complexity query with JOIN and GROUP BY.
    /// </summary>
    [Benchmark(Description = "Medium query with JOIN")]
    public int Lexer_MediumQuery()
    {
        return LexAllTokens(MediumQuery);
    }

    /// <summary>
    ///     Benchmark: Complex arithmetic expressions.
    /// </summary>
    [Benchmark(Description = "Arithmetic expressions")]
    public int Lexer_ComplexArithmetic()
    {
        return LexAllTokens(ComplexArithmeticQuery);
    }

    /// <summary>
    ///     Benchmark: Many SQL keywords.
    /// </summary>
    [Benchmark(Description = "Keyword-heavy query")]
    public int Lexer_KeywordHeavy()
    {
        return LexAllTokens(KeywordHeavyQuery);
    }

    /// <summary>
    ///     Benchmark: String literal processing.
    /// </summary>
    [Benchmark(Description = "String literals")]
    public int Lexer_StringHeavy()
    {
        return LexAllTokens(StringHeavyQuery);
    }

    /// <summary>
    ///     Benchmark: Large column list.
    /// </summary>
    [Benchmark(Description = "50-column SELECT")]
    public int Lexer_LargeColumnList()
    {
        return LexAllTokens(LargeColumnListQuery);
    }

    /// <summary>
    ///     Benchmark: Bitwise operations.
    /// </summary>
    [Benchmark(Description = "Bitwise operations")]
    public int Lexer_BitwiseOperations()
    {
        return LexAllTokens(BitwiseOperationsQuery);
    }

    private static int LexAllTokens(string query)
    {
        var lexer = new Lexer(query, true);
        var count = 0;

        var token = lexer.Next();
        while (token.TokenType != TokenType.EndOfFile)
        {
            count++;
            token = lexer.Next();
        }

        return count;
    }
}
