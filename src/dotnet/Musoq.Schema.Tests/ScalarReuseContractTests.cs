using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Optimization;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class ScalarReuseContractTests
{
    [TestMethod]
    public void PortableExpressionFactsPropagateVolatility()
    {
        var column = new SourceScalarColumn("Value", typeof(int));
        var stable = new SourceScalarBinary(
            SourceScalarBinaryOperator.Add,
            column,
            new SourceScalarLiteral(1, typeof(int)),
            typeof(int));
        var volatileExpression = stable with { ExpressionStability = Musoq.Schema.ColumnStability.Volatile };

        Assert.IsTrue(SourceScalarExpressionFacts.IsStable(stable));
        Assert.IsFalse(SourceScalarExpressionFacts.IsStable(volatileExpression));
        Assert.AreNotEqual(
            SourceScalarExpressionFingerprint.Compute(stable),
            SourceScalarExpressionFingerprint.Compute(volatileExpression));
    }

    [TestMethod]
    public void LegacyPlanHelpersPreserveComputedProjectionPartitions()
    {
        var request = new SourcePlanRequest
        {
            Identity = SourceIdentity.Empty,
            RequestedComputedProjections =
            [
                new SourceComputedProjection(
                    "Total",
                    new SourceScalarLiteral(42, typeof(int)),
                    typeof(int))
            ],
            Replayability = RowStreamReplayability.Materialized
        };

        var accepted = SourcePlanResult.AcceptAll(request);
        var rejected = SourcePlanResult.RejectAll(request);

        Assert.AreEqual(1, accepted.AcceptedComputedProjections.Count);
        Assert.AreEqual(RowStreamReplayability.Materialized, accepted.ExecutionPlan.Replayability);
        Assert.AreEqual(1, rejected.ResidualComputedProjections.Count);
        Assert.AreEqual(RowStreamReplayability.Materialized, rejected.ExecutionPlan.Replayability);
    }

    [TestMethod]
    public void QueryRowShapeFingerprintIncludesNonDefaultStabilityAndReplayability()
    {
        var stable = new QueryRowShape(
            [new QueryRowField(0, 0, "Value", typeof(int), false)]);
        var volatileShape = new QueryRowShape(
            [new QueryRowField(0, 0, "Value", typeof(int), false, ColumnStability.Volatile)],
            RowStreamReplayability.Materialized);

        Assert.AreNotEqual(stable.Fingerprint, volatileShape.Fingerprint);
        Assert.AreEqual(RowStreamReplayability.Materialized, volatileShape.Replayability);
    }

    [TestMethod]
    public void ComputedProjectionNegotiationAcceptsStableSubsetAndLeavesResiduals()
    {
        var literal = new SourceComputedProjection("A", new SourceScalarLiteral(1, typeof(int)), typeof(int));
        var column = new SourceComputedProjection("B", new SourceScalarColumn("Value", typeof(int)), typeof(int));

        var accepted = SourceComputedProjectionNegotiator.TryPartition(
            [literal, column],
            [literal],
            SourceComputedProjectionCapabilities.Literals,
            out var partition,
            out var diagnostic);

        Assert.IsTrue(accepted);
        Assert.AreEqual(string.Empty, diagnostic);
        Assert.AreEqual(1, partition.Accepted.Count);
        Assert.AreEqual("B", partition.Residual[0].Name);
    }

    [TestMethod]
    public void ComputedProjectionNegotiationRejectsVolatileOrUnknownAcceptance()
    {
        var volatileProjection = new SourceComputedProjection(
            "A",
            new SourceScalarLiteral(1, typeof(int)),
            typeof(int),
            Musoq.Schema.ColumnStability.Volatile);
        var unknown = volatileProjection with { Name = "Unknown" };

        Assert.IsFalse(SourceComputedProjectionNegotiator.TryPartition(
            [volatileProjection], [volatileProjection], SourceComputedProjectionCapabilities.AllPortable,
            out _, out var volatileDiagnostic));
        Assert.Contains("unstable", volatileDiagnostic);

        Assert.IsFalse(SourceComputedProjectionNegotiator.TryPartition(
            [volatileProjection], [unknown], SourceComputedProjectionCapabilities.AllPortable,
            out _, out var unknownDiagnostic));
        Assert.Contains("unknown", unknownDiagnostic);
    }
}
