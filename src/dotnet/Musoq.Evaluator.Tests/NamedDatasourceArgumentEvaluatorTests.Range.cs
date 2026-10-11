using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class NamedDatasourceArgumentEvaluatorTests
{
    [TestMethod]
    public void MixedRangeArguments_UnquotedReservedEnd_ShouldReportParseError()
    {
        const string query = "select r.Value as Value from #system.range(0, end: 1) r";

        var failure = Assert.Throws<MusoqQueryException>(() => CompileRangeArguments(query));

        MusoqExceptionAssertions.AssertErrorEnvelope(failure,
            DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P2-0090")]
    public void Current_P2_0090_MixedNamedRangeArgumentsUseCanonicalOrder()
    {
        const string query = "select r.Value as Value from #system.range(0, [end]: 1) r";
        using var compiled = CompileRangeArguments(query);
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 0 });
    }

    private static CompiledQuery CompileRangeArguments(string query)
    {
        return InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new SpecStressSystemRangeSchemaProvider(), new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
    }
}
