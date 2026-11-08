using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Diagnostics;

namespace Musoq.Evaluator.Tests.Diagnostics;

[TestClass]
public sealed class ProfiledOperatorEnumerableTests
{
    [TestMethod]
    public void ProfiledOperatorEnumerable_WhenIteratorThrowsInsideActiveOperator_ShouldRecordOperatorException()
    {
        var recorder = new QueryProfileRecorder();
        var rows = ProfiledOperatorEnumerable<int>.Create(ThrowInsideOperator(recorder), recorder, 0);

        using var enumerator = rows.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        var exception = Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.AreEqual("boom", exception.Message);

        var operation = recorder.CreateSnapshot().Operators.Single();
        Assert.AreEqual(1, operation.ExceptionCount);
        Assert.AreEqual(typeof(InvalidOperationException).FullName, operation.ExceptionType);
        Assert.AreEqual("boom", operation.ExceptionMessage);
    }

    [TestMethod]
    public void ProfiledOperatorEnumerable_WhenRecorderIsNull_ShouldReturnSource()
    {
        int[] rows = [1, 2, 3];

        var profiledRows = ProfiledOperatorEnumerable<int>.Create(rows, null, 0);

        Assert.AreSame(rows, profiledRows);
    }

    private static IEnumerable<int> ThrowInsideOperator(QueryProfileRecorder recorder)
    {
        var scope = recorder.BeginOperator("op0", "TestOperator");
        try
        {
            yield return 1;
            throw new InvalidOperationException("boom");
        }
        finally
        {
            scope.Dispose();
        }
    }
}
