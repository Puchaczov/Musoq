using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class BranchCoverageImprovementTests
{
    #region Exception Branch Coverage - GroupByIndexOutOfRangeException

    [TestMethod]
    public void GroupByIndexOutOfRange_WhenCreatedWithoutSpan_ShouldSetProperties()
    {
        var ex = new GroupByIndexOutOfRangeException(5, 3);

        Assert.AreEqual(5, ex.Ordinal);
        Assert.AreEqual(3, ex.SelectFields);
        Assert.AreEqual(DiagnosticCode.MQ3024_GroupByIndexOutOfRange, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void GroupByIndexOutOfRange_WhenCreatedWithSpan_ShouldSetAllProperties()
    {
        var span = new TextSpan(1, 5);
        var ex = new GroupByIndexOutOfRangeException(2, 4, span);

        Assert.AreEqual(2, ex.Ordinal);
        Assert.AreEqual(4, ex.SelectFields);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void GroupByIndexOutOfRange_ToDiagnostic_ShouldReturnError()
    {
        var ex = new GroupByIndexOutOfRangeException(5, 3);

        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3024_GroupByIndexOutOfRange, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — ObjectDoesNotImplementIndexerException

    [TestMethod]
    public void ObjectDoesNotImplementIndexer_WhenCreatedWithMessage_ShouldSetCode()
    {
        var ex = new ObjectDoesNotImplementIndexerException("test");

        Assert.AreEqual(DiagnosticCode.MQ3018_NoIndexer, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void ObjectDoesNotImplementIndexer_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var ex = new ObjectDoesNotImplementIndexerException("test", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3018_NoIndexer, ex.Code);
    }

    [TestMethod]
    public void ObjectDoesNotImplementIndexer_ToDiagnostic_ShouldReturnError()
    {
        var ex = new ObjectDoesNotImplementIndexerException("test");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — ObjectIsNotAnArrayException

    [TestMethod]
    public void ObjectIsNotAnArray_WhenCreatedWithMessage_ShouldSetCode()
    {
        var ex = new ObjectIsNotAnArrayException("test");

        Assert.AreEqual(DiagnosticCode.MQ3017_ObjectNotArray, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void ObjectIsNotAnArray_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var ex = new ObjectIsNotAnArrayException("test", span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void ObjectIsNotAnArray_ToDiagnostic_ShouldReturnError()
    {
        var ex = new ObjectIsNotAnArrayException("test");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — TypeNotFoundException

    [TestMethod]
    public void TypeNotFound_WhenCreatedWithMessage_ShouldSetCodeAndNoTypeName()
    {
        var ex = new TypeNotFoundException("test message");

        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, ex.Code);
        Assert.IsNull(ex.TypeName);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void TypeNotFound_WhenCreatedWithEmptyContext_ShouldNotAppendContextSuffix()
    {
        var span = new TextSpan(0, 5);
        var ex = new TypeNotFoundException("MyType", "", span);

        Assert.AreEqual("MyType", ex.TypeName);
        Assert.AreEqual(span, ex.Span);
        Assert.AreNotEqual(string.Empty, ex.Message);
    }

    [TestMethod]
    public void TypeNotFound_WhenCreatedWithNonEmptyContext_ShouldAppendContextSuffix()
    {
        var span = new TextSpan(0, 5);
        var ex = new TypeNotFoundException("MyType", "some context", span);

        StringAssert.Contains(ex.Message, "some context");
    }

    [TestMethod]
    public void TypeNotFound_ToDiagnostic_ShouldReturnError()
    {
        var ex = new TypeNotFoundException("test");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — TypeMismatchException

    [TestMethod]
    public void TypeMismatch_WhenCreated_ShouldSetAllProperties()
    {
        var span = new TextSpan(0, 5);
        var ex = new TypeMismatchException(typeof(int), typeof(string), span);

        Assert.AreEqual(typeof(int), ex.ExpectedType);
        Assert.AreEqual(typeof(string), ex.ActualType);
        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, ex.Code);
    }

    [TestMethod]
    public void TypeMismatch_ToDiagnostic_ShouldReturnError()
    {
        var ex = new TypeMismatchException(typeof(int), typeof(string), new TextSpan(0, 5));
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion
}
