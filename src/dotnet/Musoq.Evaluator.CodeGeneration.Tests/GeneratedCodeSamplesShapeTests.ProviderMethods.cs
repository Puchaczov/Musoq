using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void NullableProviderMethodLeftJoinSample_WhenCompiledForInspection_ShouldEmitTypedNullLiftedCall()
    {
        var result = CompileSampleForInspection(NullableProviderMethodLeftJoinSampleFileName);
        var generated = result.GeneratedCSharpCode;

        Assert.Contains("CASE WHEN b1 IS NULL THEN NULL ELSE GetCountry() END", result.ExecutionPlanText);
        Assert.Contains(
            "Musoq.Evaluator.Tests.Schema.Basic.BasicEntity b1 = (Musoq.Evaluator.Tests.Schema.Basic.BasicEntity)ab.__rightContext",
            generated);
        Assert.Contains("(b1 == null) ? (string)null :", generated);
        Assert.Contains("__resultLibrary0.GetCountry((Musoq.Evaluator.Tests.Schema.Basic.BasicEntity)b1)", generated);
        Assert.IsFalse(generated.Contains("MethodInfo.Invoke", StringComparison.Ordinal), generated);
        Assert.IsFalse(generated.Contains("dynamic ", StringComparison.Ordinal), generated);
        Assert.IsFalse(generated.Contains("GetColumnValue", StringComparison.Ordinal), generated);
    }

    [TestMethod]
    public void NullableProviderMethodLeftJoinSample_WhenCheckedIn_ShouldRetainTypedNullLiftedCall()
    {
        var sample = ReadSample(NullableProviderMethodLeftJoinSampleFileName);
        var generated = sample.Content;

        Assert.Contains("CASE WHEN b1 IS NULL THEN NULL ELSE GetCountry() END", generated);
        Assert.Contains(
            "Musoq.Evaluator.Tests.Schema.Basic.BasicEntity b1 = (Musoq.Evaluator.Tests.Schema.Basic.BasicEntity)ab.__rightContext",
            generated);
        Assert.Contains("__resultLibrary0.GetCountry((Musoq.Evaluator.Tests.Schema.Basic.BasicEntity)b1)", generated);
        Assert.IsFalse(generated.Contains("MethodInfo.Invoke", StringComparison.Ordinal), generated);
        Assert.IsFalse(generated.Contains("dynamic ", StringComparison.Ordinal), generated);
    }
}
