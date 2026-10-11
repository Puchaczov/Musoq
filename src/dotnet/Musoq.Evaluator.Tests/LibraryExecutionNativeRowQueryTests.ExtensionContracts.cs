using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionNativeRowQueryTests
{
    [TestMethod]
    public void NativeSourceContracts_WhenSqlAndLeafTypeNamesMatch_ShouldKeepProviderBindingsIndependent()
    {
        var sql = "select s.Value as Value from #schema.first() s where s.Value >= 0 -- " + Guid.NewGuid().ToString("N");
        using var first = CreateAndRunVirtualMachine(sql, new[] { new FirstOwner.SharedRow(1) });
        using var firstTable = first.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(firstTable, ("Value", typeof(int)));
        CollectionAssert.AreEqual(new[] { 1 }, firstTable.Select(row => row.GetValue<int>(0)).ToArray());
        using var second = CreateAndRunVirtualMachine(sql, new[] { new SecondOwner.SharedRow(2) });
        using var secondTable = second.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(secondTable, ("Value", typeof(int)));
        CollectionAssert.AreEqual(new[] { 2 }, secondTable.Select(row => row.GetValue<int>(0)).ToArray());
    }

    [TestMethod]
    public void NativeSourceContracts_WhenProviderReplacesItsSchema_ShouldRebindConcreteRows()
    {
        var sql = "select s.Value as Value from #schema.first() s where s.Value >= 0 -- " + Guid.NewGuid().ToString("N");
        var provider = new MutableProvider(LibraryExecutionFixture.CreateProvider(new[] { new FirstOwner.SharedRow(1) }).GetSchema("#schema"));
        using var first = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString("N"), provider, LoggerResolver, TestCompilationOptions);
        using var firstTable = first.Run(TestContext.CancellationToken);
        CollectionAssert.AreEqual(new[] { 1 }, firstTable.Select(row => row.GetValue<int>(0)).ToArray());
        provider.Schema = LibraryExecutionFixture.CreateProvider(new[] { new SecondOwner.SharedRow(2) }).GetSchema("#schema");
        using var second = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString("N"), provider, LoggerResolver, TestCompilationOptions);
        using var secondTable = second.Run(TestContext.CancellationToken);
        CollectionAssert.AreEqual(new[] { 2 }, secondTable.Select(row => row.GetValue<int>(0)).ToArray());
    }

    public sealed class MutableProvider(Musoq.Schema.ISchema schema) : Musoq.Schema.ISchemaProvider
    {
        public Musoq.Schema.ISchema Schema { get; set; } = schema;
        public Musoq.Schema.ISchema GetSchema(string name) => Schema;
    }

    public static class FirstOwner { public sealed record SharedRow(int Value); }
    public static class SecondOwner { public sealed record SharedRow(int Value); }

    public const string NativeTupleQuery = "select Number as Item1, Count(Position) as Item2 from #schema.first() group by Number";

    [TestMethod]
    public void NativeTupleResults_ShouldKeepNullableKeysAndLongAggregateResults()
    {
        var provider = LibraryExecutionFixture.CreateProvider(Rows());
        var compiled = InstanceCreator.CompileForTypedExecution<(int?, long)>(NativeTupleQuery,
            Guid.NewGuid().ToString("N"), provider, LoggerResolver, TestCompilationOptions);
        var rows = compiled.Run(TestContext.CancellationToken).ToArray();
        CollectionAssert.AreEquivalent(new (int?, long)[] { (null, 2L), (1, 4L), (2, 1L) }, rows);
        Assert.HasCount(3, rows);
    }
}
