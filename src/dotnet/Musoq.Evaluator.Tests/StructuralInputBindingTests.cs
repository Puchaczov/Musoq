using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class StructuralInputBindingTests
{
    [TestMethod]
    public void ParameterDeclaration_ShouldBindRecursiveShapeAndStructuredDefault()
    {
        var visitor = Analyze(
            "param(patterns: (Id: string, Pattern: string, Mode: string = 'literal')[] = array { (Id: 'todo', Pattern: 'TODO') }); " +
            "select 1 from #EnvironmentVariables.All()");

        var definition = visitor.ScriptParameterDefinitions.Single();
        Assert.IsTrue(definition.Contract.IsStructured);
        Assert.AreEqual(
            "(Id: string, Pattern: string, Mode: string = 'literal')[]",
            definition.Contract.CanonicalTypeName);
        Assert.AreEqual(ScriptParameterDefaultKind.Structured, definition.Contract.DefaultKind);
        Assert.IsInstanceOfType<StructuralTypeDescriptor>(definition.Contract.StructuralType);

        var defaultItems = (StructuralValue[])definition.DefaultValue!;
        Assert.HasCount(1, defaultItems);
        Assert.IsTrue(defaultItems[0].Fields.ContainsKey("Mode"));
        Assert.AreEqual("literal", defaultItems[0].Fields["Mode"].Scalar);
        Assert.AreEqual("todo", defaultItems[0].Fields["Id"].Scalar);
    }

    [TestMethod]
    public void EmptyPrimitiveArrayDefault_ShouldRetainExpectedElementType()
    {
        var visitor = Analyze(
            "param(emptyNumbers: int[] = array {}); select 1 from #EnvironmentVariables.All()");

        var definition = visitor.ScriptParameterDefinitions.Single();
        Assert.IsTrue(definition.Contract.IsStructured);
        Assert.AreEqual(typeof(int[]), definition.ParameterType);
        Assert.IsEmpty((int[])definition.DefaultValue!);
        Assert.AreEqual("int[]", definition.Contract.CanonicalTypeName);
    }

    [TestMethod]
    public void InferredRecordArray_ShouldUnionFieldsWithoutMaterializingMissingValues()
    {
        var visitor = Analyze(
            "let patterns = array { (Id: 'todo', Pattern: 'TODO'), (Id: 'issue', Pattern: 'ISSUE', Mode: 'regex') }; " +
            "select 1 from #EnvironmentVariables.All()");

        var definition = visitor.ScriptVariableDefinitions.Single();
        Assert.IsTrue(definition.StructuralType != null);
        Assert.AreEqual(typeof(StructuralValue[]), definition.VariableType);
        Assert.AreEqual(
            "(Id: string, Mode: string, Pattern: string)[]",
            definition.StructuralType!.ToCanonicalSql());

        var mode = definition.StructuralType.ElementType!.Fields.Single(field => field.Name == "Mode");
        Assert.IsFalse(mode.Required);

        var values = (StructuralValue[])definition.Value!;
        Assert.IsFalse(values[0].Fields.ContainsKey("Mode"));
        Assert.AreEqual("regex", values[1].Fields["Mode"].Scalar);
    }

    [TestMethod]
    public void InferredNestedRecordAndPrimitiveArray_ShouldPreserveTypedShape()
    {
        var visitor = Analyze(
            "let options = (Enabled: true, Codes: array { 10, 20 }, Window: (Before: 2, After: 3)); " +
            "select 1 from #EnvironmentVariables.All()");

        var definition = visitor.ScriptVariableDefinitions.Single();
        var type = definition.StructuralType!;
        Assert.AreEqual(StructuralTypeKind.Record, type.Kind);
        Assert.AreEqual("bool", type.Fields.Single(field => string.Equals(field.Name, "Enabled", StringComparison.OrdinalIgnoreCase)).Type.ToCanonicalSql());
        Assert.AreEqual("int[]", type.Fields.Single(field => string.Equals(field.Name, "Codes", StringComparison.OrdinalIgnoreCase)).Type.ToCanonicalSql());
        Assert.AreEqual("(Before: int, After: int)", type.Fields.Single(field => string.Equals(field.Name, "Window", StringComparison.OrdinalIgnoreCase)).Type.ToCanonicalSql());

        var value = (StructuralValue)definition.Value!;
        Assert.AreEqual(2, value.Fields["Codes"].Elements.Count);
    }

    [TestMethod]
    public void ConstructionPlan_ShouldMapReorderedFieldsAndReceiverDefaults()
    {
        var input = StructuralTypeDescriptor.Record(
            typeof(StructuralValue),
            [
                new StructuralFieldDescriptor("Pattern", StructuralTypeDescriptor.Scalar(typeof(string), true), true),
                new StructuralFieldDescriptor("Id", StructuralTypeDescriptor.Scalar(typeof(string), true), true)
            ]);
        var receiver = StructuralTypeDescriptor.FromClrType(typeof(ReceiverInput));

        var success = StructuralConstructionPlanBinder.TryBind(input, receiver, out var plan, out var error);

        Assert.IsTrue(success, error);
        CollectionAssert.AreEqual(new[] { 1, 0, -1 }, plan.SourceFieldIndexes.ToArray());
        Assert.AreEqual(typeof(ReceiverInput), plan.TargetType);
    }

    [TestMethod]
    public void ConstructionPlan_ShouldRejectUnexpectedAndMissingRequiredFields()
    {
        var input = StructuralTypeDescriptor.Record(
            typeof(StructuralValue),
            [new StructuralFieldDescriptor("Unknown", StructuralTypeDescriptor.Scalar(typeof(string), true), true)]);
        var receiver = StructuralTypeDescriptor.FromClrType(typeof(ReceiverInput));

        var success = StructuralConstructionPlanBinder.TryBind(input, receiver, out _, out var error);

        Assert.IsFalse(success);
        StringAssert.Contains(error, "unexpected field");
    }

    [TestMethod]
    public void StructuralValueBinder_ShouldMaterializeDeclaredFieldDefaultsAsPresent()
    {
        var descriptor = StructuralTypeDescriptor.Record(
            typeof(StructuralValue),
            [new StructuralFieldDescriptor(
                "Mode",
                StructuralTypeDescriptor.Scalar(typeof(string), true),
                required: false,
                StructuralDefaultDescriptor.Create("regex"))]);

        var raw = StructuralValue.FromRecord([]);

        Assert.IsTrue(StructuralValueBinder.TryNormalize(
            raw,
            descriptor,
            "input",
            out var normalized,
            out var error), error);
        var record = (StructuralValue)normalized!;
        Assert.IsTrue(record.Fields.ContainsKey("Mode"));
        Assert.AreEqual("regex", record.Fields["Mode"].Scalar);
    }

    [TestMethod]
    public void StructuralArgumentDescriptions_ShouldUseExplicitNullTextForNestedDefaults()
    {
        var constructor = typeof(NullDefaultSource).GetConstructors(BindingFlags.Instance | BindingFlags.Public).Single();
        var contract = StructuralInputMetadata.CreateContract("source", constructor);
        var metadata = new Musoq.Schema.Reflection.ConstructorInfo(
            constructor,
            supportsInterCommunicator: true,
            ("input", typeof(NullDefaultInput)))
        {
            StructuralContract = contract,
            SourceStableId = "source:null-default"
        };

        var descriptions = StructuralArgumentDescriptionFactory.Create(
            new SchemaMethodInfo("source", metadata),
            overload: 2);
        var field = descriptions.Single(description => description.Path == "input.Mode");

        Assert.IsTrue(field.HasDefault);
        Assert.AreEqual("null", field.Default);
        Assert.IsFalse(field.Required);
    }

    [TestMethod]
    public void StructuralArgumentDescriptions_InventoryUsesEveryRegisteredOverloadInOrder()
    {
        var schema = new DescriptionOverloadedSchema();
        var context = new SourceExecutionContext(
            "desc",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            [],
            new Dictionary<string, string>(),
            NullLogger.Instance);

        var table = EvaluationHelper.GetStructuralArgumentDescriptions(
            schema,
            "multi",
            selected: null,
            context);

        Assert.AreEqual(2, table.Rows.Count);
        Assert.AreEqual(0, table.Rows[0][0]);
        Assert.AreEqual("text", table.Rows[0][1]);
        Assert.AreEqual("string", table.Rows[0][3]);
        Assert.AreEqual(1, table.Rows[1][0]);
        Assert.AreEqual("value", table.Rows[1][1]);
        Assert.AreEqual("int", table.Rows[1][3]);
    }

    private static BuildMetadataAndInferTypesVisitor Analyze(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        var tree = parser.ComposeAll();
        var visitor = new BuildMetadataAndInferTypesVisitor(
            new EnvironmentVariablesSchemaProvider(),
            new Dictionary<string, string[]>(),
            new Mock<ILogger<BuildMetadataAndInferTypesVisitor>>().Object);
        tree.Accept(new BuildMetadataAndInferTypesTraverseVisitor(visitor));
        return visitor;
    }

    public readonly record struct ReceiverInput(string Id, string Pattern, string Mode = "literal");

    private sealed record NullDefaultInput(string? Mode = null);

    private sealed class NullDefaultSource : RowSource<ReceiverInput>
    {
        public NullDefaultSource(NullDefaultInput input, SourceExecutionContext context)
        {
            _ = input;
            _ = context;
        }

        public override IEnumerable<IReadOnlyList<ReceiverInput>> Chunks => [];
    }

    private sealed class DescriptionOverloadedSource : RowSource<ReceiverInput>
    {
        public DescriptionOverloadedSource(int value, SourceExecutionContext context)
        {
            _ = value;
            _ = context;
        }

        public DescriptionOverloadedSource(string text, SourceExecutionContext context)
        {
            _ = text;
            _ = context;
        }

        public override IEnumerable<IReadOnlyList<ReceiverInput>> Chunks => [];
    }

    private sealed class DescriptionOverloadedSchema : SchemaBase
    {
        public DescriptionOverloadedSchema()
            : base("description", CreateLibrary())
        {
            AddTypedSource<DescriptionOverloadedSource>("multi");
        }

        private static MethodsAggregator CreateLibrary()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new Musoq.Plugins.LibraryBase());
            return new MethodsAggregator(manager);
        }
    }
}
