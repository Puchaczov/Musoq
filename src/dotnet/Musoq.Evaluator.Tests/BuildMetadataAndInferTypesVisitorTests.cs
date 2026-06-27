using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Lexing;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BuildMetadataAndInferTypesVisitorTests
{
    [TestMethod]
    public void Constructor_ShouldAcceptSchemaRegistry_AndExposeItViaProperty()
    {
        var logger = new Mock<ILogger<BuildMetadataAndInferTypesVisitor>>();
        var registry = new SchemaRegistry();

        var visitor = new BuildMetadataAndInferTypesVisitor(
            new EnvironmentVariablesSchemaProvider(),
            new Dictionary<string, string[]>(),
            logger.Object,
            schemaRegistry: registry);

        Assert.AreSame(registry, visitor.SchemaRegistry);
    }

    [TestMethod]
    public void WhenPassedToSchemaMethodArgumentMustHaveKnownType_ShouldHave()
    {
        var query = "select 1 from #EnironmentVariables.All() d cross apply #EnvironmentVariables.All(d.Key) e";

        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        var tree = parser.ComposeAll();
        var logger = new Mock<ILogger<EnvironmentVariablesBuildMetadataAndInferTypesVisitor>>();

        var visitor = new EnvironmentVariablesBuildMetadataAndInferTypesVisitor(
            new EnvironmentVariablesSchemaProvider(),
            new Dictionary<string, string[]>
            {
                { "d1", ["Key", "Value"] },
                { "e1", ["Key", "Value"] }
            },
            new Dictionary<string, IEnumerable<EnvironmentVariableEntity>>
            {
                { "d:1", [] },
                { "e:1", [new EnvironmentVariableEntity("KEY_1", "VALUE_1")] }
            }, logger.Object);

        var traverser = new BuildMetadataAndInferTypesTraverseVisitor(visitor);

        tree.Accept(traverser);

        var sourceRuntimeSettingsBySourceContextId = visitor.SourceRuntimeSettingsBySourceContextId;

        Assert.HasCount(2, sourceRuntimeSettingsBySourceContextId);

        Assert.IsEmpty(sourceRuntimeSettingsBySourceContextId["d:1"]);
        Assert.HasCount(1, sourceRuntimeSettingsBySourceContextId["e:1"]);

        Assert.AreEqual("VALUE_1", sourceRuntimeSettingsBySourceContextId["e:1"]["KEY_1"]);

        Assert.HasCount(1, visitor.PassedSchemaArguments);

        Assert.AreEqual(typeof(string), visitor.PassedSchemaArguments[0]);
    }

    [TestMethod]
    public void ScriptParameters_WhenDeclared_ShouldExposeResolvedDefinitions()
    {
        var visitor = Analyze(
            "param (author: string, limit: int = 100, since: datetime? = null); " +
            "select $author, $limit, $since from #EnvironmentVariables.All()");

        Assert.HasCount(3, visitor.ScriptParameterDefinitions);

        Assert.AreEqual("author", visitor.ScriptParameterDefinitions[0].Name);
        Assert.AreEqual(typeof(string), visitor.ScriptParameterDefinitions[0].ParameterType);
        Assert.IsFalse(visitor.ScriptParameterDefinitions[0].HasDefaultValue);

        Assert.AreEqual("limit", visitor.ScriptParameterDefinitions[1].Name);
        Assert.AreEqual(typeof(int), visitor.ScriptParameterDefinitions[1].ParameterType);
        Assert.IsTrue(visitor.ScriptParameterDefinitions[1].HasDefaultValue);
        Assert.AreEqual(100, visitor.ScriptParameterDefinitions[1].DefaultValue);

        Assert.AreEqual("since", visitor.ScriptParameterDefinitions[2].Name);
        Assert.AreEqual(typeof(DateTime?), visitor.ScriptParameterDefinitions[2].ParameterType);
        Assert.IsTrue(visitor.ScriptParameterDefinitions[2].HasDefaultValue);
        Assert.IsNull(visitor.ScriptParameterDefinitions[2].DefaultValue);
    }

    [TestMethod]
    public void ScriptParameters_WhenDuplicated_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            Analyze("param (author: string, author: int); select 1 from #EnvironmentVariables.All()"));
    }

    [TestMethod]
    public void ScriptParameters_WhenBlockIsDeclaredTwice_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            Analyze("param (author: string); param (limit: int); select 1 from #EnvironmentVariables.All()"));
    }

    [TestMethod]
    public void ScriptParameters_WhenBlockAppearsAfterQuery_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            Analyze("select 1 from #EnvironmentVariables.All(); param (author: string)"));
    }

    [TestMethod]
    public void ScriptParameters_WhenReferenceIsMissing_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            Analyze("param (author: string); select $missing from #EnvironmentVariables.All()"));
    }

    [TestMethod]
    public void ScriptParameters_WhenTypeIsUnknown_ShouldThrow()
    {
        Assert.Throws<TypeNotFoundException>(() =>
            Analyze("param (author: UnknownType); select 1 from #EnvironmentVariables.All()"));
    }

    [TestMethod]
    public void ScriptParameters_WhenDefaultCannotBeConverted_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            Analyze("param (limit: int = 'abc'); select 1 from #EnvironmentVariables.All()"));
    }

    [TestMethod]
    public void ScriptParameters_WhenRequiredParameterUsedInSchemaArgument_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            Analyze("param (name: string); select 1 from #EnvironmentVariables.All($name)"));
    }

    [TestMethod]
    public void ScriptParameters_WhenDefaultedParameterUsedInSchemaArgument_ShouldBindDefault()
    {
        object?[]? capturedArguments = null;
        var visitor = Analyze(
            "param (name: string = 'KEY_1'); select 1 from #capture.any($name)",
            new CaptureArgumentsSchemaProvider(arguments => capturedArguments = arguments));

        Assert.HasCount(1, visitor.ScriptParameterDefinitions);
        Assert.AreEqual("KEY_1", visitor.ScriptParameterDefinitions[0].DefaultValue);

        Assert.IsNotNull(capturedArguments);
        Assert.HasCount(1, capturedArguments);
        Assert.AreEqual("KEY_1", capturedArguments[0]);
    }

    [TestMethod]
    public void ScriptParameters_WhenNestedInSchemaArgument_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            Analyze("param (name: string = 'KEY_1'); select 1 from #EnvironmentVariables.All($name + '_2')"));
    }

    [TestMethod]
    public void ScriptVariables_WhenDeclared_ShouldExposeResolvedDefinitions()
    {
        var visitor = Analyze(
            "let root: string = 'KEY'; " +
            "let name: string = $root + '_1'; " +
            "let limit: int = 10 + 5; " +
            "select $name, $limit from #EnvironmentVariables.All()");

        Assert.HasCount(3, visitor.ScriptVariableDefinitions);

        Assert.AreEqual("root", visitor.ScriptVariableDefinitions[0].Name);
        Assert.AreEqual(typeof(string), visitor.ScriptVariableDefinitions[0].VariableType);
        Assert.AreEqual("KEY", visitor.ScriptVariableDefinitions[0].Value);
        Assert.IsTrue(visitor.ScriptVariableDefinitions[0].CanUseConstKeyword);

        Assert.AreEqual("name", visitor.ScriptVariableDefinitions[1].Name);
        Assert.AreEqual(typeof(string), visitor.ScriptVariableDefinitions[1].VariableType);
        Assert.AreEqual("KEY_1", visitor.ScriptVariableDefinitions[1].Value);
        Assert.IsTrue(visitor.ScriptVariableDefinitions[1].CanUseConstKeyword);

        Assert.AreEqual("limit", visitor.ScriptVariableDefinitions[2].Name);
        Assert.AreEqual(typeof(int), visitor.ScriptVariableDefinitions[2].VariableType);
        Assert.AreEqual(15, visitor.ScriptVariableDefinitions[2].Value);
        Assert.IsTrue(visitor.ScriptVariableDefinitions[2].CanUseConstKeyword);
    }

    [TestMethod]
    public void ScriptVariables_WhenNameDuplicatesParameter_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            Analyze("param (name: string); let name: string = 'KEY_1'; select 1 from #EnvironmentVariables.All()"));
    }

    [TestMethod]
    public void ScriptVariables_WhenInitializerUsesLaterVariable_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            Analyze("let name: string = $later; let later: string = 'KEY_1'; select 1 from #EnvironmentVariables.All()"));
    }

    [TestMethod]
    public void ScriptVariables_WhenUsedInSchemaArgumentExpression_ShouldBindValue()
    {
        object?[]? capturedArguments = null;
        Analyze(
            "let root: string = 'KEY'; let name: string = $root + '_1'; select 1 from #capture.any($name + '_2')",
            new CaptureArgumentsSchemaProvider(arguments => capturedArguments = arguments));

        Assert.IsNotNull(capturedArguments);
        Assert.HasCount(1, capturedArguments);
        Assert.AreEqual("KEY_1_2", capturedArguments[0]);
    }

    [TestMethod]
    public void TableColumnReadModifiers_WhenCoupled_ShouldReachMetadataContext()
    {
        var capturedContexts = new List<SourceMetadataContext>();
        Analyze(
            "table LegacyRecord { Name: string encoding 'windows-1250' trim, Payload: string source codec 'base64' };" +
            "couple #capture.any with table LegacyRecord as Records;" +
            "select Name from Records()",
            new CaptureMetadataContextSchemaProvider(capturedContexts.Add));

        var contractContext = capturedContexts.SingleOrDefault(context =>
            context.AllColumns.Any(column => column.ReadModifiers.ContainsKey("encoding")));
        Assert.IsNotNull(contractContext);

        var nameColumn = contractContext.AllColumns.Single(column => column.ColumnName == "Name");
        Assert.AreEqual("windows-1250", nameColumn.ReadModifiers["encoding"]);
        Assert.AreEqual("true", nameColumn.ReadModifiers["trim"]);

        var payloadColumn = contractContext.AllColumns.Single(column => column.ColumnName == "Payload");
        Assert.AreEqual("base64", payloadColumn.ReadModifiers["source.codec"]);
    }

    [TestMethod]
    public void TableColumnType_WhenFullyQualifiedAndLoadable_ShouldResolveForCoupledSource()
    {
        var capturedContexts = new List<SourceMetadataContext>();
        Analyze(
            "table RecordsShape { Payload: System.String };" +
            "couple #capture.any with table RecordsShape as Records;" +
            "select Payload from Records()",
            new CaptureMetadataContextSchemaProvider(capturedContexts.Add));

        var contractContext = capturedContexts.SingleOrDefault(context =>
            context.AllColumns.Any(column => column.ColumnName == "Payload"));
        Assert.IsNotNull(contractContext);

        var payloadColumn = contractContext.AllColumns.Single(column => column.ColumnName == "Payload");
        Assert.AreEqual(typeof(string), payloadColumn.ColumnType);
    }

    [TestMethod]
    public void TableColumnType_WhenFullyQualifiedNullableAndCoupled_ShouldResolveForCoupledSource()
    {
        var capturedContexts = new List<SourceMetadataContext>();
        Analyze(
            "table RecordsShape { CapturedAt: System.DateTime? };" +
            "couple #capture.any with table RecordsShape as Records;" +
            "select CapturedAt from Records()",
            new CaptureMetadataContextSchemaProvider(capturedContexts.Add));

        var contractContext = capturedContexts.SingleOrDefault(context =>
            context.AllColumns.Any(column => column.ColumnName == "CapturedAt"));
        Assert.IsNotNull(contractContext);

        var capturedAtColumn = contractContext.AllColumns.Single(column => column.ColumnName == "CapturedAt");
        Assert.AreEqual(typeof(DateTime?), capturedAtColumn.ColumnType);
    }

    [TestMethod]
    public void TableColumnType_WhenFullyQualifiedAndNotLoadable_ShouldThrowTypeNotFoundException()
    {
        var exception = Assert.Throws<TypeNotFoundException>(() =>
            Analyze(
                "table RecordsShape { Payload: System.SomeCustomType };" +
                "couple #capture.any with table RecordsShape as Records;" +
                "select Payload from Records()",
                new CaptureMetadataContextSchemaProvider(_ => { })));

        Assert.AreEqual("System.SomeCustomType", exception.TypeName);
    }

    private static BuildMetadataAndInferTypesVisitor Analyze(string query)
    {
        return Analyze(query, new EnvironmentVariablesSchemaProvider());
    }

    private static BuildMetadataAndInferTypesVisitor Analyze(string query, ISchemaProvider schemaProvider)
    {
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        var tree = parser.ComposeAll();
        var logger = new Mock<ILogger<BuildMetadataAndInferTypesVisitor>>();

        var visitor = new BuildMetadataAndInferTypesVisitor(
            schemaProvider,
            new Dictionary<string, string[]>(),
            logger.Object);

        var traverser = new BuildMetadataAndInferTypesTraverseVisitor(visitor);
        tree.Accept(traverser);

        return visitor;
    }

    private sealed class CaptureArgumentsSchemaProvider(Action<object?[]> onArguments) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new CaptureArgumentsSchema(onArguments);
        }
    }

    private sealed class CaptureArgumentsSchema(Action<object?[]> onArguments)
        : SchemaBase("capture", new MethodsAggregator(new MethodsManager()))
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
        {
            onArguments(parameters);
            return new CaptureArgumentsTable();
        }

        public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CaptureArgumentsTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => [];

        public ISchemaColumn? GetColumnByName(string name)
        {
            throw new InvalidOperationException();
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return [];
        }

        public SchemaTableMetadata Metadata { get; } = new(typeof(object));
    }

    private sealed class CaptureMetadataContextSchemaProvider(Action<SourceMetadataContext> onMetadataContext) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new CaptureMetadataContextSchema(onMetadataContext);
        }
    }

    private sealed class CaptureMetadataContextSchema(Action<SourceMetadataContext> onMetadataContext)
        : SchemaBase("capture", new MethodsAggregator(new MethodsManager()))
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
        {
            onMetadataContext(metadataContext);
            return new CaptureMetadataContextTable(metadataContext.AllColumns.ToArray());
        }

        public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CaptureMetadataContextTable(ISchemaColumn[] columns) : ISchemaTable
    {
        public ISchemaColumn[] Columns => columns;

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }

        public SchemaTableMetadata Metadata { get; } = new(typeof(object));
    }

}
