using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class InterpretationCarrierPruningTests
{
    private const string Query = """
        binary Packet { Size: byte, Items: byte[Size], Tail: byte };
        text Log { Level: until '|', Message: rest };
        select t.Level as Level, Sum(p.Tail::long) as Total, Count(*) as Count
        from #schema.first() r
        cross apply Interpret<Packet>(r.ReadPayload()) p
        cross apply p.Items item
        outer apply TryParse<Log>(r.ReadText()) t
        group by t.Level
        """;

    [TestMethod]
    public void PrunedCarriers_UnusedArrayElement_PreservesExpansionAndInterpretationCounts()
    {
        var input = new PacketInput();
        using var query = Compile(input);
        using var table = query.Run();

        TableMaterializationTestHelper.AssertColumns(table,
            ("Level", typeof(string)), ("Total", typeof(long?)), ("Count", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["INFO", 18L, 2L]);
        Assert.AreEqual(1, input.PayloadCalls);
        Assert.AreEqual(2, input.TextCalls);
        Assert.AreEqual(0, input.VolatileReads);
    }

    [TestMethod]
    public void PrunedCarriers_MalformedText_PreservesTolerantNullExtensionAndMultiplicity()
    {
        var input = new PacketInput { Text = "malformed" };
        using var query = Compile(input);
        using var table = query.Run();

        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, 18L, 2L]);
        Assert.AreEqual(1, input.PayloadCalls);
        Assert.AreEqual(2, input.TextCalls);
    }

    [TestMethod]
    public void TolerantParse_ArgumentThrowsNonParseException_PropagatesFailure()
    {
        var input = new PacketInput { ThrowText = true };
        using var query = Compile(input);
        using var table = query.Run();

        var exception = Assert.Throws<QueryExecutionException>(() => _ = table.Count);
        Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);
        Assert.AreEqual("Text argument failed", exception.InnerException.Message);
        Assert.AreEqual(1, input.TextCalls);
    }

    private static CompiledQuery Compile(PacketInput input) => InstanceCreator.CompileForExecution(Query,
        Guid.NewGuid().ToString(), LibraryExecutionFixture.CreateProvider<PacketInput, PacketLibrary>([input]),
        new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));

    public sealed class PacketInput
    {
        public byte[] Payload { get; init; } = [2, 7, 8, 9];
        public string Text { get; init; } = "INFO|ready";
        public bool ThrowText { get; init; }
        public int PayloadCalls;
        public int TextCalls;
        public int VolatileReads;

        [NonDeterministic]
        public int Unused => ++VolatileReads;
    }

    public sealed class PacketLibrary : LibraryBase
    {
        [BindableMethod]
        [NonDeterministic]
        public byte[] ReadPayload([InjectSpecificSource(typeof(PacketInput))] PacketInput input)
        {
            input.PayloadCalls++;
            return input.Payload;
        }

        [BindableMethod]
        [NonDeterministic]
        public string ReadText([InjectSpecificSource(typeof(PacketInput))] PacketInput input)
        {
            input.TextCalls++;
            if (input.ThrowText)
                throw new InvalidOperationException("Text argument failed");
            return input.Text;
        }
    }
}
