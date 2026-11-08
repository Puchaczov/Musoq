using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    [DataRow("Interpret")]
    [DataRow("TryInterpret")]
    [DataRow("PartialInterpret")]
    public void InterpretationModes_ShouldPropagateComputedArithmeticFailures(string mode)
    {
        var query = $"binary Rec {{ Zero: byte, Value: 1 % Zero }}; select r.BytesConsumed " +
                    $"from #test.files() f cross apply {mode}<Rec>(f.Content) r";
        if (mode != "PartialInterpret")
            query = query.Replace("r.BytesConsumed", "r.Value", StringComparison.Ordinal);
        var exception = Assert.Throws<QueryExecutionException>(() => RunBinaryQuery(query, [0]));
        Assert.IsInstanceOfType<DivideByZeroException>(exception.InnerException);
    }

    [TestMethod]
    [DataRow("text Unused { Value: optional pattern '[bad' }")]
    [DataRow("text Unused { Value: Missing }")]
    [DataRow("text Unused { Value: repeat rest nested }")]
    public void UnusedInvalidSchemas_ShouldFailCompilation(string unused)
    {
        Assert.Throws<MusoqQueryException>(() => RunBinaryQuery(
            $"{unused}; binary Rec {{ Value: byte }}; select r.Value from #test.files() f cross apply TryInterpret<Rec>(f.Content) r", [1]));
    }
}
