using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class PrecisionDiagnosticPropagationTests
{
    [TestMethod]
    public void InvalidConstantRegex_PropagatesThroughDiagnosticCompilation()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "select Name from #data.entities() d where Name rlike '['",
            $"Precision_{Guid.NewGuid():N}",
            new EntitySetSchemaProvider(
                new Dictionary<string, IReadOnlyList<EntitySetEntity>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["#data"] = [new EntitySetEntity { Population = 42m }]
                }),
            new TestsLoggerResolver());

        var errors = result.Errors.ToArray();
        Assert.AreEqual(1, errors.Length, Describe(result.Diagnostics));
        Assert.AreEqual(
            DiagnosticCode.MQ3094_InvalidConstantRegex,
            errors[0].Code,
            Describe(result.Diagnostics));
        Assert.AreEqual(DiagnosticPhase.Bind, errors[0].Phase);
        Assert.IsNull(result.CompiledQuery);
    }

    private static string Describe(System.Collections.Generic.IReadOnlyList<Diagnostic> diagnostics) =>
        string.Join("\n", diagnostics.Select(diagnostic => $"[{diagnostic.Code}] {diagnostic.Message}"));
}
