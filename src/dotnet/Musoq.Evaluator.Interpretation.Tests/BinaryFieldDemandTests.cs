using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Fixtures;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class BinaryFieldDemandTests
{
    private const string Source = " from #schema.first() f cross apply Interpret<Packet>(f.Content) p";
    private const string Packet = "binary Packet { N: byte, Body: byte[N], Tail: byte }; ";

    [TestMethod]
    [DataRow("Length(p.Body)", "Body=Length", 2)]
    [DataRow("p.Tail", "Body=Discard", 7)]
    public void DynamicByteFieldUsesOnlyRequiredStorage(string expression, string planned, int expected)
    {
        var sql = Packet + "select " + expression + Source;
        var provider = LibraryExecutionFixture.CreateProvider<BinaryEntity>([new() { Content = [2, 9, 8, 7] }]);
        var options = new CompilationOptions(usePrimitiveTypeValidation: false);
        var inspected = InstanceCreator.CompileForInspection(
            sql,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            options
        );
        Assert.Contains(planned, inspected.OptimizedPhysicalPlanText);
        using var query = InstanceCreator.CompileForExecution(
            sql,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            options
        );
        using var table = query.Run();
        var row = table.Single();
        Assert.AreEqual(expected, expression == "p.Tail" ? row.GetValue<byte>(0) : row.GetValue<int?>(0));
    }

    [TestMethod]
    public void DifferentConsumersOfOneSchemaKeepIndependentValues()
    {
        var sql = Packet + "select q.Body, Length(p.Body)" + Source + " cross apply Interpret<Packet>(f.Content) q";
        var provider = LibraryExecutionFixture.CreateProvider<BinaryEntity>([new() { Content = [2, 9, 8, 7] }]);
        var inspected = InstanceCreator.CompileForInspection(
            sql,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
        Assert.Contains("Body=Length", inspected.OptimizedPhysicalPlanText);
        using var query = InstanceCreator.CompileForExecution(
            sql,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
        using var rows = query.Run();
        CollectionAssert.AreEqual(new byte[] { 9, 8 }, rows.Single().GetValue<byte[]>(0));
        Assert.AreEqual(2, rows.Single().GetValue<int?>(1));
    }

    [TestMethod]
    public void ConditionalLengthDistinguishesAbsentAndEmpty()
    {
        var sql = "binary Packet { N: byte, Body: byte[N] when N < 2 }; select Length(p.Body)" + Source;
        var provider = LibraryExecutionFixture.CreateProvider<BinaryEntity>([
            new() { Content = [2] },
            new() { Content = [0] },
            new() { Content = [1, 7] },
        ]);
        using var query = InstanceCreator.CompileForExecution(
            sql,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
        using var table = query.Run();
        CollectionAssert.AreEquivalent(new int?[] { null, 0, 1 }, table.Select(row => row.GetValue<int?>(0)).ToArray());
    }

    [TestMethod]
    public void LengthConsumerReportsTruncatedRegionAtOriginalPosition()
    {
        var sql = Packet + "select Length(p.Body)" + Source;
        var provider = LibraryExecutionFixture.CreateProvider<BinaryEntity>([new() { Content = [3, 1] }]);
        using var query = InstanceCreator.CompileForExecution(
            sql,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
        var error = Assert.Throws<ParseException>(() =>
        {
            using var table = query.Run();
            _ = table.Count;
        });
        Assert.AreEqual("Packet", error.SchemaName);
        Assert.AreEqual("Body", error.FieldName);
        Assert.AreEqual(1, error.Position);
    }

    [TestMethod]
    [DataRow("select p.Body, Length(p.Body)", false)]
    [DataRow("select p.Body[1], Length(p.Body)", true)]
    public void ValueConsumersKeepTheCompleteField(string projection, bool indexed)
    {
        var sql = Packet + projection + Source;
        var provider = LibraryExecutionFixture.CreateProvider<BinaryEntity>([new() { Content = [2, 9, 8, 7] }]);
        using var query = InstanceCreator.CompileForExecution(
            sql,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
        using var table = query.Run();
        var row = table.Single();
        if (indexed)
            Assert.AreEqual((byte)8, row.GetValue<byte>(0));
        else
            CollectionAssert.AreEqual(new byte[] { 9, 8 }, row.GetValue<byte[]>(0));
        Assert.AreEqual(2, row.GetValue<int?>(1));
    }

    [TestMethod]
    public void ComputedSchemaFieldRetainsItsInputBytes()
    {
        var sql = "binary Packet { N: byte, Body: byte[N], Copy: (Body) }; select p.Copy, Length(p.Body)" + Source;
        var provider = LibraryExecutionFixture.CreateProvider<BinaryEntity>([new() { Content = [2, 9, 8] }]);
        using var query = InstanceCreator.CompileForExecution(
            sql,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
        using var table = query.Run();
        var row = table.Single();
        CollectionAssert.AreEqual(new byte[] { 9, 8 }, row.GetValue<byte[]>(0));
        Assert.AreEqual(2, row.GetValue<int?>(1));
    }

    [TestMethod]
    public void OuterTryAndPartialKeepNullAndCompletedFields()
    {
        var sql =
            Packet
            + " select Length(p.Body), q.ParsedFields.Body, q.ErrorField from #schema.first() f outer apply TryInterpret<Packet>(f.Content) p cross apply PartialInterpret<Packet>(f.Content) q";
        var provider = LibraryExecutionFixture.CreateProvider<BinaryEntity>([new() { Content = [2, 9, 8] }]);
        using var query = InstanceCreator.CompileForExecution(
            sql,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
        using var table = query.Run();
        var row = table.Single();
        Assert.IsNull(row.GetValue<int?>(0));
        CollectionAssert.AreEqual(new byte[] { 9, 8 }, row.GetValue<byte[]>(1));
        Assert.AreEqual("Tail", row.GetValue<string>(2));
    }
}
