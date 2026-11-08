using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests;

public partial class BranchCoverageImprovementTests
{
    #region Exception Branch Coverage — InvalidQueryExpressionTypeException

    [TestMethod]
    public void InvalidQueryExpressionType_WhenCreatedWithDescription_ShouldIncludeTypeName()
    {
        var ex = new InvalidQueryExpressionTypeException("expr", typeof(int), "context");

        Assert.AreEqual(DiagnosticCode.MQ3027_InvalidExpressionType, ex.Code);
        Assert.IsNull(ex.Span);
        StringAssert.Contains(ex.Message, "Int32");
    }

    [TestMethod]
    public void InvalidQueryExpressionType_WhenCreatedWithNullType_ShouldShowNull()
    {
        var ex = new InvalidQueryExpressionTypeException("expr", null, "context");

        StringAssert.Contains(ex.Message, "null");
    }

    [TestMethod]
    public void InvalidQueryExpressionType_WhenCreatedWithFieldNode_ShouldIncludeFieldName()
    {
        var intNode = new IntegerNode("1", "i");
        var fieldNode = new FieldNode(intNode, 0, "testField");
        var ex = new InvalidQueryExpressionTypeException(fieldNode, typeof(string), "context");

        Assert.AreEqual(DiagnosticCode.MQ3027_InvalidExpressionType, ex.Code);
        StringAssert.Contains(ex.Message, "testField");
    }

