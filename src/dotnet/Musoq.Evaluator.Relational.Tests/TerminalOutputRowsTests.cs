using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class TerminalOutputRowsTests
{
    private const string Sql =
        "select e.MyName() as Name, e.NullableValue as Value, e.Id as SortKey from #A.Entities() e order by SortKey";

    [TestMethod]
    public void SortedRows_ShouldKeepStableTiesAndExposeOnlyVisibleTypedColumns()
    {
        using var query = Compile();
        using var result = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["first", null, 1], ["second", 7, 1], ["later", 2, 2]);
        var row = result.First();
        var visitor = new ColumnTypes([]);
        row.VisitColumns(ref visitor);

        Assert.AreEqual(3, row.Count);
        Assert.IsTrue(row.HasColumn("Value"));
        Assert.IsFalse(row.HasColumn("__musoqSequence"));
        CollectionAssert.AreEqual(new[] { typeof(string), typeof(int?), typeof(int) }, visitor.Types);
    }

    [TestMethod]
    public void RetainedSortedRows_ShouldSurviveEnumerationAndStayLocalToTheirRun()
    {
        using var query = Compile();
        using var first = query.Run();
        var retained = first.First();
        retained.AssignValue<int?>(1, 42);
        using var second = query.Run();

        Assert.AreSame(retained, first.First());
        Assert.AreEqual(42, retained.GetValue<int?>(1));
        Assert.IsNull(second.First().GetValue<int?>(1));
    }

    [TestMethod]
    public void DeferredSortedRows_ShouldObserveCancellationBeforeEnumeration()
    {
        using var query = Compile();
        using var cancellation = new CancellationTokenSource();
        using var result = query.Run(cancellation.Token);
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => result.Count);
    }

    private static CompiledQuery Compile()
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] =
                [
                    new("later") { Id = 2, NullableValue = 2 },
                    new("first") { Id = 1 },
                    new("second") { Id = 1, NullableValue = 7 },
                ],
            }
        );
        return InstanceCreator.CompileForExecution(
            Sql,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
    }

    private readonly record struct ColumnTypes(List<Type> Types) : IColumnValueVisitor
    {
        public void Visit<T>(int columnIndex, in T value) => Types.Add(typeof(T));
    }
}
