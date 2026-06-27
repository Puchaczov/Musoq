using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests;

public partial class BranchCoverageImprovementTests
{
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
        var ex = new AmbiguousMethodOwnerException("DoWork()", ["x"], span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void AmbiguousMethodOwner_ToDiagnostic_ShouldReturnError()
    {
        var ex = new AmbiguousMethodOwnerException("DoWork()", ["x"]);
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
        var ex = new NonAggregatedColumnInSelectException("City", ["Name"], span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void NonAggregatedColumn_ToDiagnostic_ShouldReturnError()
    {
        var ex = new NonAggregatedColumnInSelectException("City", ["Name"]);
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3012_NonAggregateInSelect, diagnostic.Code);
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
        var ex = new UnknownColumnOrAliasException("Col", null!, span);

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
