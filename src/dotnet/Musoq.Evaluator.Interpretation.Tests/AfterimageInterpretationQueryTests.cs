using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
[TestCategory("AfterimageRepro")]
public sealed class AfterimageInterpretationQueryTests
{
    [TestMethod]
    [DataRow("item")]
    [DataRow("b")]
    public void InterpretCteBytes_ShouldAllowReusingTheProducerSourceAlias(string resultAlias)
    {
        var sql = $$"""
            binary Payload { N: byte };
            with packets as (select b.Content as Body from #schema.first() b)
            select {{resultAlias}}.N
            from packets p
            cross apply Interpret<Payload>(p.Body) {{resultAlias}}
            """;
        var provider = LibraryExecutionFixture.CreateProvider<BinaryEntity>([new() { Content = [42] }]);

        using var compiled = InstanceCreator.CompileForExecution(
            sql,
            nameof(InterpretCteBytes_ShouldAllowReusingTheProducerSourceAlias),
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
        using var result = compiled.Run();

        Assert.AreEqual((byte)42, result.Single().GetValue<byte>(0));
    }

    [TestMethod]
    [DataRow("cross")]
    [DataRow("outer")]
    public void FilterOnNestedInterpretedField_ShouldKeepOnlyCompletePackets(string apply)
    {
        var sql = CreatePacketProjection(apply) + "\nwhere p.H.N is not null";

        using var compiled = InstanceCreator.CompileForExecution(
            sql,
            nameof(FilterOnNestedInterpretedField_ShouldKeepOnlyCompletePackets),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();

        Assert.AreEqual((byte)1, result.Single().ReadValue(0));
    }

    [TestMethod]
    public void NullCheckOnNestedInterpretedField_ShouldKeepOnlyMissingPackets()
    {
        var sql = CreatePacketProjection("outer") + "\nwhere p.H.N is null";

        using var compiled = InstanceCreator.CompileForExecution(
            sql,
            nameof(NullCheckOnNestedInterpretedField_ShouldKeepOnlyMissingPackets),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();

        Assert.IsNull(result.Single().GetValue<byte?>(0));
    }

    [TestMethod]
    public void OuterApplyProjection_ShouldPreserveNullInNestedInterpretedField()
    {
        var sql = CreatePacketProjection("outer") + "\norder by p.H.N nulls last";

        using var compiled = InstanceCreator.CompileForExecution(
            sql,
            nameof(OuterApplyProjection_ShouldPreserveNullInNestedInterpretedField),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();

        CollectionAssert.AreEqual(new byte?[] { 1, null }, result.Select(row => row.GetValue<byte?>(0)).ToArray());
    }

    private static string CreatePacketProjection(string apply) =>
        $$"""
            binary Header { N: byte };
            binary Packet { H: Header, Body: byte };
            select p.H.N
            from values { (Hex: '012A'), (Hex: '01') } b
            {{apply}} apply TryInterpret<Packet>(FromHexToBytes(b.Hex)) p
            """;
}
