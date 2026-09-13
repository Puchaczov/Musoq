using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Examples.DataSources.StructuredInputs.Tests;

[TestClass]
public sealed class StructuredInputDescriptionTests
{
    [TestMethod]
    public void DescArguments_InventoryReturnsElevenColumnsWithoutSourceConstruction()
    {
        StructuredInputSourceCounters.Reset();
        using var compiled = Compile("desc arguments #inputs.match", "structured-input-desc-inventory");

        var table = compiled.Run();

        CollectionAssert.AreEqual(
            new[]
            {
                "Overload", "Path", "Kind", "Type", "Required", "Nullable", "HasDefault",
                "Default", "MaxDepth", "MaxNodes", "MaxStringBytes"
            },
            table.Columns.OrderBy(static column => column.ColumnIndex).Select(static column => column.ColumnName).ToArray());
        Assert.IsTrue(table.Rows.Count > 0);
        Assert.AreEqual(0, StructuredInputSourceCounters.MatchConstructed);

        var patternsRoot = table.Rows.First(row => Equals(row[1], "patterns"));
        Assert.AreEqual("Collection", patternsRoot[2]);
        Assert.AreEqual("(Id: string, Pattern: string, Mode: string = 'literal')[]?", patternsRoot[3]);
        Assert.AreEqual(true, patternsRoot[4]);
        Assert.AreEqual(true, patternsRoot[5]);
        Assert.AreEqual(32, patternsRoot[8]);
        Assert.AreEqual(100_000, patternsRoot[9]);
        Assert.AreEqual(67_108_864L, patternsRoot[10]);

        var mode = table.Rows.First(row => Equals(row[1], "patterns[].Mode"));
        Assert.AreEqual(false, mode[4]);
        Assert.AreEqual(true, mode[6]);
        Assert.AreEqual("'literal'", mode[7]);
        Assert.IsNull(mode[8]);
    }

    [TestMethod]
    public void DescArguments_ConcreteCallReportsSelectedContractWithoutEvaluatingValues()
    {
        StructuredInputSourceCounters.Reset();
        const string query =
            "desc arguments #inputs.match('TODO', patterns: array { (Id: 'todo', Pattern: 'TODO') })";
        using var compiled = Compile(query, "structured-input-desc-concrete");

        var table = compiled.Run();

        Assert.AreEqual(6, table.Rows.Count);
        Assert.AreEqual(0, StructuredInputSourceCounters.MatchConstructed);
        Assert.AreEqual(0, table.Rows[0][0]);
        Assert.AreEqual("text", table.Rows[0][1]);
        Assert.AreEqual("Scalar", table.Rows[0][2]);
        Assert.AreEqual("string", table.Rows[0][3]);
        Assert.AreEqual("patterns[].Mode", table.Rows[5][1]);
    }

    [TestMethod]
    public void DescArguments_CoupledAliasUsesUnderlyingTypedContract()
    {
        StructuredInputSourceCounters.Reset();
        const string query =
            "table MatchRows { PatternId: string };" +
            "couple #inputs.match with table MatchRows as Matches;" +
            "desc arguments Matches";
        using var compiled = Compile(query, "structured-input-desc-coupled");

        var table = compiled.Run();

        Assert.IsTrue(table.Rows.Any(row => Equals(row[1], "patterns[].Mode")));
        Assert.AreEqual(0, StructuredInputSourceCounters.MatchConstructed);
    }
    [TestMethod]
    public void DescArguments_DeclaredParameterNeedsNoHostValue()
    {
        StructuredInputSourceCounters.Reset();
        const string query =
            "param(patterns: (Id: string, Pattern: string)[])" +
            "desc arguments #inputs.match('TODO', patterns: $patterns)";
        using var compiled = Compile(query, "structured-input-desc-parameter");


        var table = compiled.Run();

        Assert.IsTrue(table.Rows.Any(row => Equals(row[1], "patterns[].Id")));
        Assert.AreEqual(0, StructuredInputSourceCounters.MatchConstructed);
    }
    private static CompiledQuery Compile(string query, string name)
    {
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            name,
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        if (!build.Succeeded)
            Assert.Fail(build.CaughtException?.ToString() ?? string.Join(System.Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        return build.CompiledQuery!;
    }

    private sealed class NullLoggerResolver : ILoggerResolver
    {
        public Microsoft.Extensions.Logging.ILogger ResolveLogger() => NullLogger.Instance;
        public Microsoft.Extensions.Logging.ILogger<T> ResolveLogger<T>() => NullLogger<T>.Instance;
    }
}
