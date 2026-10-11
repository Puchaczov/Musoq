using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RuntimeExpressionFailureDiagnosticTests
{
    [TestMethod]
    public void ScalarFailureInsideInvocationAndAggregate_ShouldRetainSpecificCode()
    {
        var cause = Assert.Throws<InvalidOperationException>(() =>
            EvaluationHelper.ThrowScalarSubqueryCardinalityViolation());
        var wrapper = new AggregateException(new TargetInvocationException(cause), cause);
        var failure = QueryExecutionException.ForExecutionFailure("Execution", wrapper);
        Assert.AreEqual(DiagnosticCode.MQ7016_ScalarSubqueryCardinalityViolation, failure.Envelope!.Code);
        Assert.AreSame(wrapper, failure.InnerException);
    }

    [TestMethod]
    public void UnrelatedFailureWithCardinalityMessage_ShouldRemainInternal()
    {
        var cause = new InvalidOperationException("Scalar subquery returned more than one row.");
        var failure = QueryExecutionException.ForExecutionFailure("Execution", cause);
        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, failure.Envelope!.Code);
    }

    [TestMethod]
    public void MixedAggregate_ShouldNotHideAnUnrelatedFailure()
    {
        var cause = Assert.Throws<InvalidOperationException>(() =>
            EvaluationHelper.ThrowScalarSubqueryCardinalityViolation());
        var failure = QueryExecutionException.ForExecutionFailure("Execution",
            new AggregateException(cause, new ArgumentException("unrelated")));
        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, failure.Envelope!.Code);
    }

    [TestMethod]
    public void RegexTimeout_ShouldNotBeClassifiedAsInvalidRegexSyntax()
    {
        var cause = new RegexMatchTimeoutException("private input", "private pattern", TimeSpan.FromSeconds(1));
        var failure = QueryExecutionException.ForExecutionFailure("Execution", cause);
        Assert.AreEqual(DiagnosticCode.MQ7017_RegexMatchTimeout, failure.Envelope!.Code);
        Assert.AreSame(cause, failure.InnerException);
    }

    [TestMethod]
    public void RegexTimeout_WhenWrapped_ShouldRetainSafeFactsWithoutPatternOrInput()
    {
        var original = new RegexMatchTimeoutException("private input", "private pattern", TimeSpan.FromSeconds(1));
        var cause = new RegexExecutionTimeoutException("RLIKE", "Backtracking", original);
        var failure = QueryExecutionException.ForExecutionFailure("Execution",
            new AggregateException(new TargetInvocationException(cause), cause));

        Assert.AreEqual(DiagnosticCode.MQ7017_RegexMatchTimeout, failure.Envelope!.Code);
        Assert.AreEqual("RLIKE", failure.Envelope.Arguments["operator"]);
        Assert.AreEqual("Backtracking", failure.Envelope.Arguments["engine"]);
        Assert.AreEqual("1000", failure.Envelope.Arguments["timeoutMilliseconds"]);
        Assert.IsFalse(failure.FormatText().Contains("private", StringComparison.Ordinal));
        Assert.IsFalse(failure.FormatJson().Contains("private", StringComparison.Ordinal));
        Assert.AreSame(original, cause.InnerException);
    }

    [TestMethod]
    public void RegexTimeout_WhenMixedWithUnrelatedFailure_ShouldRemainInternal()
    {
        var cause = new RegexExecutionTimeoutException("LIKE", "NonBacktracking",
            new RegexMatchTimeoutException("private input", "private pattern", TimeSpan.FromSeconds(1)));

        var failure = QueryExecutionException.ForExecutionFailure("Execution",
            new AggregateException(cause, new InvalidOperationException("unrelated")));

        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, failure.Envelope!.Code);
    }

    [TestMethod]
    public void Cancellation_ShouldRetainOriginalException()
    {
        var cause = new OperationCanceledException();

        Assert.AreSame(cause, ExecutionFailureConverter.Convert("Execution", cause));
    }
}
