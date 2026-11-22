using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
[TestCategory("MirageRepro")]
public sealed class MirageInterpretationQueryTests
{
    [TestMethod]
    [DataRow("array { 0ub }", 0)]
    [DataRow("array { }", null)]
    public void ConditionalPayload_ShouldPreserveByteArrayTypeForTolerantInterpretation(string fallback, int? value)
    {
        var sql = $$"""
            binary Frame { Wire: byte, Payload: byte[1] };
            binary Reading { Value: byte };
            select f.Wire, r.Value
            from values { (Hex: '012A'), (Hex: '0207'), (Hex: '01') } v
            outer apply TryInterpret<Frame>(FromHexToBytes(v.Hex)) f
            outer apply TryInterpret<Reading>(case when f.Wire = 1 then f.Payload else {{fallback}} end) r
            order by v.Hex
            """;
        using var query = InstanceCreator.CompileForExecution(
            sql,
            nameof(ConditionalPayload_ShouldPreserveByteArrayTypeForTolerantInterpretation),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );

        using var result = query.Run();

        CollectionAssert.AreEqual(
            new byte?[] { (byte?)value, 42, (byte?)value },
            result.Select(row => row.GetValue<byte?>(1)).ToArray()
        );
    }
}
