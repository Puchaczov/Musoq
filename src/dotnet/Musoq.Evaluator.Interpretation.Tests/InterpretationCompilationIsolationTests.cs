using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class InterpretationCompilationIsolationTests
{
    private const string ByteQuery = "binary Packet { Value: byte } select p.Value from #test.files() f cross apply Interpret<Packet>(f.Content) p";

    [TestMethod]
    public void RepeatedCompilation_ShouldBindEachExecutableToItsOwnProvider()
    {
        using var first = Compile(ByteQuery, [7]);
        using var second = Compile(ByteQuery, [9]);
        using var firstResult = first.Run();
        using var secondResult = second.Run();

        TableMaterializationTestHelper.AssertRowsInOrder(firstResult, [(byte)7]);
        TableMaterializationTestHelper.AssertRowsInOrder(secondResult, [(byte)9]);
    }

    [TestMethod]
    public void SchemasSharingAName_ShouldKeepTheirDistinctPayloadTypes()
    {
        const string intQuery = "binary Packet { Value: int le } select p.Value from #test.files() f cross apply Interpret<Packet>(f.Content) p";
        using var bytes = Compile(ByteQuery, [1, 1, 0, 0]);
        using var integers = Compile(intQuery, [1, 1, 0, 0]);
        using var byteResult = bytes.Run();
        using var intResult = integers.Run();

        TableMaterializationTestHelper.AssertRowsInOrder(byteResult, [(byte)1]);
        TableMaterializationTestHelper.AssertRowsInOrder(intResult, [257]);
    }

    [TestMethod]
    public void RetainedOutputRow_ShouldRemainIndependentOfTheNextExecution()
    {
        byte[] input = [7];
        using var query = Compile(ByteQuery, input);
        var first = query.Run();
        var retained = first.ToArray();
        first.Dispose();

        input[0] = 9;
        using var second = query.Run();

        Assert.AreEqual((byte)7, retained.Single().GetValue<byte>(0));
        TableMaterializationTestHelper.AssertRowsInOrder(second, [(byte)9]);
    }

    [TestMethod]
    public void SortedOutput_RepeatedEnumeration_ShouldPreserveRetainedValuesAndColumnTypes()
    {
        const string sql = "binary Packet { Value: byte } select p.Value as Value, p.Value + 1 as NextValue from #test.files() f cross apply Interpret<Packet>(f.Content) p order by p.Value";
        using var query = Compile(sql, [7]);
        using var result = query.Run();
        var retained = result.ToArray();

        TableMaterializationTestHelper.AssertRowsInOrder(result, [(byte)7, 8]);

        Assert.AreEqual((byte)7, retained.Single().GetValue<byte>(0));
        Assert.AreEqual(8, retained.Single().GetValue<int>(1));
    }

    [TestMethod]
    public void SortedOutput_AssignedValues_ShouldRemainLocalToTheirResultTable()
    {
        const string sql = "binary Packet { Value: byte } select p.Value as Value from #test.files() f cross apply Interpret<Packet>(f.Content) p order by p.Value";
        using var query = Compile(sql, [7]);
        var first = query.Run();
        var retained = first.ToArray();
        retained.Single().AssignValue(0, (byte)99);
        first.Dispose();

        using var second = query.Run();

        Assert.AreEqual((byte)99, retained.Single().GetValue<byte>(0));
        TableMaterializationTestHelper.AssertRowsInOrder(second, [(byte)7]);
    }

    private static CompiledQuery Compile(string sql, byte[] data) => InstanceCreator.CompileForExecution(sql,
        Guid.NewGuid().ToString(), new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
        {
            ["#test"] = [new() { Content = data }]
        }), new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
}
