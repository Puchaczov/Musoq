using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Converter.Tests.Components;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class NullableBooleanPredicateSingleEvaluationTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void NullablePluginPredicate_ShouldEvaluateOnceAcrossPredicateContextsAndCseModes()
    {
        var contexts = new[]
        {
            ("where", "select i.Value from #artifact.items() i where Probe('{0}')", true),
            ("case", "select case when Probe('{0}') then 'yes' else 'no' end from #artifact.items() i", true),
            ("and-left", "select case when Probe('{0}') and true then 'yes' else 'no' end from #artifact.items() i", true),
            ("and-right", "select case when true and Probe('{0}') then 'yes' else 'no' end from #artifact.items() i", true),
            ("or-left", "select case when Probe('{0}') or false then 'yes' else 'no' end from #artifact.items() i", true),
            ("or-right", "select case when false or Probe('{0}') then 'yes' else 'no' end from #artifact.items() i", true),
            ("short-circuit-and", "select case when false and Probe('{0}') then 'yes' else 'no' end from #artifact.items() i", false),
            ("short-circuit-or", "select case when true or Probe('{0}') then 'yes' else 'no' end from #artifact.items() i", false)
        };

        foreach (var useCse in new[] { true, false })
        foreach (var probeValue in new[] { "true", "false", "null" })
        foreach (var (name, template, shouldInvoke) in contexts)
        {
            var library = new CountingNullablePluginLibrary();
            var provider = new PluginArtifactSchemaProvider("folder/file", library);
            var query = string.Format(template, probeValue);
            var result = InstanceCreator.CompileWithDiagnostics(
                query,
                $"NullablePredicate_{name}_{probeValue}_{useCse}",
                provider,
                _loggerResolver,
                new CompilationOptions(useCommonSubexpressionElimination: useCse));

            Assert.IsTrue(result.Succeeded, FormatFailure(name, result));
            using (var table = result.CompiledQuery.Run())
            {
                var expectedValue = shouldInvoke
                    ? probeValue == "true"
                    : name == "short-circuit-or";
                if (name == "where")
                    Assert.AreEqual(expectedValue ? 1 : 0, table.Count);
                else
                    Assert.AreEqual(expectedValue ? "yes" : "no", table[0][0]);
            }
        }
    }

    [TestMethod]
    public void NullablePluginPredicate_ShouldAppearOnceInGeneratedCode()
    {
        var provider = new PluginArtifactSchemaProvider("folder/file", new CountingNullablePluginLibrary());
        var generated = InstanceCreator.GetGeneratedCSharpCode(
            "select case when Probe('true') then 'yes' else 'no' end from #artifact.items() i",
            "NullablePredicate_GeneratedShape",
            provider,
            _loggerResolver);

        var probeCalls = CSharpSyntaxTree.ParseText(generated)
            .GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Count(invocation =>
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.ValueText == nameof(CountingNullablePluginLibrary.Probe));

        Assert.AreEqual(1, probeCalls);
    }

    private static string FormatFailure(string context, BuildResult result)
    {
        return $"{context}{Environment.NewLine}" +
               $"{result.CaughtException}{Environment.NewLine}" +
               string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
    }
}

public sealed class CountingNullablePluginLibrary : LibraryBase
{
    private int _invocationCount;

    [BindableMethod]
    [NonDeterministic]
    public bool? Probe(string value)
    {
        var invocation = Interlocked.Increment(ref _invocationCount);
        bool? parsed = value switch
        {
            "true" => true,
            "false" => false,
            "null" => null,
            _ => throw new ArgumentException($"Unknown probe value '{value}'.", nameof(value))
        };

        return invocation == 1
            ? parsed
            : parsed switch
            {
                true => false,
                false => true,
                null => null
            };
    }
}
