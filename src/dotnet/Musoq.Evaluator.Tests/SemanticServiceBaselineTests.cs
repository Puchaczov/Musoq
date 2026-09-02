using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SemanticServiceBaselineTests
{
    [TestMethod]
    public void SourceBindingService_HasAlreadyUsedAlias_ShouldSearchCurrentAndParentScopes()
    {
        var sourceBinding = new SourceBindingState();
        var root = new Scope(null, 0);
        root.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias("outer");
        var child = root.AddScope();
        child.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias("inner");
        sourceBinding.CurrentScope = child;
        var service = new SemanticSourceBindingService(sourceBinding);

        Assert.IsTrue(service.HasAlreadyUsedAlias("outer"));
        Assert.IsTrue(service.HasAlreadyUsedAlias("inner"));
        Assert.IsFalse(service.HasAlreadyUsedAlias("missing"));
    }

    [TestMethod]
    public void MethodBindingService_RegisterContextAssemblies_ShouldRegisterEntityAndBaseAssemblies()
    {
        var assemblies = new List<Assembly>();
        var service = new SemanticMethodBindingService(assemblies.Add);

        service.RegisterContextAssemblies(typeof(SemanticServiceBaselineTests));

        CollectionAssert.Contains(assemblies, typeof(SemanticServiceBaselineTests).Assembly);
        CollectionAssert.Contains(assemblies, typeof(object).Assembly);
    }

    [TestMethod]
    public void ColumnPropertyBindingService_ResolveIdentifier_ShouldBindDirectColumn()
    {
        var sourceBinding = new SourceBindingState { Identifier = "people" };
        var service = new SemanticColumnPropertyBindingService(sourceBinding, new ResultShapeState());
        var symbol = CreateTableSymbol("people", new SchemaColumn("Name", 0, typeof(string)));

        var binding = service.ResolveIdentifier(symbol, "Name");

        Assert.AreEqual(SemanticIdentifierBindingKind.Column, binding.Kind);
        Assert.AreEqual("Name", binding.Column?.ColumnName);
        Assert.AreEqual(string.Empty, binding.SourceAlias);
    }

    [TestMethod]
    public void ColumnPropertyBindingService_ResolveIdentifier_ShouldBindSingleColumnAlias()
    {
        var sourceBinding = new SourceBindingState { Identifier = "peopleorders" };
        var service = new SemanticColumnPropertyBindingService(sourceBinding, new ResultShapeState());
        var symbol = CreateTableSymbol("people", new SchemaColumn("Name", 0, typeof(string)))
            .MergeSymbols(CreateTableSymbol("orders", new SchemaColumn("OrderId", 0, typeof(int))));

        var binding = service.ResolveIdentifier(symbol, "orders");

        Assert.AreEqual(SemanticIdentifierBindingKind.Column, binding.Kind);
        Assert.AreEqual("OrderId", binding.Column?.ColumnName);
        Assert.AreEqual("orders", binding.SourceAlias);
    }

    [TestMethod]
    public void ColumnPropertyBindingService_ResolveIdentifier_ShouldPreserveAmbiguousColumnException()
    {
        var sourceBinding = new SourceBindingState { Identifier = "peopleorders" };
        var service = new SemanticColumnPropertyBindingService(sourceBinding, new ResultShapeState());
        var symbol = CreateTableSymbol("people", new SchemaColumn("Name", 0, typeof(string)))
            .MergeSymbols(CreateTableSymbol("orders", new SchemaColumn("Name", 1, typeof(string))));

        Assert.Throws<AmbiguousColumnException>(() => service.ResolveIdentifier(symbol, "Name"));
    }

    [TestMethod]
    public void ColumnPropertyBindingService_ResolveIdentifier_ShouldReturnUnknownWithAvailableColumns()
    {
        var sourceBinding = new SourceBindingState { Identifier = "people" };
        var service = new SemanticColumnPropertyBindingService(sourceBinding, new ResultShapeState());
        var symbol = CreateTableSymbol("people", new SchemaColumn("Name", 0, typeof(string)));

        var binding = service.ResolveIdentifier(symbol, "Missing");

        Assert.AreEqual(SemanticIdentifierBindingKind.Unknown, binding.Kind);
        Assert.AreEqual("Name", binding.AvailableColumns.Single().ColumnName);
    }

    [TestMethod]
    public void ColumnPropertyBindingService_ResolveDynamicProperty_ShouldApplyExplicitAndDefaultTypeHints()
    {
        var service = new SemanticColumnPropertyBindingService(new SourceBindingState(), new ResultShapeState());

        var explicitHint = service.ResolveDynamicProperty(
            typeof(HintedDynamicEntity),
            "AsInt",
            typeof(object),
            typeof(ExpandoObject));
        var defaultHint = service.ResolveDynamicProperty(
            typeof(HintedDynamicEntity),
            "Other",
            typeof(object),
            typeof(ExpandoObject));

        Assert.AreEqual(typeof(int), explicitHint.PropertyType);
        Assert.AreEqual(typeof(double), defaultHint.PropertyType);
    }

    [TestMethod]
    public void ColumnPropertyBindingService_ResolveDynamicProperty_ShouldRespectMostInnerIdentifierFallback()
    {
        var resultShape = new ResultShapeState { TheMostInnerIdentifier = new IdentifierNode("Payload") };
        var service = new SemanticColumnPropertyBindingService(new SourceBindingState(), resultShape);

        var property = service.ResolveDynamicProperty(
            typeof(object),
            "Payload",
            typeof(object[]),
            typeof(ExpandoObject[]));

        Assert.AreEqual(typeof(object[]), property.PropertyType);
    }

    [TestMethod]
    public void ColumnPropertyBindingService_TypedPropertyAndIndexerChecks_ShouldUseReflectionFacts()
    {
        var service = new SemanticColumnPropertyBindingService(new SourceBindingState(), new ResultShapeState());

        var idProperty = service.TryResolveTypedProperty(
            typeof(IndexerEntity),
            nameof(IndexerEntity.Id),
            out var idError);
        var valuesProperty = service.TryResolveTypedProperty(
            typeof(IndexerEntity),
            nameof(IndexerEntity.Values),
            out var valuesError);
        var numbersProperty = service.TryResolveTypedProperty(
            typeof(IndexerEntity),
            nameof(IndexerEntity.Numbers),
            out var numbersError);

        Assert.IsNull(idError);
        Assert.IsNull(valuesError);
        Assert.IsNull(numbersError);
        Assert.AreEqual(typeof(int), idProperty?.PropertyType);
        Assert.IsFalse(service.CanUseAsIndexer(idProperty));
        Assert.IsTrue(service.CanUseAsIndexer(valuesProperty));
        Assert.IsTrue(service.CanUseAsArrayOrIndexer(numbersProperty));
    }

    [TestMethod]
    public void ResultShapeBindingService_ShouldCreateAndRegisterAliases()
    {
        var resultShape = new ResultShapeState();
        var service = new SemanticResultShapeBindingService(resultShape);

        Assert.AreEqual("explicit", service.CreateAlias("explicit", 7));
        var generated = service.CreateAlias(string.Empty, 7);
        service.RegisterAlias(generated);

        Assert.AreEqual(6, generated.Length);
        Assert.IsFalse(char.IsDigit(generated[0]));
        CollectionAssert.Contains(resultShape.GeneratedAliases, generated);
    }

    [TestMethod]
    public void ResultShapeBindingService_AddAllColumnsFields_ShouldUseGeneratedProjectionColumns()
    {
        var resultShape = new ResultShapeState();
        var sourceBinding = new SourceBindingState { Identifier = "items" };
        resultShape.GeneratedColumns["items"] =
        [
            new FieldNode(new IntegerNode(1), 0, "Id"),
            new FieldNode(new StringNode("ada"), 1, "Name")
        ];
        var service = new SemanticResultShapeBindingService(resultShape);
        var fields = new List<FieldNode>();
        var position = 3;

        service.AddAllColumnsFields(sourceBinding, fields, new AllColumnsNode(), ref position);

        Assert.AreEqual(2, fields.Count);
        Assert.AreEqual("Id", fields[0].FieldName);
        Assert.AreEqual(3, fields[0].FieldOrder);
        Assert.AreEqual("Name", fields[1].FieldName);
        Assert.AreEqual(4, fields[1].FieldOrder);
        Assert.AreEqual(5, position);
    }

    [TestMethod]
    public void QueryValidationService_ValidateExpressionIsBoolean_ShouldPreserveDiagnosticText()
    {
        var diagnostics = new DiagnosticContext();
        var reporter = new SemanticDiagnosticReporter(diagnostics);
        var service = new SemanticQueryValidationService(reporter);

        service.ValidateExpressionIsBoolean(new BooleanNode(true), "WHERE");
        service.ValidateExpressionIsBoolean(new StringNode("not-bool"), "WHERE");

        var error = diagnostics.Errors.Single();
        Assert.AreEqual("WHERE clause requires a boolean expression, but got 'String'.", error.Message);
    }

    [TestMethod]
    public void QueryValidationService_ValidateExpressionIsBoolean_WithoutDiagnostics_ShouldPreserveExceptionText()
    {
        var reporter = new SemanticDiagnosticReporter(null);
        var service = new SemanticQueryValidationService(reporter);

        var exception = Assert.Throws<TypeMismatchException>(
            () => service.ValidateExpressionIsBoolean(new StringNode("not-bool"), "WHERE"));

        Assert.AreEqual("Type mismatch: cannot convert 'String' to 'Boolean'.", exception.Message);
    }

    [TestMethod]
    public void QueryValidationService_ValidateExpressionIsPrimitive_ShouldPreserveDiagnosticText()
    {
        var diagnostics = new DiagnosticContext();
        var reporter = new SemanticDiagnosticReporter(diagnostics);
        var service = new SemanticQueryValidationService(reporter);

        service.ValidateExpressionIsPrimitive(new IdentifierNode("Self", typeof(IndexerEntity)), "SELECT");

        var error = diagnostics.Errors.Single();
        Assert.AreEqual(DiagnosticCode.MQ3027_InvalidExpressionType, error.Code);
        Assert.AreEqual(
            "Expression 'Self' has invalid type 'IndexerEntity' in SELECT. Only primitive types are allowed in query expressions.",
            error.Message);
    }

    [TestMethod]
    public void QueryValidationService_ValidateGroupBySemantics_ShouldPreserveNonAggregateDiagnosticText()
    {
        var diagnostics = new DiagnosticContext();
        var reporter = new SemanticDiagnosticReporter(diagnostics);
        var service = new SemanticQueryValidationService(reporter);
        var select = new SelectNode(
        [
            new FieldNode(new AccessColumnNode("Name", "p", typeof(string), TextSpan.Empty), 0, "Name")
        ]);
        var groupBy = new GroupByNode(
        [
            new FieldNode(new AccessColumnNode("City", "p", typeof(string), TextSpan.Empty), 0, "City")
        ], null);

        service.ValidateGroupBySemantics(select, groupBy);

        var error = diagnostics.Errors.Single();
        Assert.AreEqual(DiagnosticCode.MQ3012_NonAggregateInSelect, error.Code);
        Assert.AreEqual(
            "Column 'Name' must appear in the GROUP BY clause or be used in an aggregate function. Current GROUP BY columns: p.City.",
            error.Message);
    }

    [TestMethod]
    public void ExpressionBindingService_BindCoalesce_ShouldFoldNullAndResolveNullableFallback()
    {
        var reporter = new SemanticDiagnosticReporter(null);
        var service = new SemanticExpressionBindingService(reporter);
        var right = new IdentifierNode("Fallback", typeof(string));
        var folded = service.BindCoalesce(new CoalesceNode(new NullNode(typeof(object)), right), new NullNode(typeof(object)), right);

        Assert.AreSame(right, folded);

        var left = new IdentifierNode("Maybe", typeof(int?));
        var fallback = new IntegerNode(3);
        var bound = service.BindCoalesce(new CoalesceNode(left, fallback), left, fallback);

        Assert.IsInstanceOfType<CoalesceNode>(bound);
        Assert.AreEqual(typeof(int), bound.ReturnType);
    }

    [TestMethod]
    public void ExpressionBindingService_BindCoalesce_ShouldPreserveMismatchText()
    {
        var reporter = new SemanticDiagnosticReporter(null);
        var service = new SemanticExpressionBindingService(reporter);
        var left = new IdentifierNode("Maybe", typeof(int?));
        var right = new StringNode("fallback");

        var exception = Assert.Throws<TypeMismatchException>(
            () => service.BindCoalesce(new CoalesceNode(left, right), left, right));

        Assert.AreEqual(
            "Operator ?? requires compatible fallback types, but got 'Int32?' and 'String'.",
            exception.Message);
    }

    [TestMethod]
    public void ExpressionDiagnosticFacts_CreateBooleanContextMessage_ShouldPreserveScriptParameterText()
    {
        var message = SemanticExpressionDiagnosticFacts.CreateBooleanContextTypeMismatchMessage(
            new ParameterReferenceNode("threshold", typeof(int)),
            typeof(int),
            "WHERE");

        Assert.AreEqual(
            "WHERE clause requires a boolean expression, but script parameter '$threshold' has type 'Int32'.",
            message);
    }

    [TestMethod]
    public void SetOperatorFactService_ShouldPreservePositionAndTypeFacts()
    {
        var query = CreateSetOperatorQuery();
        var service = new SemanticSetOperatorFactService();

        CollectionAssert.AreEqual(
            new[] { 1, 0, 0 },
            service.CreatePositionIndexes(query, ["Name", "Missing", "Id"]));
        CollectionAssert.AreEqual(
            new[] { typeof(string), typeof(object), typeof(int) },
            service.CreatePositionTypes(query, ["Name", "Missing", "Id"]));
        Assert.IsTrue(service.TryGetFieldPosition(query, "Name", out var position));
        Assert.AreEqual(1, position);
    }

    [TestMethod]
    public void SetOperatorUtilityFacade_ShouldDelegateToFactService()
    {
        var query = CreateSetOperatorQuery();

        CollectionAssert.AreEqual(
            new[] { 1, 0, 0 },
            BuildMetadataAndInferTypesVisitorUtilities.CreateSetOperatorPositionIndexes(query, ["Name", "Missing", "Id"]));
    }

    [TestMethod]
    public void SemanticDiagnosticMessages_ShouldStayStable()
    {
        Assert.AreEqual(
            "Set operator must have the same quantity of columns in both queries. Left has 2 columns, right has 3 columns.",
            new SetOperatorMustHaveSameQuantityOfColumnsException(2, 3, TextSpan.Empty).Message);
        Assert.AreEqual(
            "Set operator must have the same types of columns in both queries. Left column expression is 1 and right column expression is 'name'",
            new SetOperatorMustHaveSameTypesOfColumnsException(
                new FieldNode(new IntegerNode(1), 0, "Id"),
                new FieldNode(new StringNode("name"), 0, "Name")).Message);
    }

    private static QueryNode CreateSetOperatorQuery()
    {
        var select = new SelectNode(
        [
            new FieldNode(new IntegerNode(1), 0, "Id"),
            new FieldNode(new StringNode("ada"), 1, "Name")
        ]);
        return new QueryNode(
            select,
            new InMemoryTableFromNode("source", "s", typeof(object)),
            null,
            null,
            null,
            null,
            null);
    }

    private static TableSymbol CreateTableSymbol(string alias, params ISchemaColumn[] columns)
    {
        var table = new DynamicTable(columns);
        return new TableSymbol(alias, new TransitionSchema(alias, table), table, true);
    }

    [DynamicObjectPropertyTypeHint("AsInt", typeof(int))]
    [DynamicObjectPropertyDefaultTypeHint(typeof(double))]
    private sealed class HintedDynamicEntity : DynamicObject;

    private sealed class IndexerEntity
    {
        public int Id { get; init; }

        public Dictionary<string, int> Values { get; } = new();

        public int[] Numbers { get; init; } = [];
    }

}
