using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Evaluator.Tests;

public partial class BranchCoverageImprovementTests
{
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
        var ex = new AmbiguousAggregateOwnerException("Count(*)", ["a"]);
        var diagnostic = ex.ToDiagnostic();

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
}