    [TestMethod]
    public void InvalidQueryExpressionType_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(2, 8);
        var ex = new InvalidQueryExpressionTypeException("msg", span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void InvalidQueryExpressionType_ToDiagnostic_ShouldReturnError()
    {
        var ex = new InvalidQueryExpressionTypeException("expr", typeof(int), "ctx");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3027_InvalidExpressionType, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — SetOperatorMustHaveSameQuantityOfColumnsException

    [TestMethod]
    public void SetOperatorSameQuantity_WhenCreatedParameterless_ShouldSetCode()
    {
        var ex = new SetOperatorMustHaveSameQuantityOfColumnsException();

        Assert.AreEqual(DiagnosticCode.MQ3019_SetOperatorColumnCount, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void SetOperatorSameQuantity_WhenCreatedWithCounts_ShouldSetSpanAndMessage()
    {
        var span = new TextSpan(0, 10);
        var ex = new SetOperatorMustHaveSameQuantityOfColumnsException(3, 5, span);

        Assert.AreEqual(span, ex.Span);
        Assert.IsTrue(ex.Message.Contains('3') || ex.Message.Contains('5'));
    }

    [TestMethod]
    public void SetOperatorSameQuantity_ToDiagnostic_ShouldReturnError()
    {
        var ex = new SetOperatorMustHaveSameQuantityOfColumnsException();
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — VisitorException

    [TestMethod]
    public void VisitorException_WhenCreatedWithNullNames_ShouldCoalesceToUnknown()
    {
        var ex = new VisitorException(null!, null!, "test message");

        Assert.AreEqual("Unknown", ex.VisitorName);
        Assert.AreEqual("Unknown", ex.Operation);
    }

    [TestMethod]
    public void VisitorException_WhenCreatedWithValidNames_ShouldPreserveNames()
    {
        var ex = new VisitorException("MyVisitor", "DoStuff", "msg");

        Assert.AreEqual("MyVisitor", ex.VisitorName);
        Assert.AreEqual("DoStuff", ex.Operation);
    }

    [TestMethod]
    public void VisitorException_WhenCreatedWithDiagnosticInner_ShouldResolveFromInner()
    {
        var innerEx = new ConstructionNotYetSupported("inner", new TextSpan(0, 5));
        var ex = new VisitorException("Vis", "Op", "msg", innerEx);

        Assert.AreEqual(DiagnosticCode.MQ4016_UnsupportedSchemaConstruction, ex.Code);
    }

    [TestMethod]
    public void VisitorException_WhenCreatedWithGenericInner_ShouldUseInternalCode()
    {
        var innerEx = new InvalidOperationException("generic error");
        var ex = new VisitorException("Vis", "Op", "msg", innerEx);

        Assert.AreEqual(DiagnosticCode.MQ9001_InternalCompilerError, ex.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, ex.ToDiagnostic().Severity);
    }

    [TestMethod]
    public void VisitorException_WhenCreatedWithNullInner_ShouldUseDefaultCode()
    {
        var ex = new VisitorException("Vis", "Op", "msg", null!);

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void VisitorException_WhenCreatedWithCodeAndSpan_ShouldSetDirectly()
    {
        var span = new TextSpan(0, 5);
        var ex = new VisitorException("Vis", "Op", "msg", DiagnosticCode.MQ3005_TypeMismatch, span);

        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, ex.Code);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void VisitorException_CreateForStackUnderflow_ShouldCreateWithDetails()
    {
        var ex = VisitorException.CreateForStackUnderflow("TestVisitor", "Visit", 3, 1);

        Assert.AreEqual("TestVisitor", ex.VisitorName);
        Assert.AreEqual("Visit", ex.Operation);
        StringAssert.Contains(ex.Message, "3");
    }

    [TestMethod]
    public void VisitorException_CreateForNullNode_ShouldCreateWithNodeType()
    {
        var ex = VisitorException.CreateForNullNode("TestVisitor", "Visit", "SelectNode");

        StringAssert.Contains(ex.Message, "SelectNode");
    }

    [TestMethod]
    public void VisitorException_CreateForInvalidNodeType_ShouldCreateWithTypes()
    {
        var ex = VisitorException.CreateForInvalidNodeType("TestVisitor", "Visit", "SelectNode", "WhereNode");

        StringAssert.Contains(ex.Message, "SelectNode");
        StringAssert.Contains(ex.Message, "WhereNode");
    }

    [TestMethod]
    public void VisitorException_CreateForProcessingFailure_WithSuggestion_ShouldAppendSuggestion()
    {
        var ex = VisitorException.CreateForProcessingFailure("Vis", "Op", "context", "Try this instead");

        StringAssert.Contains(ex.Message, "context");
        StringAssert.Contains(ex.Message, "Try this instead");
    }

    [TestMethod]
    public void VisitorException_CreateForProcessingFailure_WithoutSuggestion_ShouldNotAppend()
    {
        var ex = VisitorException.CreateForProcessingFailure("Vis", "Op", "context");

        StringAssert.Contains(ex.Message, "context");
    }

    [TestMethod]
    public void VisitorException_CreateForProcessingFailure_WithEmptySuggestion_ShouldNotAppend()
    {
        var ex = VisitorException.CreateForProcessingFailure("Vis", "Op", "context", "");

        StringAssert.Contains(ex.Message, "context");
    }

    [TestMethod]
    public void VisitorException_ToDiagnostic_WhenSpanNull_ShouldUseEmpty()
    {
        var ex = new VisitorException("Vis", "Op", "msg");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [TestMethod]
    public void VisitorException_ToDiagnostic_WhenSpanSet_ShouldUseSpan()
    {
        var span = new TextSpan(5, 10);
        var ex = new VisitorException("Vis", "Op", "msg", DiagnosticCode.MQ3005_TypeMismatch, span);
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — SetOperatorMustHaveSameTypesOfColumnsException

    [TestMethod]
    public void SetOperatorSameTypes_WhenCreatedWithFieldNodes_ShouldSetCode()
    {
        var leftExpr = new IntegerNode("1", "i");
        var rightExpr = new IntegerNode("2", "i");
        var left = new FieldNode(leftExpr, 0, "left");
        var right = new FieldNode(rightExpr, 1, "right");

        var ex = new SetOperatorMustHaveSameTypesOfColumnsException(left, right);

        Assert.AreEqual(DiagnosticCode.MQ3020_SetOperatorColumnTypes, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void SetOperatorSameTypes_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 10);
        var ex = new SetOperatorMustHaveSameTypesOfColumnsException("type mismatch", span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void SetOperatorSameTypes_ToDiagnostic_ShouldReturnError()
    {
        var span = new TextSpan(0, 5);
        var ex = new SetOperatorMustHaveSameTypesOfColumnsException("msg", span);
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3020_SetOperatorColumnTypes, diagnostic.Code);
    }

    #endregion
}
