using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Integration tests for the Interpret() function transformation in the build pipeline.
///     Tests the complete flow from RewriteQueryVisitor detection through to code generation.
/// </summary>
[TestClass]
public class InterpretFunctionIntegrationTests
{
    #region SchemaRegistry Flow Tests

    [TestMethod]
    public void BuildPipeline_WithSchemaDefinition_ShouldPopulateSchemaRegistry()
    {
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            new FieldDefinitionNode("Value", new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian))
        };
        var schema = new BinarySchemaNode("TestFormat", fields);
        registry.Register("TestFormat", schema);


        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        var compilationUnit = new InterpreterCompilationUnit(
            $"TestAssembly_{Guid.NewGuid():N}",
            code);

        var success = compilationUnit.Compile();


        var interpreterType = compilationUnit.GetInterpreterType("TestFormat");
        registry.GetSchema("TestFormat").GeneratedType = interpreterType;

        // Assert
        Assert.IsTrue(success, "Compilation should succeed");
        Assert.IsNotNull(registry.GetSchema("TestFormat").GeneratedType);
        Assert.AreEqual("Musoq.Generated.Interpreters.TestFormat",
            registry.GetSchema("TestFormat").GeneratedType!.FullName);
    }

    [TestMethod]
    public void BuildPipeline_MultipleSchemas_ShouldCompileAll()
    {
        // Arrange
        var registry = new SchemaRegistry();


        registry.Register("Schema1", new BinarySchemaNode("Schema1",
        [
            new FieldDefinitionNode("Field1", new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian))
        ]));


        registry.Register("Schema2", new BinarySchemaNode("Schema2",
            [new FieldDefinitionNode("Field2", new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.BigEndian))]));

        // Act
        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        var compilationUnit = new InterpreterCompilationUnit($"TestAssembly_{Guid.NewGuid():N}", code);
        var success = compilationUnit.Compile();

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(compilationUnit.GetInterpreterType("Schema1"));
        Assert.IsNotNull(compilationUnit.GetInterpreterType("Schema2"));
    }

    #endregion

    #region InterpretCallNodeProcessor Code Generation Tests

    [TestMethod]
    public void InterpretCallNodeProcessor_WithSchemaRegistry_ShouldUseCompiledType()
    {
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            new FieldDefinitionNode("Magic", new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian))
        };
        var schema = new BinarySchemaNode("FileHeader", fields);
        registry.Register("FileHeader", schema);

        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        var compilationUnit = new InterpreterCompilationUnit($"TestAssembly_{Guid.NewGuid():N}", code);
        var success = compilationUnit.Compile();
        Assert.IsTrue(success);


        var interpreterType = compilationUnit.GetInterpreterType("FileHeader");
        registry.GetSchema("FileHeader").GeneratedType = interpreterType;


        var dataSourceNode = new IntegerNode("1");
        var interpretCallNode = new InterpretCallNode(dataSourceNode, "FileHeader", null);


        var syntaxGenerator = SyntaxGenerator.GetGenerator(
            new AdhocWorkspace(),
            LanguageNames.CSharp);
        var nodeStack = new Stack<SyntaxNode>();
        nodeStack.Push(SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(1)));
        var statements = new List<StatementSyntax>();
        var interpreterInstances = new Dictionary<string, string>();
        var addedNamespaces = new List<string>();

        // Act
        var result = InterpretCallNodeProcessor.ProcessInterpretCallNode(
            interpretCallNode,
            syntaxGenerator,
            nodeStack,
            statements,
            interpreterInstances,
            ns => addedNamespaces.Add(ns),
            registry);

        // Assert
        Assert.IsNotNull(result);
        Assert.HasCount(1, statements);
        Assert.IsTrue(interpreterInstances.ContainsKey("FileHeader"));
        Assert.AreEqual("_interpreter_FileHeader", interpreterInstances["FileHeader"]);


        var statementCode = statements[0].ToFullString();
        Assert.Contains("FileHeader", statementCode);
    }

    [TestMethod]
    public void InterpretCallNodeProcessor_WithoutSchemaRegistry_ShouldUseFallbackNaming()
    {
        var dataSourceNode = new IntegerNode("1");
        var interpretCallNode = new InterpretCallNode(dataSourceNode, "UncompiledSchema", null);

        var syntaxGenerator = SyntaxGenerator.GetGenerator(
            new AdhocWorkspace(),
            LanguageNames.CSharp);
        var nodeStack = new Stack<SyntaxNode>();
        nodeStack.Push(SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(1)));
        var statements = new List<StatementSyntax>();
        var interpreterInstances = new Dictionary<string, string>();
        var addedNamespaces = new List<string>();


        var result = InterpretCallNodeProcessor.ProcessInterpretCallNode(
            interpretCallNode,
            syntaxGenerator,
            nodeStack,
            statements,
            interpreterInstances,
            ns => addedNamespaces.Add(ns));

        // Assert
        Assert.IsNotNull(result);
        Assert.HasCount(1, statements);


        var statementCode = statements[0].ToFullString();
        // The class name is now just the schema name without "Interpreter" suffix
        Assert.Contains("UncompiledSchema", statementCode);
        Assert.Contains("Musoq.Generated.Interpreters", addedNamespaces);
    }

    #endregion

    #region BuildMetadataAndInferTypesVisitor Tests

    [TestMethod]
    public void BuildMetadataVisitor_InterpretCallNode_ShouldPreserveSchemaName()
    {
        // Arrange
        var dataSourceNode = new IntegerNode("42");
        var interpretCallNode = new InterpretCallNode(dataSourceNode, "BinaryFormat", typeof(object));


        var visitor = CreateBuildMetadataVisitor();


        PushNodeToVisitor(visitor, dataSourceNode);

        // Act
        visitor.Visit(interpretCallNode);

        // Assert
        var result = PopNodeFromVisitor(visitor);
        Assert.IsInstanceOfType(result, typeof(InterpretCallNode));
        var resultNode = (InterpretCallNode)result;
        Assert.AreEqual("BinaryFormat", resultNode.SchemaName);
    }

    [TestMethod]
    public void BuildMetadataVisitor_ParseCallNode_ShouldPreserveSchemaName()
    {
        // Arrange
        var dataSourceNode = new StringNode("some text");
        var parseCallNode = new ParseCallNode(dataSourceNode, "TextFormat", typeof(object));

        var visitor = CreateBuildMetadataVisitor();
        PushNodeToVisitor(visitor, dataSourceNode);

        // Act
        visitor.Visit(parseCallNode);

        // Assert
        var result = PopNodeFromVisitor(visitor);
        Assert.IsInstanceOfType(result, typeof(ParseCallNode));
        var resultNode = (ParseCallNode)result;
        Assert.AreEqual("TextFormat", resultNode.SchemaName);
    }

    [TestMethod]
    public void BuildMetadataVisitor_InterpretAtCallNode_ShouldPreserveSchemaAndOffset()
    {
        // Arrange
        var dataSourceNode = new IntegerNode("1");
        var offsetNode = new IntegerNode("256");
        var interpretAtNode = new InterpretAtCallNode(dataSourceNode, offsetNode, "HeaderFormat", typeof(object));

        var visitor = CreateBuildMetadataVisitor();

        PushNodeToVisitor(visitor, dataSourceNode);
        PushNodeToVisitor(visitor, offsetNode);

        // Act
        visitor.Visit(interpretAtNode);

        // Assert
        var result = PopNodeFromVisitor(visitor);
        Assert.IsInstanceOfType(result, typeof(InterpretAtCallNode));
        var resultNode = (InterpretAtCallNode)result;
        Assert.AreEqual("HeaderFormat", resultNode.SchemaName);
    }

    #endregion

    #region Helper Methods

    private static BuildMetadataAndInferTypesVisitor CreateBuildMetadataVisitor()
    {
        var schemaProvider = new TestSchemaProvider();
        var columns = new Dictionary<string, string[]>();

        return new BuildMetadataAndInferTypesVisitor(
            schemaProvider,
            columns,
            new TestsLoggerResolver()
                .ResolveLogger<BuildMetadataAndInferTypesVisitor>());
    }

    private static void PushNodeToVisitor(BuildMetadataAndInferTypesVisitor visitor, Node node)
    {
        var nodesProperty = visitor.GetType().GetProperty("Nodes",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var nodes = (Stack<Node>)nodesProperty!.GetValue(visitor)!;
        nodes.Push(node);
    }

    private static Node PopNodeFromVisitor(BuildMetadataAndInferTypesVisitor visitor)
    {
        var nodesProperty = visitor.GetType().GetProperty("Nodes",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var nodes = (Stack<Node>)nodesProperty!.GetValue(visitor)!;
        return nodes.Pop();
    }

    /// <summary>
    ///     Simple test schema provider that returns empty schemas.
    /// </summary>
    private class TestSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            throw new NotImplementedException($"Schema '{schema}' not found in test provider");
        }
    }

    #endregion

    #region ToCSharpRewriteTreeVisitor Integration Tests

    [TestMethod]
    public void ToCSharpRewriteTreeVisitor_InterpretCallNode_WithSchemaRegistry_ShouldGenerateCode()
    {
        var registry = new SchemaRegistry();
        var fields = new[]
        {
            new FieldDefinitionNode("Value", new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian))
        };
        var schema = new BinarySchemaNode("TestSchema", fields);
        registry.Register("TestSchema", schema);

        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        var compilationUnit = new InterpreterCompilationUnit($"TestAssembly_{Guid.NewGuid():N}", code);
        var success = compilationUnit.Compile();
        Assert.IsTrue(success, "Schema compilation should succeed");


        var interpreterType = compilationUnit.GetInterpreterType("TestSchema");
        registry.GetSchema("TestSchema").GeneratedType = interpreterType;


        var assemblies = new[] { typeof(object).Assembly, typeof(BytesInterpreterBase<>).Assembly };
        var cteInnerExpressionOffsets = new Dictionary<string, int[]>();
        var schemaFromNodeColumns = new Dictionary<SchemaFromNode, ISchemaColumn[]>();
        var visitor = new ToCSharpRewriteTreeVisitor(
            assemblies,
            cteInnerExpressionOffsets,
            schemaFromNodeColumns,
            $"TestQuery_{Guid.NewGuid():N}",
            new CompilationOptions(),
            registry);


        var dataSourceNode = new AccessRawIdentifierNode("testData", typeof(byte[]));
        var interpretCallNode = new InterpretCallNode(dataSourceNode, "TestSchema", typeof(object));


        var nodesProperty = visitor.GetType().GetProperty("Nodes",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var nodes = (Stack<SyntaxNode>)nodesProperty!.GetValue(visitor)!;
        nodes.Push(SyntaxFactory.IdentifierName("testData"));

        // Act
        visitor.Visit(interpretCallNode);


        Assert.HasCount(1, nodes, "One node should be on the stack");
        var resultNode = nodes.Peek();
        Assert.IsNotNull(resultNode, "Result node should not be null");


        var statementsProperty = visitor.GetType().GetProperty("Statements",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var statements = (List<StatementSyntax>)statementsProperty!.GetValue(visitor)!;


        Assert.IsGreaterThanOrEqualTo(1,
            statements.Count, "At least one statement should be generated for interpreter instantiation");


        var allCode = string.Join("\n", statements.Select(s => s.ToFullString()));
        Assert.Contains("TestSchema", allCode, "Generated code should reference TestSchema interpreter");
    }

    [TestMethod]
    public void ToCSharpRewriteTreeVisitor_InterpretCallNode_WithoutSchemaRegistry_ShouldStillGenerateCode()
    {
        var assemblies = new[] { typeof(object).Assembly, typeof(BytesInterpreterBase<>).Assembly };
        var cteInnerExpressionOffsets = new Dictionary<string, int[]>();
        var schemaFromNodeColumns = new Dictionary<SchemaFromNode, ISchemaColumn[]>();
        var visitor = new ToCSharpRewriteTreeVisitor(
            assemblies,
            cteInnerExpressionOffsets,
            schemaFromNodeColumns,
            $"TestQuery_{Guid.NewGuid():N}",
            new CompilationOptions());


        var dataSourceNode = new AccessRawIdentifierNode("testData", typeof(byte[]));
        var interpretCallNode = new InterpretCallNode(dataSourceNode, "FallbackSchema", typeof(object));


        var nodesProperty = visitor.GetType().GetProperty("Nodes",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var nodes = (Stack<SyntaxNode>)nodesProperty!.GetValue(visitor)!;
        nodes.Push(SyntaxFactory.IdentifierName("testData"));

        // Act
        visitor.Visit(interpretCallNode);


        Assert.HasCount(1, nodes, "One node should be on the stack");

        var statementsProperty = visitor.GetType().GetProperty("Statements",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var statements = (List<StatementSyntax>)statementsProperty!.GetValue(visitor)!;

        Assert.IsGreaterThanOrEqualTo(1, statements.Count, "At least one statement should be generated");

        var allCode = string.Join("\n", statements.Select(s => s.ToFullString()));

        // The class name is now just the schema name without "Interpreter" suffix
        Assert.Contains("FallbackSchema",
            allCode, "Generated code should use schema name as class name");
    }

    #endregion

    #region CROSS APPLY Integration Tests

    [TestMethod]
    public void InterpretFromNode_ShouldWrapInterpretCallNode_Correctly()
    {
        var dataSource = new IntegerNode("1");
        var interpretCallNode = new InterpretCallNode(dataSource, "TestSchema", null);


        var interpretFromNode = new InterpretFromNode("s", interpretCallNode, ApplyType.Cross);

        // Assert
        Assert.AreEqual("s", interpretFromNode.Alias, "Alias should be preserved");
        Assert.AreEqual("TestSchema", interpretFromNode.SchemaName,
            "SchemaName should be accessible from wrapped node");
        Assert.AreEqual(ApplyType.Cross, interpretFromNode.ApplyType, "ApplyType should be Cross");
        Assert.AreSame(interpretCallNode, interpretFromNode.InterpretCall,
            "InterpretCall should reference the wrapped node");
    }

    [TestMethod]
    public void InterpretFromNode_WithParseCall_ShouldExtractSchemaName()
    {
        // Arrange
        var dataSource = new StringNode("text data");
        var parseCallNode = new ParseCallNode(dataSource, "TextFormat", null);

        // Act
        var interpretFromNode = new InterpretFromNode("p", parseCallNode, ApplyType.Cross);

        // Assert
        Assert.AreEqual("TextFormat", interpretFromNode.SchemaName);
        Assert.IsInstanceOfType(interpretFromNode.InterpretCall, typeof(ParseCallNode));
    }

    [TestMethod]
    public void InterpretFromNode_WithInterpretAtCall_ShouldExtractSchemaName()
    {
        // Arrange
        var dataSource = new IntegerNode("1");
        var offsetNode = new IntegerNode("256");
        var interpretAtCallNode = new InterpretAtCallNode(dataSource, offsetNode, "HeaderFormat", null);

        // Act
        var interpretFromNode = new InterpretFromNode("h", interpretAtCallNode, ApplyType.Outer);

        // Assert
        Assert.AreEqual("HeaderFormat", interpretFromNode.SchemaName);
        Assert.AreEqual(ApplyType.Outer, interpretFromNode.ApplyType);
        Assert.IsInstanceOfType(interpretFromNode.InterpretCall, typeof(InterpretAtCallNode));
    }

    [TestMethod]
    public void InterpretFromNode_CrossApplyType_ShouldHaveCrossApplyType()
    {
        // Arrange
        var dataSource = new IntegerNode("1");
        var interpretCallNode = new InterpretCallNode(dataSource, "BinaryFormat", null);

        // Act
        var interpretFromNode = new InterpretFromNode("x", interpretCallNode, ApplyType.Cross);

        // Assert
        Assert.AreEqual(ApplyType.Cross, interpretFromNode.ApplyType);
    }

    [TestMethod]
    public void InterpretFromNode_OuterApplyType_ShouldHaveOuterApplyType()
    {
        // Arrange
        var dataSource = new IntegerNode("1");
        var interpretCallNode = new InterpretCallNode(dataSource, "BinaryFormat", null);

        // Act
        var interpretFromNode = new InterpretFromNode("x", interpretCallNode, ApplyType.Outer);

        // Assert
        Assert.AreEqual(ApplyType.Outer, interpretFromNode.ApplyType);
    }

    [TestMethod]
    public void InterpretFromNode_Id_ShouldBeUnique()
    {
        // Arrange
        var dataSource1 = new IntegerNode("1");
        var interpretCall1 = new InterpretCallNode(dataSource1, "Schema1", null);
        var node1 = new InterpretFromNode("a", interpretCall1, ApplyType.Cross);

        var dataSource2 = new IntegerNode("2");
        var interpretCall2 = new InterpretCallNode(dataSource2, "Schema2", null);
        var node2 = new InterpretFromNode("b", interpretCall2, ApplyType.Outer);


        Assert.AreNotEqual(node1.Id, node2.Id, "Different InterpretFromNodes should have different IDs");
    }

    [TestMethod]
    public void InterpretFromNode_Accept_ShouldCallVisitorMethod()
    {
        // Arrange
        var dataSource = new IntegerNode("1");
        var interpretCallNode = new InterpretCallNode(dataSource, "TestSchema", null);
        var interpretFromNode = new InterpretFromNode("s", interpretCallNode, ApplyType.Cross);

        var visitedNodes = new List<Node>();
        var mockVisitor = new TrackingVisitor(visitedNodes);

        // Act
        interpretFromNode.Accept(mockVisitor);


        Assert.IsTrue(visitedNodes.OfType<InterpretFromNode>().Any(),
            "Visitor.Visit(InterpretFromNode) should be called");
    }

    /// <summary>
    ///     Simple visitor that tracks which nodes it visits.
    /// </summary>
    private class TrackingVisitor : NoOpExpressionVisitor
    {
        private readonly List<Node> _visitedNodes;

        public TrackingVisitor(List<Node> visitedNodes)
        {
            _visitedNodes = visitedNodes;
        }

        public override void Visit(InterpretFromNode node)
        {
            _visitedNodes.Add(node);
        }
    }

    #endregion
}
