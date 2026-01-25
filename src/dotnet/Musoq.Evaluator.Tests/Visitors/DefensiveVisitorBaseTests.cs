using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Visitors;

/// <summary>
///     Tests for DefensiveVisitorBase class
/// </summary>
[TestClass]
public class DefensiveVisitorBaseTests
{
    #region VisitorName Tests

    [TestMethod]
    public void VisitorName_ShouldBeAccessible()
    {
        // Arrange
        var visitor = new TestVisitor();

        // Act
        var name = visitor.GetVisitorName();

        // Assert
        Assert.AreEqual("TestVisitor", name);
    }

    #endregion

    #region Test Helper Class

    private class TestVisitor : DefensiveVisitorBase
    {
        protected override string VisitorName => "TestVisitor";

        public string GetVisitorName()
        {
            return VisitorName;
        }

        public Node TestSafePop(Stack<Node> nodes, string operation)
        {
            return SafePop(nodes, operation);
        }

        public Node[] TestSafePopMultiple(Stack<Node> nodes, int count, string operation)
        {
            return SafePopMultiple(nodes, count, operation);
        }

        public Node TestSafePeek(Stack<Node> nodes, string operation)
        {
            return SafePeek(nodes, operation);
        }

        public T TestSafeCast<T>(Node node, string operation) where T : Node
        {
            return SafeCast<T>(node, operation);
        }

        public void TestValidateConstructorParameter(string name, object value, string op)
        {
            ValidateConstructorParameter(name, value, op);
        }

        public void TestValidateStringParameter(string name, string value, string op)
        {
            ValidateStringParameter(name, value, op);
        }

        public void TestSafeExecuteAction(Action action, string operation)
        {
            SafeExecute(action, operation);
        }

        public T TestSafeExecuteFunc<T>(Func<T> func, string operation)
        {
            return SafeExecute(func, operation);
        }
    }

    #endregion

    #region SafePop Tests

    [TestMethod]
    public void SafePop_WithNonEmptyStack_ShouldReturnNode()
    {
        // Arrange
        var visitor = new TestVisitor();
        var stack = new Stack<Node>();
        var node = new IntegerNode(42);
        stack.Push(node);

        // Act
        var result = visitor.TestSafePop(stack, "TestOp");

        // Assert
        Assert.AreSame(node, result);
    }

    [TestMethod]
    public void SafePop_WithEmptyStack_ShouldThrowVisitorException()
    {
        // Arrange
        var visitor = new TestVisitor();
        var stack = new Stack<Node>();

        // Act & Assert
        var ex = Assert.Throws<VisitorException>(() => visitor.TestSafePop(stack, "TestOp"));
        Assert.Contains("Stack underflow", ex.Message);
    }

