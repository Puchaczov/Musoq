using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SubqueryExecutionTests
{
    [TestMethod]
    public void Exists_ShouldDisposeItsInputAfterTheFirstRow()
    {
        var rows = new ObservedRows([1, 2]);
        var result = SubqueryExecution.Exists(rows, CancellationToken.None);
        Assert.IsTrue(result);
        Assert.AreEqual(1, rows.Visited);
        Assert.AreEqual(1, rows.Disposals);
    }

    [TestMethod]
    public void Membership_ShouldStopAtTrueAndDisposeItsInput()
    {
        var rows = new ObservedRows([2, 0]);
        var result = SubqueryExecution.In(2, rows, static value => value,
            static (left, right) => left / right == 1, CancellationToken.None);
        Assert.AreEqual(true, result);
        Assert.AreEqual(1, rows.Visited);
        Assert.AreEqual(1, rows.Disposals);
    }

    [TestMethod]
    public void Scalar_ShouldObserveCancellationAndDisposeItsInput()
    {
        using var cancellation = new CancellationTokenSource();
        var rows = new ObservedRows([1, 2], cancellation.Cancel);
        Assert.Throws<OperationCanceledException>(() => SubqueryExecution.Scalar<int, int?>(rows, static value => value, cancellation.Token));
        Assert.AreEqual(1, rows.Disposals);
    }

    [TestMethod]
    [DataRow("ANY", 2, true)]
    [DataRow("ALL", 1, false)]
    public void Quantifier_ShouldDisposeAfterItsDecisiveComparison(string quantifier, int first, bool expected)
    {
        var rows = new ObservedRows([first, 0]);
        var result = RunQuantifier(quantifier, rows, static (left, right) => left / right == 1, CancellationToken.None);
        Assert.AreEqual(expected, result);
        Assert.AreEqual(1, rows.Visited);
        Assert.AreEqual(1, rows.Disposals);
    }

    [TestMethod]
    [DataRow("ANY")]
    [DataRow("ALL")]
    public void Quantifier_ShouldObserveCancellationAndDisposeItsInput(string quantifier)
    {
        using var cancellation = new CancellationTokenSource();
        var rows = new ObservedRows([1, 2], cancellation.Cancel);
        Assert.Throws<OperationCanceledException>(() =>
            RunQuantifier(quantifier, rows, static (left, right) => left > right, cancellation.Token));
        Assert.AreEqual(1, rows.Disposals);
    }

    private static bool? RunQuantifier(string quantifier, ObservedRows rows, Func<int, int, bool?> compare, CancellationToken token)
    {
        if (quantifier == "ALL")
            return SubqueryExecution.All(2, rows, static value => value, compare, token);
        return SubqueryExecution.Any(2, rows, static value => value, compare, token);
    }

    private sealed class ObservedRows(int[] values, Action? beforeYield = null) : IEnumerable<int>
    {
        internal int Visited { get; private set; }
        internal int Disposals { get; private set; }
        public IEnumerator<int> GetEnumerator()
        {
            try
            {
                foreach (var value in values)
                {
                    Visited++;
                    beforeYield?.Invoke();
                    yield return value;
                }
            }
            finally
            {
                Disposals++;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
