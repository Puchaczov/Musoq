using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SqlConcatenationEvaluationOrderTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("Touch(null, 1) || Touch('a', 2)", new[] { 1, 2 }, null)]
    [DataRow("Touch(null, 1) || Touch('a', 2) || Touch('b', 3) || Touch('c', 4) || Touch('d', 5)", new[] { 1, 2, 3, 4, 5 }, null)]
    [DataRow("Touch('a', 1) || Touch('b', 2) || Touch('c', 3) || Touch('d', 4) || Touch('e', 5)", new[] { 1, 2, 3, 4, 5 }, "abcde")]
    public void Operands_ShouldExecuteOnceInOrderIncludingAfterNull(string expression, int[] expectedCalls, string? expected)
    {
        ConcatenationOrderLibrary.Calls = [];
        using var query = Compile($"select {expression} as A from #test.entities() e");
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { expected });
        CollectionAssert.AreEqual(expectedCalls, ConcatenationOrderLibrary.Calls);
    }

    [TestMethod]
    public void RepeatedVolatileConcatenation_ShouldNotBeReused()
    {
        ConcatenationOrderLibrary.Calls = [];
        const string expression = "Touch('a', 1) || Touch('b', 2)";
        using var query = Compile($"select {expression} as A, {expression} as B from #test.entities() e");
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["ab", "ab"]);
        CollectionAssert.AreEqual(new[] { 1, 2, 1, 2 }, ConcatenationOrderLibrary.Calls);
    }

    [TestMethod]
    public void NullLeftOperand_ShouldStillExecuteTheThrowingRightOperand()
    {
        ConcatenationOrderLibrary.Calls = [];
        using var query = Compile("select Touch(null, 1) || Fail(2) as A from #test.entities() e");

        Assert.Throws<QueryExecutionException>(() =>
        {
            using var table = query.Run(TestContext.CancellationToken);
            _ = table.Count;
        });
        CollectionAssert.AreEqual(new[] { 1, 2 }, ConcatenationOrderLibrary.Calls);
    }

    private CompiledQuery Compile(string sql)
    {
        var rows = new EntitySource<BasicEntity>(new[] { new[] { new BasicEntity("fixture") } },
            new Dictionary<string, int>(BasicEntity.TestNameToIndexMap),
            new Dictionary<int, Func<BasicEntity, object?>>(BasicEntity.TestIndexToObjectAccessMap));
        var schema = new GenericSchema<ConcatenationOrderLibrary>(
            new Dictionary<string, (ISchemaTable, object)> { ["entities"] = (new BasicEntityTable(), rows) });
        return InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(),
            new GenericSchemaProvider(new Dictionary<string, ISchema> { ["#test"] = schema }),
            new TestsLoggerResolver(), new CompilationOptions(ParallelizationMode.None));
    }
}

public sealed class ConcatenationOrderLibrary : LibraryBase
{
    [ThreadStatic]
    public static List<int>? Calls;

    [BindableMethod]
    [NonDeterministic]
    public string? Touch(string? value, int sequence)
    {
        Calls!.Add(sequence);
        return value;
    }

    [BindableMethod]
    [NonDeterministic]
    public string Fail(int sequence)
    {
        Calls!.Add(sequence);
        throw new InvalidOperationException("Expected operand failure.");
    }
}
