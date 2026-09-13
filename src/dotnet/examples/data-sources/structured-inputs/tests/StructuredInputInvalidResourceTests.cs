using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Examples.DataSources.StructuredInputs.Tests;

[TestClass]
public sealed class StructuredInputInvalidResourceTests
{
    [TestMethod]
    public void InvalidResources_ReportSyntaxOrBindingErrorsWithoutOpeningSources()
    {
        var root = FindRepositoryRoot();
        var invalidDirectory = Path.Combine(root, "src", "dotnet", "examples", "data-sources", "structured-inputs", "queries", "invalid");
        var expectedFamilies = new Dictionary<string, DiagnosticCode[]>(StringComparer.Ordinal)
        {
            ["old-values.sql"] = [DiagnosticCode.MQ2001_UnexpectedToken],
            ["mixed-values.sql"] = [DiagnosticCode.MQ2001_UnexpectedToken],
            ["missing-array-keyword.sql"] = [DiagnosticCode.MQ2001_UnexpectedToken],
            ["positional-record.sql"] = [DiagnosticCode.MQ2010_MissingClosingParenthesis],
            ["nullable-rejection.sql"] = [DiagnosticCode.MQ3088_NoMatchingCallableOverload],
            ["overload-ambiguity.sql"] = [DiagnosticCode.MQ3089_AmbiguousCallableOverload],
            ["scalar-cte-ambiguity.sql"] = [DiagnosticCode.MQ3116_AmbiguousRelationArgument]
        };

        foreach (var path in Directory.GetFiles(invalidDirectory, "*.sql", SearchOption.TopDirectoryOnly)
                     .Where(path => expectedFamilies.ContainsKey(Path.GetFileName(path))))
        {
            StructuredInputSourceCounters.Reset();
            var name = Path.GetFileName(path);
            var build = InstanceCreator.CompileWithDiagnostics(
                File.ReadAllText(path),
                $"structured-input-invalid-{Path.GetFileNameWithoutExtension(path)}",
                new StructuredInputsSchemaProvider(),
                new NullLoggerResolver(),
                new CompilationOptions(usePrimitiveTypeValidation: false));

            Assert.IsFalse(build.Succeeded, name);
            Assert.IsTrue(build.Errors.Count > 0, name);
            Assert.IsTrue(expectedFamilies[name].Contains(build.Errors[0].Code), $"{name}: actual {build.Errors[0].Code}; diagnostics: {string.Join(" | ", build.Errors.Select(static diagnostic => diagnostic.ToString()))}");
            Assert.AreEqual(0, StructuredInputSourceCounters.MatchConstructed, name);
        }
    }

    [TestMethod]
    public void InvalidRuntimeResources_ReportLimitAndConstructorFailuresAtUse()
    {
        var root = FindRepositoryRoot();
        var invalidDirectory = Path.Combine(root, "src", "dotnet", "examples", "data-sources", "structured-inputs", "queries", "invalid");
        foreach (var (file, counter, expectedText) in new[]
                 {
                     ("strict-limit-exceeded.sql", "strict", "MQ7013"),
                     ("throwing-source.sql", "throwing", "Throwing source fixture")
                 })
        {
            StructuredInputSourceCounters.Reset();
            var build = InstanceCreator.CompileWithDiagnostics(
                File.ReadAllText(Path.Combine(invalidDirectory, file)),
                $"structured-input-invalid-runtime-{Path.GetFileNameWithoutExtension(file)}",
                new StructuredInputsSchemaProvider(),
                new NullLoggerResolver(),
                new CompilationOptions(usePrimitiveTypeValidation: false));

            Assert.IsTrue(build.Succeeded, $"{file}: {string.Join(" | ", build.Diagnostics.Select(static diagnostic => diagnostic.ToString()))}");
            using var compiled = build.CompiledQuery!;
            var exception = Assert.ThrowsExactly<QueryExecutionException>(() =>
            {
                using var table = compiled.Run();
                _ = table.Count;
            });
            StringAssert.Contains(exception.ToString(), expectedText, file);
            var expectedCounter = counter == "throwing" ? 1 : 0;
            Assert.AreEqual(expectedCounter, ReadCounter(counter), file);
        }
    }

    [TestMethod]
    public void ReceiverLimitsApplyToLetAndParameterRootsBeforeSourceConstruction()
    {
        const string letQuery =
            "let values = array { 1, 2, 3 }; select s.Value from #inputs.strict(values: $values) s";
        StructuredInputSourceCounters.Reset();
        using (var letCompiled = Compile(letQuery, "structured-limit-let"))
        {
            var exception = Assert.ThrowsExactly<QueryExecutionException>(() =>
            {
                using var table = letCompiled.Run();
                _ = table.Count;
            });
            StringAssert.Contains(exception.ToString(), "MQ7013");
            StringAssert.Contains(exception.ToString(), "Structural input from 'let'");
            Assert.AreEqual(0, StructuredInputSourceCounters.StrictConstructed);
        }

        const string parameterQuery =
            "param(values: int[]) select s.Value from #inputs.strict(values: $values) s";
        StructuredInputSourceCounters.Reset();
        using var parameterCompiled = Compile(parameterQuery, "structured-limit-parameter");
        parameterCompiled.Parameters["values"] = new[] { 1, 2, 3 };
        var parameterException = Assert.ThrowsExactly<QueryExecutionException>(() =>
        {
            using var table = parameterCompiled.Run();
            _ = table.Count;
        });
        StringAssert.Contains(parameterException.ToString(), "MQ7013");
        Assert.AreEqual(0, StructuredInputSourceCounters.StrictConstructed);
    }

    [TestMethod]
    public void ReceiverLimitsApplyToCteRowsBeforeTargetAllocationAndSourceConstruction()
    {
        const string query =
            "with valuesFromQuery as (" +
            "select p.Value from values { (Value: 1), (Value: 2), (Value: 3) } p) " +
            "select s.Value from #inputs.strict(values: valuesFromQuery) s";

        StructuredInputSourceCounters.Reset();
        using var compiled = Compile(query, "structured-limit-cte");
        var exception = Assert.ThrowsExactly<QueryExecutionException>(() =>
        {
            using var table = compiled.Run();
            _ = table.Count;
        });

        StringAssert.Contains(exception.ToString(), "MQ7013");
        StringAssert.Contains(exception.ToString(), "from 'cte'");
        Assert.AreEqual(0, StructuredInputSourceCounters.StrictConstructed);
    }

    private static CompiledQuery Compile(string query, string name)
    {
        var build = InstanceCreator.CompileWithDiagnostics(
            query,
            name,
            new StructuredInputsSchemaProvider(),
            new NullLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        Assert.IsTrue(build.Succeeded, string.Join(" | ", build.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString())));
        return build.CompiledQuery!;
    }

    private static int ReadCounter(string name) => name switch
    {
        "strict" => StructuredInputSourceCounters.StrictConstructed,
        "throwing" => StructuredInputSourceCounters.ThrowingConstructed,
        _ => throw new InvalidOperationException(name)
    };

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "specs", "musoq-core-language-spec.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }

    private sealed class NullLoggerResolver : ILoggerResolver
    {
        public ILogger ResolveLogger() => NullLogger.Instance;
        public ILogger<T> ResolveLogger<T>() => NullLogger<T>.Instance;
    }
}
