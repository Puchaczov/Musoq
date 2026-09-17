using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Exceptions;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.StructuredInputs.Tests;

[TestClass]
public sealed class StructuredInputFoundationTests
{
    [TestMethod]
    public void Match_LiteralPreservesPatternOrderAndOffsets()
    {
        var rows = StructuredInputOperations.Match(
            "TODO FIXME TODO",
            [new PatternInput("todo", "TODO"), new PatternInput("fixme", "FIXME")]);

        CollectionAssert.AreEqual(
            new[]
            {
                new PatternMatchRow("todo", "TODO", 0),
                new PatternMatchRow("todo", "TODO", 11),
                new PatternMatchRow("fixme", "FIXME", 5)
            },
            rows);
    }

    [TestMethod]
    public void Match_RegexCompilesAndFindsMatchesInPatternOrder()
    {
        var rows = StructuredInputOperations.Match(
            "TODO ISSUE-42",
            [new PatternInput("issue", "ISSUE-[0-9]+", "regex")]);

        CollectionAssert.AreEqual(new[] { new PatternMatchRow("issue", "ISSUE-42", 5) }, rows);
    }

    [TestMethod]
    public void Match_InvalidModeAndCancellationAreRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(() => StructuredInputOperations.Match(
            "x", [new PatternInput("x", "x", "glob")]));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsExactly<OperationCanceledException>(() => StructuredInputOperations.Match(
            "x", [new PatternInput("x", "x")], cancellation.Token));
    }

    [TestMethod]
    public void Operations_PreserveDuplicatesNestedValuesAndDefaults()
    {
        var options = StructuredInputOperations.Configure(
            new OptionsInput(true, [10, 20], new WindowInput(2, 3)));
        var numbers = StructuredInputOperations.Numbers([1, 2, 2, 3]);
        var matrix = StructuredInputOperations.Matrix([[1, 2], [3], []]);
        var weighted = StructuredInputOperations.Weighted([
            new WeightedInput(10, 1.5m),
            new WeightedInput(20, 2.5m, false)]);

        CollectionAssert.AreEqual(new[] { 10, 20 }, options.Codes);
        Assert.AreEqual(2, options.Before);
        CollectionAssert.AreEqual(new[] { 1, 2, 2, 3 }, numbers.Select(static row => row.Value).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, matrix.Select(static row => row.Value).ToArray());
        Assert.IsTrue(weighted[0].Enabled);
        Assert.IsFalse(weighted[1].Enabled);
    }

    [TestMethod]
    public void Sources_UseTypedRowsAndCountConstruction()
    {
        StructuredInputSourceCounters.Reset();
        var context = CreateContext();
        var source = new MatchSource("TODO", [new PatternInput("todo", "TODO")], context);
        var rows = source.Chunks.SelectMany(static chunk => chunk).ToArray();

        Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
        CollectionAssert.AreEqual(new[] { new PatternMatchRow("todo", "TODO", 0) }, rows);
        Assert.AreEqual(typeof(PatternMatchRow), new StructuredInputsSchema().GetTableByName(
            StructuredInputsSchema.Match,
            new SourceMetadataContext("test", new CancellationTokenSource().Token, [], new Dictionary<string, string>(), NullLogger.Instance)).Metadata.TableEntityType);
    }

    [TestMethod]
    public void Schema_ExposesEveryFoundationSource()
    {
        var schema = new StructuredInputsSchema();
        var expected = new[]
        {
            "match", "configure", "numbers", "matrix", "weighted", "empty", "overloaded",
            "defaultconflict", "contextprobe", "strict", "mutable", "throwing", "ambiguous",
            "collectioncolumn", "relationambiguity"
        };
        CollectionAssert.AreEquivalent(expected, schema.GetConstructors()
            .Where(static constructor => constructor.MethodName.EndsWith("_source", StringComparison.Ordinal))
            .Select(static constructor => constructor.MethodName[..^"_source".Length])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray());
    }

    [TestMethod]
    public void PublicDocumentation_ListsContractAndBreakingSyntax()
    {
        var root = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(root, "src", "dotnet", "examples", "data-sources", "structured-inputs", "README.md"));
        StringAssert.Contains(readme, "array { ... }");
        StringAssert.Contains(readme, "(Id: 'todo')");
        StringAssert.Contains(readme, "match");
        StringAssert.Contains(readme, "DESC");
    }


    [TestMethod]
    public void Sql_InlineTypedStructuralSourceConstructsWithoutObjectArguments()
    {
        StructuredInputSourceCounters.Reset();
        var build = InstanceCreator.CompileWithDiagnostics(
            "select m.PatternId, m.MatchText from #inputs.match('TODO FIXME', patterns: array { (Id: 'todo', Pattern: 'TODO'), (Id: 'fixme', Pattern: 'FIXME') }) m",
            "structured-input-sql",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        if (!build.Succeeded)
            Assert.Fail(build.CaughtException?.ToString() ?? string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        using var compiled = build.CompiledQuery!;
        var table = compiled.Run();
        Assert.AreEqual(2, table.Rows.Count);
        Assert.AreEqual("todo", table.Rows[0][0]);
        Assert.AreEqual("TODO", table.Rows[0][1]);
        Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
    }
    [TestMethod]
    public void Sql_CorrelatedStructuralArgumentsPreservePreparationOrderAndSingleContext()
    {
        StructuredInputSourceCounters.Reset();
        var query = "param(suffix: string = '!') " +
                    "select m.PatternId from values { (Pattern: 'TODO'), } p " +
                    "cross apply #inputs.match('TODO!', patterns: array { (Id: 'todo', Pattern: p.Pattern + $suffix), }) m";
        var inspection = InstanceCreator.CompileForInspection(
            query,
            "structured-input-correlated-inspection",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        Assert.IsNotNull(inspection.GeneratedCSharpCode);
        var generated = inspection.GeneratedCSharpCode!;
        var fieldRead = generated.IndexOf("string pPattern = p.Pattern", StringComparison.Ordinal);
        var preparation = generated.IndexOf(" = new Musoq.Examples.DataSources.StructuredInputs.PatternInput(", StringComparison.Ordinal);
        Assert.IsTrue(fieldRead >= 0, generated);
        Assert.IsTrue(preparation >= 0, generated);
        Assert.IsTrue(fieldRead < preparation, generated);
        Assert.AreEqual(1, CountOccurrences(generated, "new SourceExecutionContext("), generated);
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            "structured-input-correlated-execution",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        if (!build.Succeeded)
            Assert.Fail(build.CaughtException?.ToString() ?? string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        using var compiled = build.CompiledQuery!;
        var table = compiled.Run();
        Assert.AreEqual(1, table.Rows.Count);
        Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
    }
    [TestMethod]
    public void Sql_AllInlineTypedStructuralSourcesExecute()
    {
        var cases = new[]
        {
            (Query: "select c.Enabled, c.Before, c.After from #inputs.configure(options: (Enabled: true, Codes: array { 10, 20 }, Window: (Before: 2, After: 3))) c", Columns: 3, Rows: 1),
            (Query: "select n.Value from #inputs.numbers(values: array { 1, 2, 2, 3 }) n", Columns: 1, Rows: 4),
            (Query: "select m.Value from #inputs.matrix(values: array { array { 1, 2 }, array { 3 }, array {} }) m", Columns: 1, Rows: 3),
            (Query: "select w.Value, w.Weight, w.Enabled from #inputs.weighted(items: array { (Value: 10, Weight: 1.5), (Weight: 2.5, Value: 20, Enabled: false) }) w", Columns: 3, Rows: 2)
        };

        foreach (var testCase in cases)
        {
            StructuredInputSourceCounters.Reset();
            var build = InstanceCreator.CompileWithDiagnostics(
                testCase.Query,
                "structured-input-inline",
                new StructuredInputsSchemaProvider(),
                new NullLoggerResolver(),
                new CompilationOptions(usePrimitiveTypeValidation: false));
            if (!build.Succeeded)
                Assert.Fail($"{testCase.Query}{Environment.NewLine}{string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString()))}");

            using var compiled = build.CompiledQuery!;
            var table = compiled.Run();
            Assert.AreEqual(testCase.Columns, table.Columns.Count(), testCase.Query);
            Assert.AreEqual(testCase.Rows, table.Rows.Count, testCase.Query);
            Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed +
                StructuredInputSourceCounters.ConfigureConstructed +
                StructuredInputSourceCounters.NumbersConstructed +
                StructuredInputSourceCounters.MatrixConstructed +
                StructuredInputSourceCounters.WeightedConstructed, testCase.Query);
        }
    }
    [TestMethod]
    public void Sql_RetainedLetNestedRecordAndArrayUseTypedStorage()
    {
        StructuredInputSourceCounters.Reset();
        var build = InstanceCreator.CompileWithDiagnostics(
            "let options = (Enabled: true, Codes: array { 10, 20 }, Window: (Before: 2, After: 3)); " +
            "select c.Enabled, c.Before, c.After from #inputs.configure(options: $options) c",
            "structured-input-retained-nested",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        if (!build.Succeeded)
            Assert.Fail(build.CaughtException?.ToString() ?? string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        using var compiled = build.CompiledQuery!;
        var table = compiled.Run();
        Assert.AreEqual(1, table.Rows.Count);
        Assert.IsTrue((bool)table.Rows[0][0]!);
        Assert.AreEqual(2, table.Rows[0][1]);
        Assert.AreEqual(3, table.Rows[0][2]);
        Assert.AreEqual(1, StructuredInputSourceCounters.ConfigureConstructed);
    }
    [TestMethod]
    public void Sql_DeclaredLetDefaultOverridesReceivingConstructorDefault()
    {
        StructuredInputSourceCounters.Reset();
        var build = InstanceCreator.CompileWithDiagnostics(
            "let patterns: (Id: string, Pattern: string, Mode: string = 'regex')[] = " +
            "array { (Id: 'issue', Pattern: 'ISSUE-[0-9]+') }; " +
            "select m.PatternId, m.MatchText from #inputs.match('ISSUE-42', patterns: $patterns) m",
            "structured-input-declared-default-precedence",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        if (!build.Succeeded)
            Assert.Fail(build.CaughtException?.ToString() ?? string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        using var compiled = build.CompiledQuery!;
        var table = compiled.Run();
        Assert.AreEqual(1, table.Rows.Count);
        Assert.AreEqual("issue", table.Rows[0][0]);
        Assert.AreEqual("ISSUE-42", table.Rows[0][1]);
        Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
    }
    [TestMethod]
    public void Sql_StructuredParameterSnapshotsHostDictionaryAndAppliesRecordDefaults()
    {
        StructuredInputSourceCounters.Reset();
        var build = InstanceCreator.CompileWithDiagnostics(
            "param(patterns: (Id: string, Pattern: string, Mode: string = 'literal')[]) " +
            "select m.PatternId, m.MatchText from #inputs.match('TODO FIXME', patterns: $patterns) m",
            "structured-input-parameter",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        if (!build.Succeeded)
            Assert.Fail(build.CaughtException?.ToString() ?? string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        using var compiled = build.CompiledQuery!;
        var definition = compiled.ParameterDefinitions.Single();
        Assert.IsTrue(definition.Contract.IsStructured, definition.Contract.CanonicalTypeName);
        compiled.Parameters["patterns"] = new List<Dictionary<string, object?>>
        {
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "todo",
                ["Pattern"] = "TODO"
            },
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Pattern"] = "FIXME",
                ["Id"] = "fixme",
                ["Mode"] = "literal"
            }
        };

        var table = compiled.Run();
        Assert.AreEqual(2, table.Rows.Count);
        Assert.AreEqual("todo", table.Rows[0][0]);
        Assert.AreEqual("fixme", table.Rows[1][0]);
        Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
    }

    [TestMethod]
    public void Sql_ReorderedStructuralFieldsUseAuthoredEvaluationLocals()
    {
        var query =
            "select m.PatternId from #inputs.match('TODO', patterns: array { " +
            "(Pattern: 'TODO', Id: 'todo') }) m";
        var inspection = InstanceCreator.CompileForInspection(
            query,
            "structured-input-authored-order-inspection",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));

        Assert.IsNotNull(inspection.GeneratedCSharpCode);
        var generated = inspection.GeneratedCSharpCode!;
        var pattern = generated.IndexOf("string __musoqStructural_m_0 = \"TODO\"", StringComparison.Ordinal);
        var id = generated.IndexOf("string __musoqStructural_m_1 = \"todo\"", StringComparison.Ordinal);
        var construction = generated.IndexOf("PatternInput(__musoqStructural_m_1, __musoqStructural_m_0", StringComparison.Ordinal);

        Assert.IsTrue(pattern >= 0, generated);
        Assert.IsTrue(id > pattern, generated);
        Assert.IsTrue(construction > id, generated);
        Assert.IsFalse(generated.Contains("PatternInput(\"todo\", \"TODO\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Sql_DeclaredParameterDefaultOverridesReceivingConstructorDefault()
    {
        StructuredInputSourceCounters.Reset();
        var build = InstanceCreator.CompileWithDiagnostics(
            "param(patterns: (Id: string, Pattern: string, Mode: string = 'regex')[]) " +
            "select m.PatternId, m.MatchText from #inputs.match('ISSUE-42', patterns: $patterns) m",
            "structured-input-parameter-default-precedence",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        if (!build.Succeeded)
            Assert.Fail(build.CaughtException?.ToString() ?? string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        using var compiled = build.CompiledQuery!;
        compiled.Parameters["patterns"] = new List<Dictionary<string, object?>>
        {
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = "issue",
                ["Pattern"] = "ISSUE-[0-9]+"
            }
        };

        var table = compiled.Run();
        Assert.AreEqual(1, table.Rows.Count);
        Assert.AreEqual("issue", table.Rows[0][0]);
        Assert.AreEqual("ISSUE-42", table.Rows[0][1]);
        Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
    }

    [TestMethod]
    public void Sql_TypedExecutionSnapshotsStructuredParametersBeforeEnumeration()
    {
        StructuredInputSourceCounters.Reset();
        var compiled = InstanceCreator.CompileForTypedExecution<PatternMatchRow>(
            "param(patterns: (Id: string, Pattern: string)[]) " +
            "select m.PatternId, m.MatchText, m.Offset from #inputs.match('TODO', patterns: $patterns) m",
            "structured-input-typed-parameter",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        var hostRecord = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = "todo",
            ["Pattern"] = "TODO"
        };
        var hostRecords = new List<Dictionary<string, object?>> { hostRecord };
        var rows = compiled.Run(new TypedQueryRunOptions(
            CancellationToken.None,
            new Dictionary<string, object?> { ["patterns"] = hostRecords }));

        hostRecord["Pattern"] = "MUTATED";
        var materialized = rows.ToArray();

        Assert.AreEqual(1, materialized.Length);
        Assert.AreEqual("TODO", materialized[0].MatchText);
        Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
    }
    [TestMethod]
    public void Sql_StructuredParameterRejectsLazyAndNonStringHostCollections()
    {
        var query =
            "param(patterns: (Id: string, Pattern: string)[]) " +
            "select m.PatternId from #inputs.match('TODO', patterns: $patterns) m";
        var hosts = new object?[]
        {
            Enumerable.Repeat(
                new Dictionary<string, object?>
                {
                    ["Id"] = "todo",
                    ["Pattern"] = "TODO"
                },
                1),
            new List<Dictionary<int, object?>>
            {
                new() { [1] = "todo" }
            }
        };

        foreach (var host in hosts)
        {
            StructuredInputSourceCounters.Reset();
            var build = InstanceCreator.CompileWithDiagnostics(
                query,
                "structured-input-invalid-host",
                new StructuredInputsSchemaProvider(),
                new NullLoggerResolver(),
                new CompilationOptions(usePrimitiveTypeValidation: false));
            if (!build.Succeeded)
                Assert.Fail(build.CaughtException?.ToString() ?? string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

            using var compiled = build.CompiledQuery!;
            compiled.Parameters["patterns"] = host;
            Exception? actual = null;
            try
            {
                _ = compiled.Run();
            }
            catch (Exception exception)
            {
                actual = exception;
            }
            Assert.IsNotNull(actual, actual?.ToString());
            Assert.IsInstanceOfType<QueryExecutionException>(actual, actual?.ToString());
            Assert.AreEqual(0, StructuredInputSourceCounters.MatchConstructed);
        }
    }
    [TestMethod]
    public void Sql_ParamsAliasAndStructuredDefaultProduceTypedInput()
    {
        StructuredInputSourceCounters.Reset();
        var build = InstanceCreator.CompileWithDiagnostics(
            "params(values: int[] = array { 1, 2, 2 }) " +
            "select n.Value from #inputs.numbers(values: $values) n",
            "structured-input-parameter-default",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        if (!build.Succeeded)
            Assert.Fail(build.CaughtException?.ToString() ?? string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        using var compiled = build.CompiledQuery!;
        Assert.IsTrue(compiled.ParameterDefinitions.Single().Contract.IsStructured);
        var table = compiled.Run();
        CollectionAssert.AreEqual(new[] { 1, 2, 2 }, table.Rows.Select(static row => (int)row[0]!).ToArray());
        Assert.AreEqual(1, StructuredInputSourceCounters.NumbersConstructed);
    }
    [TestMethod]
    public void Sql_StructuredParameterPreflightRejectsMissingRequiredBeforeOpeningSources()
    {
        StructuredInputSourceCounters.Reset();
        var build = InstanceCreator.CompileWithDiagnostics(
            "param(requiredPatterns: (Id: string, Pattern: string)[]) " +
            "select n.Value from #inputs.numbers(values: array { 1 }) n",
            "structured-input-parameter-required",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        if (!build.Succeeded)
            Assert.Fail(build.CaughtException?.ToString() ?? string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        using var compiled = build.CompiledQuery!;
        var definition = compiled.ParameterDefinitions.Single();
        Assert.IsTrue(definition.Contract.IsStructured, definition.Contract.CanonicalTypeName);
        var exception = Assert.Throws<QueryExecutionException>(() => compiled.Run());
        StringAssert.Contains(exception.ToString(), "requiredPatterns");
        Assert.AreEqual(0, StructuredInputSourceCounters.NumbersConstructed);
    }
    [TestMethod]
    public void Sql_CteRowsAdaptToTypedRecordCollection()
    {
        StructuredInputSourceCounters.Reset();
        var query =
            "with patterns as (" +
            "select p.Id, p.Pattern from values { (Id: 'todo', Pattern: 'TODO'), (Id: 'fixme', Pattern: 'FIXME') } p) " +
            "select m.PatternId, m.MatchText from #inputs.match('TODO FIXME', patterns: patterns) m";
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            "structured-input-cte",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        if (!build.Succeeded)
            Assert.Fail(build.CaughtException?.ToString() ?? string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        using var compiled = build.CompiledQuery!;
        var table = compiled.Run();
        Assert.AreEqual(2, table.Rows.Count);
        Assert.AreEqual("todo", table.Rows[0][0]);
        Assert.AreEqual("fixme", table.Rows[1][0]);
        Assert.AreEqual(1, StructuredInputSourceCounters.MatchConstructed);
    }

    [TestMethod]
    public void Sql_CteRowsAdaptToTypedPrimitiveCollection()
    {
        StructuredInputSourceCounters.Reset();
        var query =
            "with numbers as (" +
            "select p.Value from values { (Value: 1), (Value: 2), (Value: 2) } p) " +
            "select n.Value from #inputs.numbers(values: numbers) n";
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            "structured-input-cte-primitive",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        if (!build.Succeeded)
            Assert.Fail(build.CaughtException?.ToString() ?? string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        using var compiled = build.CompiledQuery!;
        var table = compiled.Run();
        CollectionAssert.AreEqual(new[] { 1, 2, 2 }, table.Rows.Select(static row => (int)row[0]!).ToArray());
        Assert.AreEqual(1, StructuredInputSourceCounters.NumbersConstructed);
    }

    [TestMethod]
    public void Sql_CtePreparationMeasuresTypedRowsBeforeTargetAllocation()
    {
        const string query =
            "with patterns as (select p.Id, p.Pattern from values { (Id: 'todo', Pattern: 'TODO') } p) " +
            "select m.PatternId from #inputs.match('TODO', patterns: patterns) m";
        var inspection = InstanceCreator.CompileForInspection(
            query,
            "structured-input-cte-preparation-shape",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));

        StringAssert.Contains(inspection.ExecutionPlanText, "ownership=ConstructFresh");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "__musoqPrepareCte_");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "__musoqStructuralNodes");
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("__musoqCheckStructural_", StringComparison.Ordinal));
        Assert.IsTrue(
            inspection.GeneratedCSharpCode.IndexOf("__musoqStructuralNodes", StringComparison.Ordinal) <
            inspection.GeneratedCSharpCode.IndexOf("PatternInput[rows.Count]", StringComparison.Ordinal),
            inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void Sql_DecimalWideningSelectsTheDecimalTypedSourceOverload()
    {
        StructuredInputSourceCounters.Reset();
        var build = InstanceCreator.CompileWithDiagnostics(
            "select o.Kind, o.Value from #inputs.overloaded(42.5) o",
            "structured-input-decimal-overload",
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        Assert.IsTrue(build.Succeeded, string.Join(Environment.NewLine, build.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString())));

        using var table = build.CompiledQuery!.Run();
        Assert.AreEqual("decimal", table.Rows.Single()[0]);
        Assert.AreEqual(42, table.Rows.Single()[1]);
        Assert.AreEqual(1, StructuredInputSourceCounters.OverloadConstructed);
    }

    [TestMethod]
    public void StrictLimitSourceLeavesResourceValidationToCore()
    {
        StructuredInputSourceCounters.Reset();
        var source = new StrictLimitSource([1, 2, 3], CreateContext());

        Assert.AreEqual(1, StructuredInputSourceCounters.StrictConstructed);
        Assert.AreEqual(3, source.Chunks.Single().Count);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var index = 0; (index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0; index += value.Length)
            count++;
        return count;
    }
    private static SourceExecutionContext CreateContext()
    {
        return new SourceExecutionContext(
            "structured-input-test",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            new CancellationTokenSource().Token,
            [],
            new Dictionary<string, string>(),
            NullLogger.Instance);
    }

    private sealed class NullLoggerResolver : ILoggerResolver
    {
        public Microsoft.Extensions.Logging.ILogger ResolveLogger() => NullLogger.Instance;
        public Microsoft.Extensions.Logging.ILogger<T> ResolveLogger<T>() => NullLogger<T>.Instance;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "specs", "musoq-core-language-spec.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
public sealed class MutableClassFixture
{
    public MutableClassFixture(int value) => Value = value;
    public int Value { get; set; }
}

public sealed class ThrowingConstructorFixture
{
    public ThrowingConstructorFixture() => throw new InvalidOperationException("fixture constructor");
}

public sealed class MultipleConstructorFixture
{
    public MultipleConstructorFixture() { }
    public MultipleConstructorFixture(int value) => Value = value;
    public int Value { get; }
}
