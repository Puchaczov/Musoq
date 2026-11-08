using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Schema.Exceptions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class NumericOverflowDiagnosticTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void NumericOverflow_ShouldClassifySupportedFailureWrappers(int wrapper)
    {
        var overflow = new OverflowException("private numeric data");
        Exception cause = wrapper switch
        {
            0 => overflow,
            1 => new TargetInvocationException(overflow),
            2 => new AggregateException(overflow, new TargetInvocationException(overflow)),
            _ => new TargetInvocationException(new AggregateException(new AggregateException(overflow), overflow))
        };

        var failure = QueryExecutionException.ForExecutionFailure("Execution", cause);

        Assert.AreEqual(DiagnosticCode.MQ7018_NumericOverflow, failure.Envelope!.Code);
        Assert.AreEqual(DiagnosticPhase.Runtime, failure.Envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Runtime, failure.Envelope.SourceKind);
        Assert.IsNotEmpty(failure.Envelope.SuggestedFixes);
        Assert.DoesNotContain("private numeric data", failure.FormatJson());
        Assert.AreSame(cause, failure.InnerException);
    }

    [TestMethod]
    [DataRow("select Sum(e.Id) as S from #A.Entities() e")]
    [DataRow("select Sum(e.Money) as S from #A.Entities() e")]
    [DataRow("select e.Money * 2 as S from #A.Entities() e")]
    [DataRow("select Sum(e.Id) over (order by e.Name rows between unbounded preceding and current row) as S from #A.Entities() e")]
    public void IntegralDecimalAndWindowOverflow_ShouldReportNumericExecutionFailure(string sql)
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("A") { Id = int.MaxValue, Money = decimal.MaxValue },
                new BasicEntity("B") { Id = 1, Money = 1m }]
        };
        using var query = CreateAndRunVirtualMachine(sql, sources);

        var failure = Assert.Throws<QueryExecutionException>(() => query.Run(TestContext.CancellationToken).ToArray());

        Assert.AreEqual(DiagnosticCode.MQ7018_NumericOverflow, failure.Envelope!.Code);
        Assert.AreEqual(DiagnosticPhase.Runtime, failure.Envelope.Phase);
    }

    [TestMethod]
    public void CheckedAggregateMergeOverflow_ShouldReceiveTheSameDiagnostic()
    {
        var target = new SumAggregateKernel<int>.State();
        var source = new SumAggregateKernel<int>.State();
        SumAggregateKernel<int>.Set(ref target, int.MaxValue);
        SumAggregateKernel<int>.Set(ref source, 1);
        var overflow = Assert.Throws<OverflowException>(() => SumAggregateKernel<int>.Merge(ref target, in source));

        var failure = QueryExecutionException.ForExecutionFailure("Merge", new AggregateException(overflow));

        Assert.AreEqual(DiagnosticCode.MQ7018_NumericOverflow, failure.Envelope!.Code);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public void MixedParallelOverflow_ShouldRetainConservativeInternalClassification(int otherFailure)
    {
        Exception other = otherFailure switch
        {
            0 => new ArgumentException("unrelated"),
            1 => new OperationCanceledException(),
            _ => InternalDiagnosticException.ForExecution(new OverflowException())
        };
        var failure = QueryExecutionException.ForExecutionFailure("Parallel",
            new AggregateException(new OverflowException(), other));

        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, failure.Envelope!.Code);
    }

    [TestMethod]
    [DataRow(DiagnosticCode.MQ7010_DataSourceOpenFailed)]
    [DataRow(DiagnosticCode.MQ7011_DataSourceReadFailed)]
    [DataRow(DiagnosticCode.MQ7012_DataSourceCleanupFailed)]
    public void SourceBoundaryContainingOverflow_ShouldKeepItsSourceDiagnostic(DiagnosticCode code)
    {
        var cause = new DataSourceLifecycleException(code, "#test", "rows", "r", "source", "Read", new OverflowException());

        var failure = (QueryExecutionException)ExecutionFailureConverter.Convert("Execution", cause);

        Assert.AreEqual(code, failure.Envelope!.Code);
        Assert.AreSame(cause, failure.InnerException);
    }

    [TestMethod]
    public void ExplicitInternalMarkerContainingOverflow_ShouldRemainInternal()
    {
        var marker = InternalDiagnosticException.ForExecution(new OverflowException());

        var failure = QueryExecutionException.ForExecutionFailure("Execution", new TargetInvocationException(marker));

        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, failure.Envelope!.Code);
        Assert.AreEqual(DiagnosticPhase.Internal, failure.Envelope.Phase);
    }

    [TestMethod]
    public void CancellationContainingOverflow_ShouldRetainTheOriginalCancellation()
    {
        var cancellation = new OperationCanceledException("cancelled", new OverflowException());

        Assert.AreSame(cancellation, ExecutionFailureConverter.Convert("Execution", cancellation));
    }

    [TestMethod]
    public void UnknownWrapperContainingOverflow_ShouldRemainInternal()
    {
        var failure = QueryExecutionException.ForExecutionFailure("Execution",
            new InvalidOperationException("Unrelated failure", new OverflowException()));

        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, failure.Envelope!.Code);
    }

    [TestMethod]
    public void SuccessfulNumericAggregate_ShouldRetainTypedExecutionWithoutBoxingOrReflection()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("A") { Id = 1 }, new BasicEntity("B") { Id = 2 }]
        };
        using var query = CreateAndRunVirtualMachine("select Sum(e.Id) as S from #A.Entities() e", sources);
        using var result = query.Run(TestContext.CancellationToken);
        var boxedMethods = GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Where(GeneratedExecutionIlInspection.ContainsBoxOpcode).Select(static method => method.Name).ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("S", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, [3]);
        Assert.IsEmpty(boxedMethods, string.Join(", ", boxedMethods));
    }
}
