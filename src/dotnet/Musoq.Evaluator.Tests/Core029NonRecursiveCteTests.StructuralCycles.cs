using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public sealed partial class Core029NonRecursiveCteTests
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0173")]
    public void ReachableCteCycleReportsQueryDiagnostic()
    {
        const string query = "with leftRows as (select n.Value from #inputs.numbers(values: rightRows) n), rightRows as (select n.Value from #inputs.numbers(values: leftRows) n) select n.Value from #inputs.numbers(values: leftRows) n";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
                new StructuredInputsSchemaProvider(), LoggerResolver, TestCompilationOptions, TestContext.CancellationToken));

        MusoqExceptionAssertions.AssertSingleError(exception, DiagnosticCode.MQ3016_CircularReference, DiagnosticPhase.Bind, "circular dependency");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void AcyclicStructuralCteSourceArgument_ShouldExecute()
    {
        const string query = "with leftRows as (select p.Value from values { (Value: 7) } p), rightRows as (select n.Value from #inputs.numbers(values: leftRows) n) select n.Value from #inputs.numbers(values: rightRows) n";
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new StructuredInputsSchemaProvider(), LoggerResolver, TestCompilationOptions, TestContext.CancellationToken);

        using var result = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(7, result[0].ReadValue(0));
    }
}