    [TestMethod]
    public void SafePop_WithNullStack_ShouldThrowArgumentNullException()
    {
        // Arrange
        var visitor = new TestVisitor();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => visitor.TestSafePop(null, "TestOp"));
    }

    #endregion

    #region SafePopMultiple Tests

    [TestMethod]
    public void SafePopMultiple_WithEnoughItems_ShouldReturnNodes()
    {
        // Arrange
        var visitor = new TestVisitor();
        var stack = new Stack<Node>();
        var node1 = new IntegerNode(1);
        var node2 = new IntegerNode(2);
        var node3 = new IntegerNode(3);
        stack.Push(node1);
        stack.Push(node2);
        stack.Push(node3);

        // Act
        var result = visitor.TestSafePopMultiple(stack, 2, "TestOp");

        // Assert
        Assert.HasCount(2, result);
        Assert.AreSame(node2, result[0]);
        Assert.AreSame(node3, result[1]);
    }

    [TestMethod]
    public void SafePopMultiple_WithInsufficientItems_ShouldThrow()
    {
        // Arrange
        var visitor = new TestVisitor();
        var stack = new Stack<Node>();
        stack.Push(new IntegerNode(1));

        // Act & Assert
        var ex = Assert.Throws<VisitorException>(() => visitor.TestSafePopMultiple(stack, 3, "TestOp"));
        Assert.Contains("Stack underflow", ex.Message);
    }

    [TestMethod]
    public void SafePopMultiple_WithNegativeCount_ShouldThrow()
    {
        // Arrange
        var visitor = new TestVisitor();
        var stack = new Stack<Node>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => visitor.TestSafePopMultiple(stack, -1, "TestOp"));
    }

    [TestMethod]
    public void SafePopMultiple_WithZeroCount_ShouldReturnEmptyArray()
    {
        // Arrange
        var visitor = new TestVisitor();
        var stack = new Stack<Node>();

        // Act
        var result = visitor.TestSafePopMultiple(stack, 0, "TestOp");

        // Assert
        Assert.IsEmpty(result);
    }

    #endregion

    #region SafePeek Tests

    [TestMethod]
    public void SafePeek_WithNonEmptyStack_ShouldReturnNodeWithoutRemoving()
    {
        // Arrange
        var visitor = new TestVisitor();
        var stack = new Stack<Node>();
        var node = new IntegerNode(42);
        stack.Push(node);

        // Act
        var result = visitor.TestSafePeek(stack, "TestOp");

        // Assert
        Assert.AreSame(node, result);
        Assert.HasCount(1, stack); // Still on stack
    }

    [TestMethod]
    public void SafePeek_WithEmptyStack_ShouldThrowVisitorException()
    {
        // Arrange
        var visitor = new TestVisitor();
        var stack = new Stack<Node>();

        // Act & Assert
        var ex = Assert.Throws<VisitorException>(() => visitor.TestSafePeek(stack, "TestOp"));
        Assert.Contains("Stack underflow", ex.Message);
    }

    #endregion

    #region SafeCast Tests

    [TestMethod]
    public void SafeCast_WithCorrectType_ShouldReturnCastNode()
    {
        // Arrange
        var visitor = new TestVisitor();
        Node node = new IntegerNode(42);

        // Act
        var result = visitor.TestSafeCast<IntegerNode>(node, "TestOp");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(IntegerNode));
    }

    [TestMethod]
    public void SafeCast_WithWrongType_ShouldThrowVisitorException()
    {
        // Arrange
        var visitor = new TestVisitor();
        Node node = new IntegerNode(42);

        // Act & Assert
        var ex = Assert.Throws<VisitorException>(() => visitor.TestSafeCast<StringNode>(node, "TestOp"));
        Assert.Contains("Invalid node type", ex.Message);
    }

    [TestMethod]
    public void SafeCast_WithNullNode_ShouldThrowVisitorException()
    {
        // Arrange
        var visitor = new TestVisitor();

        // Act & Assert
        var ex = Assert.Throws<VisitorException>(() => visitor.TestSafeCast<IntegerNode>(null, "TestOp"));
        Assert.Contains("null", ex.Message);
    }

    #endregion

    #region ValidateConstructorParameter Tests

    [TestMethod]
    public void ValidateConstructorParameter_WithNonNullValue_ShouldNotThrow()
    {
        // Arrange
        var visitor = new TestVisitor();

        // Act & Assert - should not throw
        visitor.TestValidateConstructorParameter("param", "value", "ctor");
    }

    [TestMethod]
    public void ValidateConstructorParameter_WithNullValue_ShouldThrow()
    {
        // Arrange
        var visitor = new TestVisitor();

        // Act & Assert
        var ex = Assert.Throws<VisitorException>(() => visitor.TestValidateConstructorParameter("param", null, "ctor"));
        Assert.Contains("param", ex.Message);
    }

    #endregion

    #region ValidateStringParameter Tests

    [TestMethod]
    public void ValidateStringParameter_WithValidString_ShouldNotThrow()
    {
        // Arrange
        var visitor = new TestVisitor();

        // Act & Assert - should not throw
        visitor.TestValidateStringParameter("param", "value", "op");
    }

    [TestMethod]
    public void ValidateStringParameter_WithNullString_ShouldThrow()
    {
        // Arrange
        var visitor = new TestVisitor();

        // Act & Assert
        var ex = Assert.Throws<VisitorException>(() => visitor.TestValidateStringParameter("param", null, "op"));
        Assert.Contains("param", ex.Message);
    }

    [TestMethod]
    public void ValidateStringParameter_WithEmptyString_ShouldThrow()
    {
        // Arrange
        var visitor = new TestVisitor();

        // Act & Assert
        var ex = Assert.Throws<VisitorException>(() => visitor.TestValidateStringParameter("param", "", "op"));
        Assert.Contains("param", ex.Message);
    }

    #endregion

    #region SafeExecute Tests

    [TestMethod]
    public void SafeExecute_Action_WithSuccessfulAction_ShouldComplete()
    {
        // Arrange
        var visitor = new TestVisitor();
        var executed = false;

        // Act
        visitor.TestSafeExecuteAction(() => executed = true, "TestOp");

        // Assert
        Assert.IsTrue(executed);
    }

    [TestMethod]
    public void SafeExecute_Action_WithException_ShouldWrapInVisitorException()
    {
        // Arrange
        var visitor = new TestVisitor();

        // Act & Assert
        var ex = Assert.Throws<VisitorException>(() =>
            visitor.TestSafeExecuteAction(() => throw new InvalidOperationException("Test"), "TestOp"));
        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidOperationException));
    }

    [TestMethod]
    public void SafeExecute_Action_WithVisitorException_ShouldRethrow()
    {
        // Arrange
        var visitor = new TestVisitor();
        var original = new VisitorException("Test", "Op", "Message");

        // Act & Assert
        var ex = Assert.Throws<VisitorException>(() =>
            visitor.TestSafeExecuteAction(() => throw original, "TestOp"));
        Assert.AreSame(original, ex);
    }

    [TestMethod]
    public void SafeExecute_Func_WithSuccessfulFunc_ShouldReturnResult()
    {
        // Arrange
        var visitor = new TestVisitor();

        // Act
        var result = visitor.TestSafeExecuteFunc(() => 42, "TestOp");

        // Assert
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void SafeExecute_Func_WithException_ShouldWrapInVisitorException()
    {
        // Arrange
        var visitor = new TestVisitor();

        // Act & Assert
        var ex = Assert.Throws<VisitorException>(() =>
            visitor.TestSafeExecuteFunc<int>(() => throw new InvalidOperationException("Test"), "TestOp"));
        Assert.IsNotNull(ex.InnerException);
    }

    #endregion
}
