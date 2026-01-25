using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Evaluator.Tests.Visitors;

[TestClass]
public class RewriteQueryVisitorTests
{
    private Scope _scope;
    private RewriteQueryVisitor _visitor;

    [TestInitialize]
    public void Setup()
    {
        _visitor = new RewriteQueryVisitor();
        _scope = new Scope(null, 0, "test");
        _visitor.SetScope(_scope);
    }

    private Stack<Node> GetPrivateNodes()
    {
        var nodesProperty = _visitor.GetType().GetProperty("Nodes", BindingFlags.NonPublic | BindingFlags.Instance);
        return (Stack<Node>)nodesProperty?.GetValue(_visitor);
    }

    private void PushNode(Node node)
    {
        var nodes = GetPrivateNodes();
        nodes.Push(node);
    }

    private int GetNodesCount()
    {
        var nodes = GetPrivateNodes();
        return nodes.Count;
    }

    [TestMethod]
    public void Visit_StarNode_ShouldProcessCorrectly()
    {
        // Arrange
        var left = new IntegerNode("5");
        var right = new IntegerNode("3");
        var starNode = new StarNode(left, right);


        PushNode(left);
        PushNode(right);

        // Act
        _visitor.Visit(starNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Should have one result node after multiplication");
    }

    [TestMethod]
    public void Visit_AddNode_ShouldProcessCorrectly()
    {
        // Arrange
        var left = new IntegerNode("10");
        var right = new IntegerNode("20");
        var addNode = new AddNode(left, right);


        PushNode(left);
        PushNode(right);

        // Act
        _visitor.Visit(addNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Should have one result node after addition");
    }

    [TestMethod]
    public void Visit_EqualityNode_ShouldProcessCorrectly()
    {
        // Arrange
        var left = new IntegerNode("5");
        var right = new IntegerNode("5");
        var equalityNode = new EqualityNode(left, right);


        PushNode(left);
        PushNode(right);

        // Act
        _visitor.Visit(equalityNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Should have one result node after equality comparison");
    }

    [TestMethod]
    public void Visit_AndNode_ShouldProcessCorrectly()
    {
        var left = new BooleanNode(true);
        var right = new BooleanNode(false);
        var andNode = new AndNode(left, right);


        PushNode(left);
        PushNode(right);


        _visitor.Visit(andNode);


        Assert.AreEqual(1, GetNodesCount(), "Should have one result node after AND operation");
    }

    [TestMethod]
    public void Visit_FieldNode_ShouldProcessCorrectly()
    {
        // Arrange
        var expression = new IdentifierNode("TestField");
        var fieldNode = new FieldNode(expression, 0, "TestField");


        PushNode(expression);

        // Act
        _visitor.Visit(fieldNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Should have one result node after field processing");
    }

    [TestMethod]
    public void Visit_ArgsListNode_ShouldProcessCorrectly()
    {
        // Arrange
        var arg1 = new IntegerNode("1");
        var arg2 = new StringNode("test");
        var argsListNode = new ArgsListNode([arg1, arg2]);


        PushNode(arg2);
        PushNode(arg1);

        // Act
        _visitor.Visit(argsListNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Should have one result node after args list processing");
    }

    [TestMethod]
    public void Visit_GroupByNode_ShouldProcessCorrectly()
    {
        // Arrange
        var field1 = new FieldNode(new IdentifierNode("Field1"), 0, "Field1");
        var field2 = new FieldNode(new IdentifierNode("Field2"), 1, "Field2");
        var groupByNode = new GroupByNode([field1, field2], null);


        PushNode(field2);
        PushNode(field1);

        // Act
        _visitor.Visit(groupByNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Should have one result node after group by processing");
    }

    [TestMethod]
    public void Visit_SkipNode_ShouldProcessCorrectly()
    {
        // Arrange
        var skipNode = new SkipNode(new IntegerNode("10"));

        // Act
        _visitor.Visit(skipNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Should have one result node after skip processing");
    }

    [TestMethod]
    public void Visit_TakeNode_ShouldProcessCorrectly()
    {
        // Arrange
        var takeNode = new TakeNode(new IntegerNode("20"));

        // Act
        _visitor.Visit(takeNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Should have one result node after take processing");
    }

    [TestMethod]
    public void Visit_SchemaFromNode_ShouldProcessCorrectly()
    {
        // Arrange
        var argsListNode = new ArgsListNode([]);
        var schemaFromNode = new SchemaFromNode("testSchema", "testMethod", argsListNode, "alias", typeof(object), 0);


        PushNode(argsListNode);

        // Act
        _visitor.Visit(schemaFromNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Should have one result node after schema from processing");
    }


    [TestMethod]
    public void Visit_StarNode_WithEmptyStack_ShouldThrowInvalidOperationException()
    {
        var starNode = new StarNode(new IntegerNode("5"), new IntegerNode("3"));


        Assert.Throws<InvalidOperationException>(() => _visitor.Visit(starNode));
    }

    [TestMethod]
    public void Visit_AddNode_WithInsufficientNodes_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var addNode = new AddNode(new IntegerNode("10"), new IntegerNode("20"));


        PushNode(new IntegerNode("10"));

        // Act
        Assert.Throws<InvalidOperationException>(() => _visitor.Visit(addNode));
    }

    [TestMethod]
    public void Visit_EqualityNode_WithEmptyStack_ShouldThrowInvalidOperationException()
    {
        var equalityNode = new EqualityNode(new IntegerNode("5"), new IntegerNode("5"));


        Assert.Throws<InvalidOperationException>(() => _visitor.Visit(equalityNode));
    }

    [TestMethod]
    public void Visit_AndNode_WithInsufficientNodes_ShouldThrowInvalidOperationException()
    {
        var andNode = new AndNode(new BooleanNode(true), new BooleanNode(false));


        PushNode(new BooleanNode(true));


        Assert.Throws<InvalidOperationException>(() => _visitor.Visit(andNode));
    }

    [TestMethod]
    public void Visit_FieldNode_WithEmptyStack_ShouldThrowInvalidOperationException()
    {
        var fieldNode = new FieldNode(new IdentifierNode("TestField"), 0, "TestField");


        Assert.Throws<InvalidOperationException>(() => _visitor.Visit(fieldNode));
    }

    [TestMethod]
    public void Visit_ArgsListNode_WithInsufficientNodes_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var argsListNode = new ArgsListNode([new IntegerNode("1"), new StringNode("test")]);


        PushNode(new IntegerNode("1"));

        // Act
        Assert.Throws<InvalidOperationException>(() => _visitor.Visit(argsListNode));
    }

    [TestMethod]
    public void Visit_GroupByNode_WithInsufficientNodes_ShouldThrowInvalidOperationException()
    {
        var field1 = new FieldNode(new IdentifierNode("Field1"), 0, "Field1");
        var field2 = new FieldNode(new IdentifierNode("Field2"), 1, "Field2");
        var groupByNode = new GroupByNode([field1, field2], null);


        PushNode(field1);


        Assert.Throws<InvalidOperationException>(() => _visitor.Visit(groupByNode));
    }

    [TestMethod]
    public void Visit_SchemaFromNode_WithEmptyStack_ShouldThrowInvalidOperationException()
    {
        var argsListNode = new ArgsListNode([]);
        var schemaFromNode = new SchemaFromNode("testSchema", "testMethod", argsListNode, "alias", typeof(object), 0);


        Assert.Throws<InvalidOperationException>(() => _visitor.Visit(schemaFromNode));
    }

    [TestMethod]
    public void Visit_StarNode_WithNullOperands_ShouldThrowArgumentException()
    {
        // Arrange
        var starNode = new StarNode(new IntegerNode("5"), new IntegerNode("3"));


        PushNode(null);
        PushNode(new IntegerNode("5"));


        Assert.Throws<ArgumentException>(() => _visitor.Visit(starNode));
    }

    [TestMethod]
    public void Multiple_Operations_ShouldMaintainStackConsistency()
    {
        // Arrange
        var val1 = new IntegerNode("10");
        var val2 = new IntegerNode("20");
        var val3 = new IntegerNode("30");


        PushNode(val1);
        PushNode(val2);
        _visitor.Visit(new AddNode(val1, val2));

        PushNode(val3);
        _visitor.Visit(new StarNode(val1, val3));

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Stack should be consistent after multiple operations");
    }

    [TestMethod]
    public void Scope_Operations_ShouldNotThrowWithValidScope()
    {
        var field = new FieldNode(new IdentifierNode("TestField"), 0, "TestField");
        PushNode(new IdentifierNode("TestField"));


        _visitor.Visit(field);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Should handle scope operations correctly");
    }

    [TestMethod]
    public void SetScope_WithNull_ShouldNotThrowImmediately()
    {
        _visitor.SetScope(null);
    }


    [TestMethod]
    public void Visit_ComplexExpression_ShouldHandleCorrectly()
    {
        var five = new IntegerNode("5");
        var three = new IntegerNode("3");
        var two = new IntegerNode("2");


        PushNode(five);
        PushNode(three);
        _visitor.Visit(new AddNode(five, three));


        PushNode(two);
        _visitor.Visit(new StarNode(five,
            two));

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Complex expression should result in single node");
    }

    [TestMethod]
    public void Visit_JoinFromNode_ShouldProcessCorrectly()
    {
        // Arrange
        var leftFrom = new SchemaFromNode("leftSchema", "leftMethod", new ArgsListNode([]), "left", typeof(object), 0);
        var rightFrom =
            new SchemaFromNode("rightSchema", "rightMethod", new ArgsListNode([]), "right", typeof(object), 1);
        var joinExpression = new EqualityNode(new IdentifierNode("left.id"), new IdentifierNode("right.id"));
        var joinNode = new JoinFromNode(leftFrom, rightFrom, joinExpression, JoinType.Inner, typeof(object));


        PushNode(leftFrom);
        PushNode(rightFrom);
        PushNode(joinExpression);

        // Act
        _visitor.Visit(joinNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Join operation should result in single node");
    }

    [TestMethod]
    public void Visit_HavingNode_ShouldProcessCorrectly()
    {
        // Arrange
        var havingExpression = new GreaterNode(new IdentifierNode("count"), new IntegerNode("5"));
        var havingNode = new HavingNode(havingExpression);


        PushNode(havingExpression);

        // Act
        _visitor.Visit(havingNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Having node should result in single node");
    }

    [TestMethod]
    public void Visit_HavingNode_WithEmptyStack_ShouldThrow()
    {
        var havingNode = new HavingNode(new GreaterNode(new IdentifierNode("count"), new IntegerNode("5")));


        Assert.Throws<InvalidOperationException>(() => _visitor.Visit(havingNode));
    }

    [TestMethod]
    public void Visit_CreateTransformationTableNode_ShouldProcessCorrectly()
    {
        // Arrange
        var field1 = new FieldNode(new IdentifierNode("Field1"), 0, "Field1");
        var field2 = new FieldNode(new IdentifierNode("Field2"), 1, "Field2");
        var createTableNode = new CreateTransformationTableNode("TestTable", [], [field1, field2], false);


        PushNode(field2);
        PushNode(field1);

        // Act
        _visitor.Visit(createTableNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Create table should result in single node");
    }

    [TestMethod]
    public void Visit_RenameTableNode_ShouldProcessCorrectly()
    {
        // Arrange
        var renameNode = new RenameTableNode("OldName", "NewName");

        // Act
        _visitor.Visit(renameNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Rename table should result in single node");
    }

    [TestMethod]
    public void Visit_InMemoryTableFromNode_ShouldProcessCorrectly()
    {
        // Arrange
        var inMemoryNode = new InMemoryTableFromNode("variableName", "alias", typeof(object));

        // Act
        _visitor.Visit(inMemoryNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "In memory table should result in single node");
    }

    [TestMethod]
    public void Visit_AccessMethodFromNode_ShouldProcessCorrectly()
    {
        var simpleNode = new InMemoryTableFromNode("variableName", "alias", typeof(object));

        // Act
        _visitor.Visit(simpleNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Simple from node should result in single node");
    }

    [TestMethod]
    public void Stack_Underflow_Prevention_ShouldProvideInformativeErrors()
    {
        var operations = new[]
        {
            (Action)(() => _visitor.Visit(new StarNode(new IntegerNode("1"), new IntegerNode("2")))),
            (Action)(() => _visitor.Visit(new AddNode(new IntegerNode("1"), new IntegerNode("2")))),
            (Action)(() => _visitor.Visit(new EqualityNode(new IntegerNode("1"), new IntegerNode("2")))),
            (Action)(() => _visitor.Visit(new AndNode(new BooleanNode(true), new BooleanNode(false))))
        };

        foreach (var operation in operations)
            try
            {
                operation();
                Assert.Fail("Expected InvalidOperationException was not thrown");
            }
            catch (InvalidOperationException ex)
            {
                Assert.Contains("Stack must contain at least",
                    ex.Message, $"Error message should be informative: {ex.Message}");
            }
    }

    [TestMethod]
    public void Null_Operand_Prevention_ShouldProvideInformativeErrors()
    {
        var operations = new[]
        {
            () =>
            {
                PushNode(new IntegerNode("1"));
                PushNode(null);
                _visitor.Visit(new StarNode(new IntegerNode("1"), new IntegerNode("2")));
            },
            () =>
            {
                PushNode(null);
                PushNode(new IntegerNode("2"));
                _visitor.Visit(new AddNode(new IntegerNode("1"), new IntegerNode("2")));
            }
        };

        foreach (var operation in operations)
        {
            try
            {
                operation();
                Assert.Fail("Expected ArgumentException was not thrown");
            }
            catch (ArgumentException ex)
            {
                Assert.Contains("operand cannot be null",
                    ex.Message, $"Error message should be informative: {ex.Message}");
            }


            while (GetNodesCount() > 0) GetPrivateNodes().Pop();
        }
    }

    #region Interpret Function Detection Tests

    [TestMethod]
    public void Visit_AccessMethodNode_WithInterpretCall_ShouldTransformToInterpretCallNode()
    {
        // Arrange
        var dataSourceNode = new AccessColumnNode("data", "source", typeof(byte[]), TextSpan.Empty);
        var schemaNameNode = new StringNode("MySchema");
        var argsListNode = new ArgsListNode([dataSourceNode, schemaNameNode]);
        var functionToken = new FunctionToken("Interpret", TextSpan.Empty);
        var accessMethodNode = new AccessMethodNode(functionToken, argsListNode, null, false);


        PushNode(argsListNode);

        // Act
        _visitor.Visit(accessMethodNode);

        // Assert
        var result = GetPrivateNodes().Pop();
        Assert.IsInstanceOfType(result, typeof(InterpretCallNode));
        var interpretNode = (InterpretCallNode)result;
        Assert.AreEqual("MySchema", interpretNode.SchemaName);
    }

    [TestMethod]
    public void Visit_AccessMethodNode_WithParseCall_ShouldTransformToParseCallNode()
    {
        // Arrange
        var dataSourceNode = new AccessColumnNode("data", "source", typeof(byte[]), TextSpan.Empty);
        var schemaNameNode = new StringNode("MyParser");
        var argsListNode = new ArgsListNode([dataSourceNode, schemaNameNode]);
        var functionToken = new FunctionToken("Parse", TextSpan.Empty);
        var accessMethodNode = new AccessMethodNode(functionToken, argsListNode, null, false);


        PushNode(argsListNode);

        // Act
        _visitor.Visit(accessMethodNode);

        // Assert
        var result = GetPrivateNodes().Pop();
        Assert.IsInstanceOfType(result, typeof(ParseCallNode));
        var parseNode = (ParseCallNode)result;
        Assert.AreEqual("MyParser", parseNode.SchemaName);
    }

    [TestMethod]
    public void Visit_AccessMethodNode_WithInterpretAtCall_ShouldTransformToInterpretAtCallNode()
    {
        // Arrange
        var dataSourceNode = new AccessColumnNode("data", "source", typeof(byte[]), TextSpan.Empty);
        var offsetNode = new IntegerNode("10");
        var schemaNameNode = new StringNode("MyOffsetSchema");
        var argsListNode = new ArgsListNode([dataSourceNode, offsetNode, schemaNameNode]);
        var functionToken = new FunctionToken("InterpretAt", TextSpan.Empty);
        var accessMethodNode = new AccessMethodNode(functionToken, argsListNode, null, false);


        PushNode(argsListNode);

        // Act
        _visitor.Visit(accessMethodNode);

        // Assert
        var result = GetPrivateNodes().Pop();
        Assert.IsInstanceOfType(result, typeof(InterpretAtCallNode));
        var interpretAtNode = (InterpretAtCallNode)result;
        Assert.AreEqual("MyOffsetSchema", interpretAtNode.SchemaName);
    }

    [TestMethod]
    public void Visit_AccessMethodNode_WithNonInterpretFunction_ShouldRemainAsAccessMethodNode()
    {
        // Arrange
        var arg1 = new IntegerNode("42");
        var argsListNode = new ArgsListNode([arg1]);
        var functionToken = new FunctionToken("SomeOtherFunction", TextSpan.Empty);
        var accessMethodNode = new AccessMethodNode(functionToken, argsListNode, null, false);


        PushNode(argsListNode);

        // Act
        _visitor.Visit(accessMethodNode);

        // Assert
        var result = GetPrivateNodes().Pop();
        Assert.IsInstanceOfType(result, typeof(AccessMethodNode));
        var methodNode = (AccessMethodNode)result;
        Assert.AreEqual("SomeOtherFunction", methodNode.FunctionToken.Value);
    }

    [TestMethod]
    public void Visit_AccessMethodNode_InterpretWithWrongArgCount_ShouldRemainAsAccessMethodNode()
    {
        var dataSourceNode = new AccessColumnNode("data", "source", typeof(byte[]), TextSpan.Empty);
        var argsListNode = new ArgsListNode([dataSourceNode]);
        var functionToken = new FunctionToken("Interpret", TextSpan.Empty);
        var accessMethodNode = new AccessMethodNode(functionToken, argsListNode, null, false);


        PushNode(argsListNode);


        _visitor.Visit(accessMethodNode);


        var result = GetPrivateNodes().Pop();
        Assert.IsInstanceOfType(result, typeof(AccessMethodNode));
    }

    [TestMethod]
    public void Visit_AccessMethodNode_InterpretCaseInsensitive_ShouldTransformToInterpretCallNode()
    {
        var dataSourceNode = new AccessColumnNode("data", "source", typeof(byte[]), TextSpan.Empty);
        var schemaNameNode = new StringNode("MySchema");
        var argsListNode = new ArgsListNode([dataSourceNode, schemaNameNode]);
        var functionToken = new FunctionToken("INTERPRET", TextSpan.Empty);
        var accessMethodNode = new AccessMethodNode(functionToken, argsListNode, null, false);


        PushNode(argsListNode);


        _visitor.Visit(accessMethodNode);


        var result = GetPrivateNodes().Pop();
        Assert.IsInstanceOfType(result, typeof(InterpretCallNode));
    }

    #endregion
}
