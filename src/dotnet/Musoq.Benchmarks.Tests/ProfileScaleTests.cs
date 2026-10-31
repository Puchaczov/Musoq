using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;
using Musoq.Schema;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class ProfileScaleTests
{
    [TestMethod]
    [DataRow("B14_Group_ManyGroups", 1_000, 1_000L, 499_500L)]
    [DataRow("B14_Group_ManyGroups", 10_000, 10_000L, 49_995_000L)]
    [DataRow("B14_Group_ManyGroups", 100_000, 100_000L, 4_999_950_000L)]
    [DataRow("B07_EqualityJoin_Shuffled", 1_000, 1_000L, 499_500L)]
    [DataRow("B07_EqualityJoin_Shuffled", 10_000, 10_000L, 49_995_000L)]
    [DataRow("B07_EqualityJoin_Shuffled", 100_000, 100_000L, 4_999_950_000L)]
    [DataRow("B07_DuplicateJoin_Fanout8", 1_000, 6_000L, 2_994_000L)]
    [DataRow("B07_DuplicateJoin_Fanout8", 10_000, 60_000L, 299_940_000L)]
    [DataRow("B07_DuplicateJoin_Fanout8", 100_000, 600_000L, 29_999_400_000L)]
    public void StandardScale_PreservesCompleteCardinalityAndIdSum(string id, int count, long expectedRows, long expectedSum)
    {
        var scenario = ScenarioCatalog.Get(id);
        using var query = InstanceCreator.CompileForExecution(scenario.Sql, "profile-scale",
            FixtureCatalog.Prepare(scenario, count).CreateSchemaProvider(), BenchmarkLogger.Instance, ExecutionSettings.Create(scenario, count));
        using var table = query.Run();
        var result = Consume(table);
        Assert.AreEqual(expectedRows, result.Rows, id);
        Assert.AreEqual(expectedSum, result.Sum, id);
    }

    private static (long Rows, long Sum) Consume(Musoq.Evaluator.Tables.Table table)
    {
        var visitor = new IdSumVisitor();
        long count = 0;
        foreach (var row in table)
        {
            row.VisitColumns(ref visitor);
            count++;
        }
        return (count, visitor.Sum);
    }

    private struct IdSumVisitor : IColumnValueVisitor
    {
        public long Sum { get; private set; }
        public void Visit<T>(int columnIndex, in T value)
        {
            if (columnIndex == 0 && value is int id)
                Sum += id;
        }
    }
}
