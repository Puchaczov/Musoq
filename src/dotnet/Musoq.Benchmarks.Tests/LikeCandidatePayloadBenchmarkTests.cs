using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class LikeCandidatePayloadBenchmarkTests
{
    [TestMethod]
    [DataRow(10_000)]
    [DataRow(100_000)]
    public void CandidateMetadataOn_ShouldOpenOnlyMatchingPayloadsAndPreserveResults(int rowsCount)
    {
        using var benchmark = new LikeCandidatePayloadBenchmark { RowsCount = rowsCount };
        benchmark.Setup();

        benchmark.ResetPayloadCounters();
        var baseline = benchmark.CandidatePayload_SourcePlanningOff();
        var baselineIds = baseline.Select(row => (int)row[0]).ToArray();
        var baselinePayloads = baseline.Select(row => (string)row[1]).ToArray();
        var baselineOpens = benchmark.CandidateMetadataOffPayloadOpens;

        benchmark.ResetPayloadCounters();
        var optimized = benchmark.CandidatePayload_SourcePlanningOn();
        var optimizedIds = optimized.Select(row => (int)row[0]).ToArray();
        var optimizedPayloads = optimized.Select(row => (string)row[1]).ToArray();

        CollectionAssert.AreEqual(baselineIds, optimizedIds);
        CollectionAssert.AreEqual(baselinePayloads, optimizedPayloads);
        Assert.AreEqual(ExpectedResultHash, ComputeHash(baseline));
        Assert.AreEqual(ExpectedResultHash, ComputeHash(optimized));
        Assert.AreEqual(1_000, optimized.Count);
        Assert.AreEqual(rowsCount, baselineOpens);
        Assert.AreEqual(1_000, benchmark.CandidateMetadataOnPayloadOpens);
    }

    private static string ComputeHash(Table table)
    {
        var text = new StringBuilder();
        foreach (var row in table)
        {
            foreach (var value in row.Values)
            {
                var formatted = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>";
                text.Append(formatted.Length).Append(':').Append(formatted).Append('|');
            }

            text.AppendLine();
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }

    private const string ExpectedResultHash =
        "E01E398494E8A0FB7094309FBBF66574308CF0AC034748B46B4091C857440178";
}
