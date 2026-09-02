using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core030RecursiveCteTests : BasicEntityTestBase
{
    [TestMethod]
    public void RecursiveCte_ShouldTerminateAtEmptyFrontierAndPreserveGenerationOrder()
    {
        const string query =
            "with recursive counter (Value, Depth) as (" +
            "select 1, 0 from values {{ Seed: 1 }} seed union all " +
            "select c.Value + 1, c.Depth + 1 from counter c where c.Depth < 2) " +
            "select Value, Depth from counter order by Depth";

        using var vm = CreateAndRunVirtualMachine(query, CreateSingleSource());
        var table = TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Value", typeof(int)),
            ("Depth", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [1, 0],
            [2, 1],
            [3, 2]);
    }

    [TestMethod]
    public void RecursiveCte_ShouldAllowOuterAggregateOverExportedState()
    {
        const string query =
            "with recursive walk (Id, Depth) as (" +
            "select 1, 0 from values {{ Seed: 1 }} seed union all " +
            "select w.Id + 1, w.Depth + 1 from walk w where w.Depth < 3) " +
            "select Count(Id) as Nodes, Max(Depth) as MaxDepth from walk";

        using var vm = CreateAndRunVirtualMachine(query, CreateSingleSource());
        var table = TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Nodes", typeof(long)),
            ("MaxDepth", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [4L, 3]);
    }

    [TestMethod]
    public void RecursiveCteRuntimeLimits_ShouldUseRuntimeEnvelopeWithoutQueryLocation()
    {
        var cases = new[]
        {
            (DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 7),
            (DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 11),
            (DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 13)
        };

        foreach (var (code, limit) in cases)
        {
            var exception = new RecursiveCteLimitExceededException("walk", code, limit);
            var envelope = MusoqErrorEnvelope.FromException(
                exception,
                "with recursive walk (Id) as (select 1 union all select Id from walk) select Id from walk");

            Assert.AreEqual(code, envelope.Code);
            Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
            Assert.AreEqual(DiagnosticPhase.Runtime, envelope.Phase);
            Assert.AreEqual(DiagnosticSourceKind.Runtime, envelope.SourceKind);
            Assert.IsNull(envelope.Offset);
            Assert.IsNull(envelope.EndOffset);
            Assert.IsNull(envelope.Snippet);
            Assert.IsNotNull(envelope.Explanation);
            Assert.IsNotEmpty(envelope.SuggestedFixes);
            Assert.HasCount(envelope.SuggestedFixes.Count, envelope.Actions);
            Assert.IsTrue(envelope.Actions.All(static action =>
                action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
            Assert.AreEqual("walk", envelope.Arguments["cteName"]);
            Assert.AreEqual(limit.ToString(), envelope.Arguments["configuredLimit"]);
        }
    }
}
