using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Csv.Tests;

[TestClass]
public sealed class CsvSourcePlanningExecutionTests : CsvExampleTestBase
{
    [TestMethod]
    public void Execution_WhenPredicateIsAccepted_ShouldFilterInsideCsvSource()
    {
        var path = WriteTempCsv(
            "Name,Amount,Status\n" +
            "Ada,12.5,Open\n" +
            "Bob,8,Open\n" +
            "Cyd,13,Closed\n" +
            "Dee,15,Hold\n");
        var recorder = new CsvDataSourceApiRecorder();
        var query =
            "table CsvShape { Name: string, Amount: decimal, Status: string };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select r.Name from Rows({SqlString(path)}, true) r " +
            "where r.Amount >= 10 and r.Status in ('Open', 'Closed') " +
            "order by r.Name";

        var table = Run(query, new CsvSchemaProvider(recorder));

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Ada", table[0][0]);
        Assert.AreEqual("Cyd", table[1][0]);
        Assert.IsNotNull(recorder.RowSourceCalls.Single().Execution.Plan.AcceptedPredicate);
    }

    [TestMethod]
    public void Execution_WhenOrderSkipTakeAreAccepted_ShouldSortAndSliceInsideCsvSource()
    {
        var path = WriteTempCsv(
            "Name,Amount\n" +
            "Ada,12\n" +
            "Bob,30\n" +
            "Cyd,20\n" +
            "Dee,15\n");
        var recorder = new CsvDataSourceApiRecorder();
        var query =
            "table CsvShape { Name: string, Amount: int };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select r.Name from Rows({SqlString(path)}, true) r " +
            "order by r.Amount desc skip 1 take 2";

        var table = Run(query, new CsvSchemaProvider(recorder));

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Cyd", table[0][0]);
        Assert.AreEqual("Dee", table[1][0]);

        var executionPlan = recorder.RowSourceCalls.Single().Execution.Plan;
        Assert.AreEqual(1, executionPlan.AcceptedOrderBy.Count);
        Assert.AreEqual("Amount", executionPlan.AcceptedOrderBy[0].Column.Name);
        Assert.AreEqual(OrderDirection.Descending, executionPlan.AcceptedOrderBy[0].Direction);
        Assert.AreEqual(1L, executionPlan.AcceptedSkip);
        Assert.AreEqual(2L, executionPlan.AcceptedTake);
    }

    [TestMethod]
    public void Execution_WhenAcceptedPredicateUsesUnprojectedColumn_ShouldStillReadPredicateColumn()
    {
        var path = WriteTempCsv(
            "Name,Amount\n" +
            "Ada,12\n" +
            "Bob,8\n" +
            "Cyd,20\n");
        var query =
            "table CsvShape { Name: string, Amount: int };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select r.Name from Rows({SqlString(path)}, true) r " +
            "where r.Amount > 10 order by r.Name";

        var table = Run(query);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Ada", table[0][0]);
        Assert.AreEqual("Cyd", table[1][0]);
    }

    [TestMethod]
    public void Execution_WhenColumnIsUnusedByPlan_ShouldNotConvertIt()
    {
        var path = WriteTempCsv(
            "Name,Bad\n" +
            "Ada,not-a-decimal\n" +
            "Bob,also-bad\n");
        var query =
            "table CsvShape { Name: string, Bad: decimal };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select r.Name from Rows({SqlString(path)}, true) r where r.Name = 'Ada'";

        var table = Run(query);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Ada", table[0][0]);
    }

    [TestMethod]
    public void Inspection_WhenOrderSkipTakeAreAccepted_ShouldRemovePhysicalSortAndSlice()
    {
        var path = WriteTempCsv(
            "Name,Amount\n" +
            "Ada,12\n" +
            "Bob,30\n" +
            "Cyd,20\n");
        var query =
            "table CsvShape { Name: string, Amount: int };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select r.Name from Rows({SqlString(path)}, true) r " +
            "order by r.Amount desc skip 1 take 1";

        var inspection = Inspect(query);

        StringAssert.Contains(inspection.PlanningText, "source plan accepted:");
        StringAssert.Contains(inspection.PlanningText, "orderBy=1, skip=1, take=1");
        Assert.IsFalse(inspection.OptimizedPhysicalPlanText.Contains("PhysicalSort", StringComparison.Ordinal));
        Assert.IsFalse(inspection.OptimizedPhysicalPlanText.Contains("PhysicalSkip", StringComparison.Ordinal));
        Assert.IsFalse(inspection.OptimizedPhysicalPlanText.Contains("PhysicalTake", StringComparison.Ordinal));
        Assert.IsFalse(inspection.OptimizedPhysicalPlanText.Contains("PhysicalTop", StringComparison.Ordinal));
    }
}
