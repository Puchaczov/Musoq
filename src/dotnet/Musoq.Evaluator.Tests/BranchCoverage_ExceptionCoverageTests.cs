using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.Api;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

public partial class BranchCoverageImprovementTests
{
    #region Exception Branch Coverage — ConstructionNotYetSupported

    [TestMethod]
    public void ConstructionNotYetSupported_WhenCreatedWithMessage_ShouldSetCodeAndNullSpan()
    {
        var ex = new ConstructionNotYetSupported("test message");

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, ex.Code);
        Assert.IsNull(ex.Span);
        Assert.AreEqual("test message", ex.Message);
    }

    [TestMethod]
    public void ConstructionNotYetSupported_WhenCreatedWithSpan_ShouldSetCodeAndSpan()
    {
        var span = new TextSpan(5, 10);
        var ex = new ConstructionNotYetSupported("test", span);

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, ex.Code);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void ConstructionNotYetSupported_ToDiagnostic_WhenSpanIsNull_ShouldUseEmptySpan()
    {
        var ex = new ConstructionNotYetSupported("test message");

        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual("test message", diagnostic.Message);
    }

    [TestMethod]
    public void ConstructionNotYetSupported_ToDiagnostic_WhenSpanIsSet_ShouldUseSpan()
    {
        var span = new TextSpan(5, 10);
        var ex = new ConstructionNotYetSupported("test", span);

        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, diagnostic.Code);
        Assert.AreEqual("test", diagnostic.Message);
    }

    #endregion

    #region Exception Branch Coverage — FieldLinkIndexOutOfRangeException

    [TestMethod]
    public void FieldLinkIndexOutOfRange_WhenCreatedWithoutSpan_ShouldSetProperties()
    {
        var ex = new FieldLinkIndexOutOfRangeException(5, 3);

        Assert.AreEqual(5, ex.Index);
        Assert.AreEqual(3, ex.MaxGroups);
        Assert.AreEqual(DiagnosticCode.MQ3024_GroupByIndexOutOfRange, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void FieldLinkIndexOutOfRange_WhenCreatedWithSpan_ShouldSetAllProperties()
    {
        var span = new TextSpan(1, 5);
        var ex = new FieldLinkIndexOutOfRangeException(2, 4, span);

        Assert.AreEqual(2, ex.Index);
        Assert.AreEqual(4, ex.MaxGroups);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void FieldLinkIndexOutOfRange_ToDiagnostic_ShouldReturnError()
    {
        var ex = new FieldLinkIndexOutOfRangeException(5, 3);

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
        Assert.IsTrue(ex.Message.Contains("3") || ex.Message.Contains("5"));
    }

    [TestMethod]
    public void SetOperatorSameQuantity_ToDiagnostic_ShouldReturnError()
    {
        var ex = new SetOperatorMustHaveSameQuantityOfColumnsException();
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

    #region Exception Branch Coverage — ColumnMustBeAnArrayOrImplementIEnumerableException

    [TestMethod]
    public void ColumnMustBeArray_WhenCreatedParameterless_ShouldSetCode()
    {
        var ex = new ColumnMustBeAnArrayOrImplementIEnumerableException();

        Assert.AreEqual(DiagnosticCode.MQ3025_ColumnMustBeArray, ex.Code);
        Assert.IsNull(ex.ColumnName);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void ColumnMustBeArray_WhenCreatedWithColumnAndSpan_ShouldSetProperties()
    {
        var span = new TextSpan(0, 5);
        var ex = new ColumnMustBeAnArrayOrImplementIEnumerableException("col1", span);

        Assert.AreEqual("col1", ex.ColumnName);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void ColumnMustBeArray_ToDiagnostic_ShouldReturnError()
    {
        var ex = new ColumnMustBeAnArrayOrImplementIEnumerableException();
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — ColumnMustBeMarkedAsBindablePropertyAsTableException

    [TestMethod]
    public void ColumnMustBeBindable_WhenCreatedParameterless_ShouldSetCode()
    {
        var ex = new ColumnMustBeMarkedAsBindablePropertyAsTableException();

        Assert.AreEqual(DiagnosticCode.MQ3026_ColumnNotBindable, ex.Code);
        Assert.IsNull(ex.ColumnName);
    }

    [TestMethod]
    public void ColumnMustBeBindable_WhenCreatedWithColumnAndSpan_ShouldSetProperties()
    {
        var span = new TextSpan(0, 5);
        var ex = new ColumnMustBeMarkedAsBindablePropertyAsTableException("col1", span);

        Assert.AreEqual("col1", ex.ColumnName);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void ColumnMustBeBindable_ToDiagnostic_ShouldReturnError()
    {
        var ex = new ColumnMustBeMarkedAsBindablePropertyAsTableException();
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — TableIsNotDefinedException

    [TestMethod]
    public void TableIsNotDefined_WhenCreatedWithTableName_ShouldSetProperties()
    {
        var ex = new TableIsNotDefinedException("MyTable");

        Assert.AreEqual("MyTable", ex.TableName);
        Assert.AreEqual(DiagnosticCode.MQ3023_TableNotDefined, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void TableIsNotDefined_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var ex = new TableIsNotDefinedException("MyTable", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual("MyTable", ex.TableName);
    }

    [TestMethod]
    public void TableIsNotDefined_ToDiagnostic_ShouldReturnError()
    {
        var ex = new TableIsNotDefinedException("MyTable");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3023_TableNotDefined, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — UnresolvableMethodException

    [TestMethod]
    public void UnresolvableMethod_WhenCreatedWithMessage_ShouldSetCode()
    {
        var ex = new UnresolvableMethodException("test");

        Assert.AreEqual(DiagnosticCode.MQ3004_UnknownFunction, ex.Code);
        Assert.IsNull(ex.MethodName);
        Assert.IsNull(ex.ArgumentTypes);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void UnresolvableMethod_WhenCreatedWithDetails_ShouldSetAllProperties()
    {
        var span = new TextSpan(0, 5);
        var argTypes = new[] { "int", "string" };
        var ex = new UnresolvableMethodException("DoWork", argTypes, span);

        Assert.AreEqual("DoWork", ex.MethodName);
        Assert.AreEqual(argTypes, ex.ArgumentTypes);
        Assert.AreEqual(span, ex.Span);
        StringAssert.Contains(ex.Message, "DoWork");
    }

    [TestMethod]
    public void UnresolvableMethod_ToDiagnostic_ShouldReturnError()
    {
        var ex = new UnresolvableMethodException("test");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3004_UnknownFunction, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — AliasAlreadyUsedException

    [TestMethod]
    public void AliasAlreadyUsed_WhenCreatedWithSchemaFromNodeWithoutSpan_ShouldHaveNullSpan_ViaBranch()
    {
        var node = new SchemaFromNode("schema", "method", ArgsListNode.Empty, "alias", typeof(object), 0);
        var ex = new AliasAlreadyUsedException(node, "myAlias");

        Assert.AreEqual("myAlias", ex.Alias);
        Assert.AreEqual(DiagnosticCode.MQ3021_DuplicateAlias, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void AliasAlreadyUsed_WhenCreatedWithAliasAndSpanOverload_ShouldSetSpanAndAlias()
    {
        var span = new TextSpan(0, 5);
        var ex = new AliasAlreadyUsedException("duplicateAlias", span);

        Assert.AreEqual("duplicateAlias", ex.Alias);
        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3021_DuplicateAlias, ex.Code);
    }

    [TestMethod]
    public void AliasAlreadyUsed_WhenCreatedWithAliasAndSpan_ShouldSetProperties()
    {
        var span = new TextSpan(0, 5);
        var ex = new AliasAlreadyUsedException("myAlias", span);

        Assert.AreEqual("myAlias", ex.Alias);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void AliasAlreadyUsed_ToDiagnostic_ShouldReturnError()
    {
        var span = new TextSpan(0, 5);
        var ex = new AliasAlreadyUsedException("alias", span);
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3021_DuplicateAlias, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — SetOperatorMustHaveKeyColumnsException

    [TestMethod]
    public void SetOperatorKeyColumns_WhenCreatedWithUnion_ShouldCreateCorrectMessage()
    {
        var ex = new SetOperatorMustHaveKeyColumnsException("Union");

        StringAssert.Contains(ex.Message, "UNION");
        StringAssert.Contains(ex.Message, "UNION (<key_columns>)");
    }

    [TestMethod]
    public void SetOperatorKeyColumns_WhenCreatedWithUnionAll_ShouldCreateCorrectMessage()
    {
        var ex = new SetOperatorMustHaveKeyColumnsException("UnionAll");

        StringAssert.Contains(ex.Message, "UNION ALL");
        StringAssert.Contains(ex.Message, "UNION ALL (<key_columns>)");
    }

    [TestMethod]
    public void SetOperatorKeyColumns_WhenCreatedWithExcept_ShouldCreateCorrectMessage()
    {
        var ex = new SetOperatorMustHaveKeyColumnsException("Except");

        StringAssert.Contains(ex.Message, "EXCEPT");
        StringAssert.Contains(ex.Message, "EXCEPT (<key_columns>)");
    }

    [TestMethod]
    public void SetOperatorKeyColumns_WhenCreatedWithIntersect_ShouldCreateCorrectMessage()
    {
        var ex = new SetOperatorMustHaveKeyColumnsException("Intersect");

        StringAssert.Contains(ex.Message, "INTERSECT");
        StringAssert.Contains(ex.Message, "INTERSECT (<key_columns>)");
    }

    [TestMethod]
    public void SetOperatorKeyColumns_WhenCreatedWithUnknownOperator_ShouldUseFallback()
    {
        var ex = new SetOperatorMustHaveKeyColumnsException("CustomOp");

        StringAssert.Contains(ex.Message, "CUSTOMOP");
    }

    [TestMethod]
    public void SetOperatorKeyColumns_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(1, 10);
        var ex = new SetOperatorMustHaveKeyColumnsException("Union", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3031_SetOperatorMissingKeys, ex.Code);
    }

    [TestMethod]
    public void SetOperatorKeyColumns_ToDiagnostic_ShouldReturnError()
    {
        var ex = new SetOperatorMustHaveKeyColumnsException("Union");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3031_SetOperatorMissingKeys, diagnostic.Code);
    }

    [TestMethod]
    public void SetOperatorKeyColumns_CreateMessage_ShouldCombineSyntaxAndDisplayName()
    {
        var message = SetOperatorMustHaveKeyColumnsException.CreateMessage("Intersect");

        StringAssert.Contains(message, "INTERSECT (<key_columns>)");
        StringAssert.Contains(message, "INTERSECT");
    }

    #endregion

    #region Exception Branch Coverage — VisitorException

    [TestMethod]
    public void VisitorException_WhenCreatedWithNullNames_ShouldCoalesceToUnknown()
    {
        var ex = new VisitorException(null, null, "test message");

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

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, ex.Code);
    }

    [TestMethod]
    public void VisitorException_WhenCreatedWithGenericInner_ShouldUseFallbackCode()
    {
        var innerEx = new InvalidOperationException("generic error");
        var ex = new VisitorException("Vis", "Op", "msg", innerEx);

        Assert.AreEqual(DiagnosticSeverity.Error, ex.ToDiagnostic().Severity);
    }

    [TestMethod]
    public void VisitorException_WhenCreatedWithNullInner_ShouldUseDefaultCode()
    {
        var ex = new VisitorException("Vis", "Op", "msg", (Exception)null);

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
        var ex = VisitorException.CreateForProcessingFailure("Vis", "Op", "context", null);

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

    #region Exception Branch Coverage — AliasMissingException

    [TestMethod]
    public void AliasMissing_WhenCreatedWithAccessMethodNode_ShouldSetCode()
    {
        var funcToken = new FunctionToken("Count", new TextSpan(0, 5));
        var node = new AccessMethodNode(funcToken, ArgsListNode.Empty, ArgsListNode.Empty, true);

        var ex = new AliasMissingException(node);

        Assert.AreEqual(DiagnosticCode.MQ3022_MissingAlias, ex.Code);
        StringAssert.Contains(ex.Message, "Count");
    }

    [TestMethod]
    public void AliasMissing_WhenCreatedWithMessageAndSpan_ShouldSetProperties()
    {
        var span = new TextSpan(0, 5);
        var ex = new AliasMissingException("test message", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3022_MissingAlias, ex.Code);
    }

    [TestMethod]
    public void AliasMissing_ToDiagnostic_ShouldReturnError()
    {
        var span = new TextSpan(0, 5);
        var ex = new AliasMissingException("test", span);
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3022_MissingAlias, diagnostic.Code);
    }

    [TestMethod]
    public void AliasMissing_CreateMethodCallMessage_ShouldFormatCorrectly()
    {
        var message = AliasMissingException.CreateMethodCallMessage("Sum(col)");

        StringAssert.Contains(message, "Sum(col)");
        StringAssert.Contains(message, "alias");
    }

    #endregion

    #region Exception Branch Coverage — AmbiguousAggregateOwnerException

    [TestMethod]
    public void AmbiguousAggregateOwner_WhenCreatedWithoutSpan_ShouldSetCode()
    {
        var aliases = new[] { "a", "b" };
        var ex = new AmbiguousAggregateOwnerException("Count(*)", aliases);

        Assert.AreEqual(DiagnosticCode.MQ3034_AmbiguousAggregateOwner, ex.Code);
        Assert.IsNull(ex.Span);
        StringAssert.Contains(ex.Message, "Count(*)");
    }

    [TestMethod]
    public void AmbiguousAggregateOwner_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var aliases = new[] { "a", "b" };
        var ex = new AmbiguousAggregateOwnerException("Count(*)", aliases, span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void AmbiguousAggregateOwner_ToDiagnostic_ShouldReturnError()
    {
        var ex = new AmbiguousAggregateOwnerException("Count(*)", new[] { "a" });
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — AmbiguousMethodOwnerException

    [TestMethod]
    public void AmbiguousMethodOwner_WhenCreatedWithoutSpan_ShouldSetCode()
    {
        var aliases = new[] { "x", "y" };
        var ex = new AmbiguousMethodOwnerException("DoWork()", aliases);

        Assert.AreEqual(DiagnosticCode.MQ3035_AmbiguousMethodOwner, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void AmbiguousMethodOwner_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var ex = new AmbiguousMethodOwnerException("DoWork()", new[] { "x" }, span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void AmbiguousMethodOwner_ToDiagnostic_ShouldReturnError()
    {
        var ex = new AmbiguousMethodOwnerException("DoWork()", new[] { "x" });
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — NonAggregatedColumnInSelectException

    [TestMethod]
    public void NonAggregatedColumn_WhenCreatedWithGroupByColumns_ShouldSetProperties()
    {
        var groupByCols = new[] { "Name", "Age" };
        var ex = new NonAggregatedColumnInSelectException("City", groupByCols);

        Assert.AreEqual("City", ex.ColumnName);
        Assert.AreEqual(groupByCols, ex.GroupByColumns);
        Assert.AreEqual(DiagnosticCode.MQ3012_NonAggregateInSelect, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void NonAggregatedColumn_WhenCreatedWithEmptyGroupBy_ShouldShowNone()
    {
        var ex = new NonAggregatedColumnInSelectException("City", []);

        StringAssert.Contains(ex.Message, "(none)");
    }

    [TestMethod]
    public void NonAggregatedColumn_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var ex = new NonAggregatedColumnInSelectException("City", new[] { "Name" }, span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void NonAggregatedColumn_ToDiagnostic_ShouldReturnError()
    {
        var ex = new NonAggregatedColumnInSelectException("City", new[] { "Name" });
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3012_NonAggregateInSelect, diagnostic.Code);
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

    #region Exception Branch Coverage — AmbiguousColumnException

    [TestMethod]
    public void AmbiguousColumn_WhenCreatedWithoutSpan_ShouldSetProperties()
    {
        var ex = new AmbiguousColumnException("Name", "a", "b");

        Assert.AreEqual("Name", ex.ColumnName);
        Assert.AreEqual("a", ex.Alias1);
        Assert.AreEqual("b", ex.Alias2);
        Assert.AreEqual(DiagnosticCode.MQ3002_AmbiguousColumn, ex.Code);
        Assert.IsNull(ex.Span);
        StringAssert.Contains(ex.Message, "Name");
    }

    [TestMethod]
    public void AmbiguousColumn_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(5, 10);
        var ex = new AmbiguousColumnException("Col", "x", "y", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual("Col", ex.ColumnName);
        Assert.AreEqual("x", ex.Alias1);
        Assert.AreEqual("y", ex.Alias2);
        Assert.AreEqual(DiagnosticCode.MQ3002_AmbiguousColumn, ex.Code);
    }

    [TestMethod]
    public void AmbiguousColumn_ToDiagnostic_WithoutSpan_ShouldUseEmptySpan()
    {
        var ex = new AmbiguousColumnException("Name", "a", "b");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3002_AmbiguousColumn, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [TestMethod]
    public void AmbiguousColumn_ToDiagnostic_WithSpan_ShouldUseProvidedSpan()
    {
        var span = new TextSpan(5, 10);
        var ex = new AmbiguousColumnException("Name", "a", "b", span);
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3002_AmbiguousColumn, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — CannotResolveMethodException

    [TestMethod]
    public void CannotResolveMethod_WhenCreatedWithMessage_ShouldSetDefaults()
    {
        var ex = new CannotResolveMethodException("test error");

        Assert.AreEqual("test error", ex.Message);
        Assert.AreEqual(DiagnosticCode.MQ3029_UnresolvableMethod, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void CannotResolveMethod_WhenCreatedWithMessageAndSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var ex = new CannotResolveMethodException("error", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3029_UnresolvableMethod, ex.Code);
    }

    [TestMethod]
    public void CannotResolveMethod_WhenCreatedWithCustomCode_ShouldUseProvidedCode()
    {
        var span = new TextSpan(0, 5);
        var ex = new CannotResolveMethodException("error", DiagnosticCode.MQ3004_UnknownFunction, span);

        Assert.AreEqual(DiagnosticCode.MQ3004_UnknownFunction, ex.Code);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void CannotResolveMethod_ToDiagnostic_ShouldReturnError()
    {
        var ex = new CannotResolveMethodException("cannot resolve");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3029_UnresolvableMethod, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [TestMethod]
    public void CannotResolveMethod_CreateForNullArguments_ShouldContainMethodName()
    {
        var ex = CannotResolveMethodException.CreateForNullArguments("Foo");

        StringAssert.Contains(ex.Message, "Foo");
        StringAssert.Contains(ex.Message, "null arguments");
    }

    [TestMethod]
    public void CannotResolveMethod_CreateForCannotMatch_WithArgs_ShouldListTypes()
    {
        var args = new Node[] { new IntegerNode("1", "i"), new IntegerNode("2", "i") };
        var ex = CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments("Bar", args);

        StringAssert.Contains(ex.Message, "Bar");
    }

    [TestMethod]
    public void CannotResolveMethod_CreateForCannotMatch_WithEmptyArgs_ShouldUseEmptyTypes()
    {
        var ex = CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments("Baz", []);

        StringAssert.Contains(ex.Message, "Baz");
    }

    #endregion

    #region Exception Branch Coverage — UnknownColumnOrAliasException

    [TestMethod]
    public void UnknownColumnOrAlias_WhenCreatedWithMessage_ShouldSetDefaults()
    {
        var ex = new UnknownColumnOrAliasException("unknown col");

        Assert.AreEqual("unknown col", ex.Message);
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, ex.Code);
        Assert.IsNull(ex.Span);
        Assert.IsNull(ex.ColumnName);
    }

    [TestMethod]
    public void UnknownColumnOrAlias_WhenCreatedWithContext_ShouldAppendContext()
    {
        var span = new TextSpan(0, 5);
        var ex = new UnknownColumnOrAliasException("Col", "in table Users", span);

        Assert.AreEqual("Col", ex.ColumnName);
        Assert.AreEqual(span, ex.Span);
        StringAssert.Contains(ex.Message, "Col");
        StringAssert.Contains(ex.Message, "in table Users");
    }

    [TestMethod]
    public void UnknownColumnOrAlias_WhenCreatedWithEmptyContext_ShouldOmitContext()
    {
        var span = new TextSpan(0, 5);
        var ex = new UnknownColumnOrAliasException("Col", "", span);

        StringAssert.Contains(ex.Message, "Col");
        Assert.DoesNotContain("  ", ex.Message);
    }

    [TestMethod]
    public void UnknownColumnOrAlias_WhenCreatedWithNullContext_ShouldOmitContext()
    {
        var span = new TextSpan(0, 5);
        var ex = new UnknownColumnOrAliasException("Col", null, span);

        StringAssert.Contains(ex.Message, "Col");
    }

    [TestMethod]
    public void UnknownColumnOrAlias_ToDiagnostic_ShouldReturnError()
    {
        var ex = new UnknownColumnOrAliasException("Col", "ctx", new TextSpan(0, 5));
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — UnknownInterpretationSchemaException

    [TestMethod]
    public void UnknownInterpretationSchema_WhenCreatedWithName_ShouldSetProperties()
    {
        var ex = new UnknownInterpretationSchemaException("mySchema");

        Assert.AreEqual("mySchema", ex.SchemaName);
        Assert.AreEqual(DiagnosticCode.MQ3010_UnknownSchema, ex.Code);
        Assert.IsNull(ex.Span);
        StringAssert.Contains(ex.Message, "mySchema");
    }

    [TestMethod]
    public void UnknownInterpretationSchema_WhenCreatedWithNameAndMessage_ShouldSetCustomMessage()
    {
        var ex = new UnknownInterpretationSchemaException("mySchema", "custom error");

        Assert.AreEqual("mySchema", ex.SchemaName);
        Assert.AreEqual("custom error", ex.Message);
    }

    [TestMethod]
    public void UnknownInterpretationSchema_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(10, 20);
        var ex = new UnknownInterpretationSchemaException("s", "msg", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual("s", ex.SchemaName);
    }

    [TestMethod]
    public void UnknownInterpretationSchema_ToDiagnostic_ShouldReturnError()
    {
        var ex = new UnknownInterpretationSchemaException("schema");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3010_UnknownSchema, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [TestMethod]
    public void UnknownInterpretationSchema_CreateForSchemaNotInRegistry_ShouldContainSchemaName()
    {
        var ex = UnknownInterpretationSchemaException.CreateForSchemaNotInRegistry("missing");

        Assert.AreEqual("missing", ex.SchemaName);
        StringAssert.Contains(ex.Message, "missing");
        StringAssert.Contains(ex.Message, "not found");
    }

    [TestMethod]
    public void UnknownInterpretationSchema_CreateForTypeGenerationFailed_ShouldContainSchemaName()
    {
        var ex = UnknownInterpretationSchemaException.CreateForTypeGenerationFailed("broken");

        Assert.AreEqual("broken", ex.SchemaName);
        StringAssert.Contains(ex.Message, "broken");
        StringAssert.Contains(ex.Message, "unavailable");
    }

    #endregion

    #region Exception Branch Coverage — UnknownPropertyException

    [TestMethod]
    public void UnknownProperty_WhenCreatedWithMessage_ShouldSetDefaults()
    {
        var ex = new UnknownPropertyException("property not found");

        Assert.AreEqual("property not found", ex.Message);
        Assert.AreEqual(DiagnosticCode.MQ3014_InvalidPropertyAccess, ex.Code);
        Assert.IsNull(ex.Span);
        Assert.IsNull(ex.PropertyName);
        Assert.IsNull(ex.TypeName);
    }

    [TestMethod]
    public void UnknownProperty_WhenCreatedWithDetails_ShouldSetAllProperties()
    {
        var span = new TextSpan(0, 10);
        var ex = new UnknownPropertyException("Age", "Person", span);

        Assert.AreEqual("Age", ex.PropertyName);
        Assert.AreEqual("Person", ex.TypeName);
        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3014_InvalidPropertyAccess, ex.Code);
        StringAssert.Contains(ex.Message, "Age");
        StringAssert.Contains(ex.Message, "Person");
    }

    [TestMethod]
    public void UnknownProperty_ToDiagnostic_ShouldReturnError()
    {
        var ex = new UnknownPropertyException("Age", "Person", new TextSpan(0, 5));
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3014_InvalidPropertyAccess, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion
}
