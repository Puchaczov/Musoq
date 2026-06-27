using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Csv.Tests;

[TestClass]
public sealed class CsvSourceExecutionTests : CsvExampleTestBase
{
    [TestMethod]
    public void Source_WhenEnumeratedInScaffoldWave_ShouldReportEmptyProgress()
    {
        var events = new List<DataSourceEventArgs>();
        var context = new SourceExecutionContext(
            "execution-query",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            [],
            new Dictionary<string, string>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
            (_, args) => events.Add(args));
        var source = new CsvFileSource(context);

        var rows = source.Chunks.SelectMany(static chunk => chunk).ToArray();

        Assert.AreEqual(0, rows.Length);
        CollectionAssert.AreEqual(
            new[]
            {
                DataSourcePhase.Begin,
                DataSourcePhase.RowsKnown,
                DataSourcePhase.End
            },
            events.Select(static item => item.Phase).ToArray());
    }
}
