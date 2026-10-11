using System;
using System.Linq;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionConcurrencyQueryTests
{
    [TestMethod]
    public void VolatileScalar_WhenExecutedTwice_ShouldOwnOneReceiverPerExecutionAndEvaluateEveryRow()
    {
        const string query = "select Position, NextSerialValue() as Tick from #schema.first() order by Position";
        LibraryExecutionWindowQueryTests.NumericRow<int>[] input = [new(1, 10), new(2, 20), new(3, 30)];
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"),
            LibraryExecutionFixture.CreateProvider<LibraryExecutionWindowQueryTests.NumericRow<int>, ReceiverLifetimeLibrary>(input),
            _logger);
        for (var run = 0; run < 2; run++)
        {
            using var table = compiled.Run(TestContext.CancellationToken);
            var rows = table.ToArray();
            TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Tick", typeof(int)));
            Assert.HasCount(3, rows);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, rows.Select(row => row.GetValue<int>(1)).ToArray());
        }
    }

    public sealed class ReceiverLifetimeLibrary : LibraryBase
    {
        private int _next;
        [BindableMethod, NonDeterministic]
        public int NextSerialValue() => checked(++_next);
    }
}
