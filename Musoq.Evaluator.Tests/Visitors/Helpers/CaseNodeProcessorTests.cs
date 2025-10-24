using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Visitors.Helpers
{
    [TestClass]
    public class CaseNodeProcessorTests
    {
        [TestMethod]
        public void ProcessCaseNode_WithValidSingleWhenThen_ReturnsCorrectResult()
        {
            // Arrange
            var nodes = new Stack<SyntaxNode>();
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("else")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("then1")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));
            
            var whenThenPairs = new (Node When, Node Then)[] { (null, null) };
            var caseNode = new CaseNode(whenThenPairs, null, typeof(string));
            
            var typesToInstantiate = new Dictionary<string, Type>();
            var caseWhenMethodIndex = 1;
            
            // Act
            var result = CaseNodeProcessor.ProcessCaseNode(
                caseNode, nodes, typesToInstantiate, MethodAccessType.ResultQuery, "test", ref caseWhenMethodIndex);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Method);
            Assert.IsNotNull(result.MethodInvocation);
            Assert.IsNotNull(result.RequiredNamespaces);
            Assert.AreEqual(2, caseWhenMethodIndex);
            Assert.AreEqual("CaseWhen_1", result.Method.Identifier.ValueText);
        }

        [TestMethod]
        public void ProcessCaseNode_WithMultipleWhenThenPairs_CreatesNestedIfElseChain()
        {
            // Arrange
            var nodes = new Stack<SyntaxNode>();
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("else")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("then2")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("then1")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));
            
            var whenThenPairs = new (Node When, Node Then)[] { (null, null), (null, null) };
            var caseNode = new CaseNode(whenThenPairs, null, typeof(string));
            
            var typesToInstantiate = new Dictionary<string, Type>();
            var caseWhenMethodIndex = 1;
            
            // Act
            var result = CaseNodeProcessor.ProcessCaseNode(
                caseNode, nodes, typesToInstantiate, MethodAccessType.ResultQuery, "test", ref caseWhenMethodIndex);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Method);
            var methodBody = result.Method.Body.Statements.First() as IfStatementSyntax;
            Assert.IsNotNull(methodBody);
            Assert.IsNotNull(methodBody.Else);
        }

        [TestMethod]
        public void ProcessCaseNode_WithTypesToInstantiate_AddsCorrectParameters()
        {
            // Arrange
            var nodes = new Stack<SyntaxNode>();
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("else")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("then1")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));
            
            var whenThenPairs = new (Node When, Node Then)[] { (null, null) };
            var caseNode = new CaseNode(whenThenPairs, null, typeof(string));
            
            var typesToInstantiate = new Dictionary<string, Type>
            {
                { "var1", typeof(int) },
                { "var2", typeof(string) }
            };
            var caseWhenMethodIndex = 1;
            
            // Act
            var result = CaseNodeProcessor.ProcessCaseNode(
                caseNode, nodes, typesToInstantiate, MethodAccessType.ResultQuery, "test", ref caseWhenMethodIndex);
            
            // Assert
            Assert.IsNotNull(result.Method);
            Assert.AreEqual(3, result.Method.ParameterList.Parameters.Count); // score + 2 instantiated types
            Assert.IsTrue(result.Method.ParameterList.Parameters.Any(p => p.Identifier.ValueText == "var1"));
            Assert.IsTrue(result.Method.ParameterList.Parameters.Any(p => p.Identifier.ValueText == "var2"));
        }

        [TestMethod]
        public void ProcessCaseNode_WithTransformingQuery_UsesCorrectRowVariableName()
        {
            // Arrange
            var nodes = new Stack<SyntaxNode>();
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("else")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("then1")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));
            
            var whenThenPairs = new (Node When, Node Then)[] { (null, null) };
            var caseNode = new CaseNode(whenThenPairs, null, typeof(string));
            
            var typesToInstantiate = new Dictionary<string, Type>();
            var caseWhenMethodIndex = 1;
            
            // Act
            var result = CaseNodeProcessor.ProcessCaseNode(
                caseNode, nodes, typesToInstantiate, MethodAccessType.TransformingQuery, "myQuery", ref caseWhenMethodIndex);
            
            // Assert
            Assert.IsNotNull(result.MethodInvocation);
            var argument = result.MethodInvocation.ArgumentList.Arguments.First();
            var identifier = argument.Expression as IdentifierNameSyntax;
            Assert.IsNotNull(identifier);
            Assert.AreEqual("myQueryRow", identifier.Identifier.ValueText);
        }

        [TestMethod]
        public void ProcessCaseNode_WithDifferentReturnType_UsesCorrectType()
        {
            // Arrange
            var nodes = new Stack<SyntaxNode>();
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));
            
            var whenThenPairs = new (Node When, Node Then)[] { (null, null) };
            var caseNode = new CaseNode(whenThenPairs, null, typeof(int));
            
            var typesToInstantiate = new Dictionary<string, Type>();
            var caseWhenMethodIndex = 1;
            
            // Act
            var result = CaseNodeProcessor.ProcessCaseNode(
                caseNode, nodes, typesToInstantiate, MethodAccessType.ResultQuery, "test", ref caseWhenMethodIndex);
            
            // Assert
            Assert.IsNotNull(result.Method);
            var returnType = result.Method.ReturnType as IdentifierNameSyntax;
            Assert.IsNotNull(returnType);
            Assert.AreEqual("System.Int32", returnType.Identifier.ValueText);
        }

        [TestMethod]
        public void ProcessCaseNode_WithRequiredNamespaces_ReturnsCorrectNamespaces()
        {
            // Arrange
            var nodes = new Stack<SyntaxNode>();
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("else")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("then1")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));
            
            var whenThenPairs = new (Node When, Node Then)[] { (null, null) };
            var caseNode = new CaseNode(whenThenPairs, null, typeof(string));
            
            var typesToInstantiate = new Dictionary<string, Type>();
            var caseWhenMethodIndex = 1;
            
            // Act
            var result = CaseNodeProcessor.ProcessCaseNode(
                caseNode, nodes, typesToInstantiate, MethodAccessType.ResultQuery, "test", ref caseWhenMethodIndex);
            
            // Assert
            Assert.IsNotNull(result.RequiredNamespaces);
            Assert.AreEqual(2, result.RequiredNamespaces.Length);
            Assert.IsTrue(result.RequiredNamespaces.Contains(typeof(string).Namespace));
            Assert.IsTrue(result.RequiredNamespaces.Contains(typeof(IObjectResolver).Namespace));
        }

        [TestMethod]
        public void ProcessCaseNode_WithNullNode_ThrowsArgumentNullException()
        {
            // Arrange
            var nodes = new Stack<SyntaxNode>();
            var typesToInstantiate = new Dictionary<string, Type>();
            var caseWhenMethodIndex = 1;
            
            // Act
            Assert.Throws<ArgumentNullException>(() => CaseNodeProcessor.ProcessCaseNode(null, nodes, typesToInstantiate, MethodAccessType.ResultQuery, "test", ref caseWhenMethodIndex));
        }

        [TestMethod]
        public void ProcessCaseNode_WithNullNodes_ThrowsArgumentNullException()
        {
            // Arrange
            var whenThenPairs = new (Node When, Node Then)[] { (null, null) };
            var caseNode = new CaseNode(whenThenPairs, null, typeof(string));
            var typesToInstantiate = new Dictionary<string, Type>();
            var caseWhenMethodIndex = 1;
            
            // Act
            Assert.Throws<ArgumentNullException>(() => CaseNodeProcessor.ProcessCaseNode(caseNode, null, typesToInstantiate, MethodAccessType.ResultQuery, "test", ref caseWhenMethodIndex));
        }

        [TestMethod]
        public void ProcessCaseNode_WithNullTypesToInstantiate_ThrowsArgumentNullException()
        {
            // Arrange
            var nodes = new Stack<SyntaxNode>();
            var whenThenPairs = new (Node When, Node Then)[] { (null, null) };
            var caseNode = new CaseNode(whenThenPairs, null, typeof(string));
            var caseWhenMethodIndex = 1;
            
            // Act
            Assert.Throws<ArgumentNullException>(() => CaseNodeProcessor.ProcessCaseNode(caseNode, nodes, null, MethodAccessType.ResultQuery, "test", ref caseWhenMethodIndex));
        }

        [TestMethod]
        public void ProcessCaseNode_WithComplexNestedStructure_GeneratesCorrectSyntax()
        {
            // Arrange
            var nodes = new Stack<SyntaxNode>();
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("default")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("result3")));
            nodes.Push(SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                SyntaxFactory.IdentifierName("x"), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(3))));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("result2")));
            nodes.Push(SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                SyntaxFactory.IdentifierName("x"), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(2))));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("result1")));
            nodes.Push(SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                SyntaxFactory.IdentifierName("x"), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1))));
            
            var whenThenPairs = new (Node When, Node Then)[] { (null, null), (null, null), (null, null) };
            var caseNode = new CaseNode(whenThenPairs, null, typeof(string));
            
            var typesToInstantiate = new Dictionary<string, Type>();
            var caseWhenMethodIndex = 5;
            
            // Act
            var result = CaseNodeProcessor.ProcessCaseNode(
                caseNode, nodes, typesToInstantiate, MethodAccessType.ResultQuery, "test", ref caseWhenMethodIndex);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Method);
            Assert.AreEqual("CaseWhen_5", result.Method.Identifier.ValueText);
            Assert.AreEqual(6, caseWhenMethodIndex);
            
            // Verify the method body contains nested if-else structure
            var methodBody = result.Method.Body.Statements.First() as IfStatementSyntax;
            Assert.IsNotNull(methodBody);
            Assert.IsNotNull(methodBody.Else);
        }

        [TestMethod]
        public void ProcessCaseNode_MethodAccessTypeDefault_UsesEmptyRowVariableName()
        {
            // Arrange
            var nodes = new Stack<SyntaxNode>();
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("else")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("then1")));
            nodes.Push(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));
            
            var whenThenPairs = new (Node When, Node Then)[] { (null, null) };
            var caseNode = new CaseNode(whenThenPairs, null, typeof(string));
            
            var typesToInstantiate = new Dictionary<string, Type>();
            var caseWhenMethodIndex = 1;
            
            // Act
            var result = CaseNodeProcessor.ProcessCaseNode(
                caseNode, nodes, typesToInstantiate, (MethodAccessType)999, "test", ref caseWhenMethodIndex);
            
            // Assert
            Assert.IsNotNull(result.MethodInvocation);
            var argument = result.MethodInvocation.ArgumentList.Arguments.First();
            var identifier = argument.Expression as IdentifierNameSyntax;
            Assert.IsNotNull(identifier);
            Assert.AreEqual("", identifier.Identifier.ValueText);
        }
    }
}
