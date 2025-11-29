using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class AccessMethodNodeProcessorTests
{
    [TestMethod]
    public void ProcessAccessMethodNode_WithNullNode_ShouldThrowArgumentNullException()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var nodes = new Stack<SyntaxNode>();
        var statements = new List<StatementSyntax>();
        var typesToInstantiate = new Dictionary<string, Type>();
        var nullSuspiciousNodes = new List<Stack<SyntaxNode>>();
        
        nodes.Push(SyntaxFactory.ArgumentList());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AccessMethodNodeProcessor.ProcessAccessMethodNode(
                null,
                generator,
                nodes,
                statements,
                typesToInstantiate,
                null, // scope
                0, // type
                false,
                nullSuspiciousNodes,
                _ => { }));
    }

    [TestMethod]
    public void ProcessAccessMethodNode_WithNullGenerator_ShouldThrowArgumentNullException()
    {
        // Arrange
        var nodes = new Stack<SyntaxNode>();
        var statements = new List<StatementSyntax>();
        var typesToInstantiate = new Dictionary<string, Type>();
        var nullSuspiciousNodes = new List<Stack<SyntaxNode>>();
        
        nodes.Push(SyntaxFactory.ArgumentList());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AccessMethodNodeProcessor.ProcessAccessMethodNode(
                null, // This should cause ArgumentNullException before generator
                null,
                nodes,
                statements,
                typesToInstantiate,
                null,
                0,
                false,
                nullSuspiciousNodes,
                _ => { }));
    }

    [TestMethod]
    public void ProcessAccessMethodNode_WithNullNodes_ShouldThrowArgumentNullException()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var statements = new List<StatementSyntax>();
        var typesToInstantiate = new Dictionary<string, Type>();
        var nullSuspiciousNodes = new List<Stack<SyntaxNode>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AccessMethodNodeProcessor.ProcessAccessMethodNode(
                null, // This should cause ArgumentNullException before nodes
                generator,
                null,
                statements,
                typesToInstantiate,
                null,
                0,
                false,
                nullSuspiciousNodes,
                _ => { }));
    }

    [TestMethod]
    public void ProcessAccessMethodNode_WithNullStatements_ShouldThrowArgumentNullException()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var nodes = new Stack<SyntaxNode>();
        var typesToInstantiate = new Dictionary<string, Type>();
        var nullSuspiciousNodes = new List<Stack<SyntaxNode>>();
        
        nodes.Push(SyntaxFactory.ArgumentList());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AccessMethodNodeProcessor.ProcessAccessMethodNode(
                null, // This should cause ArgumentNullException before statements
                generator,
                nodes,
                null,
                typesToInstantiate,
                null,
                0,
                false,
                nullSuspiciousNodes,
                _ => { }));
    }

    [TestMethod]
    public void ProcessAccessMethodNode_WithNullTypesToInstantiate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var nodes = new Stack<SyntaxNode>();
        var statements = new List<StatementSyntax>();
        var nullSuspiciousNodes = new List<Stack<SyntaxNode>>();
        
        nodes.Push(SyntaxFactory.ArgumentList());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AccessMethodNodeProcessor.ProcessAccessMethodNode(
                null, // This should cause ArgumentNullException before typesToInstantiate
                generator,
                nodes,
                statements,
                null,
                null,
                0,
                false,
                nullSuspiciousNodes,
                _ => { }));
    }

    [TestMethod]
    public void ProcessAccessMethodNode_WithNullAddNamespaceAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var nodes = new Stack<SyntaxNode>();
        var statements = new List<StatementSyntax>();
        var typesToInstantiate = new Dictionary<string, Type>();
        var nullSuspiciousNodes = new List<Stack<SyntaxNode>>();
        
        nodes.Push(SyntaxFactory.ArgumentList());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AccessMethodNodeProcessor.ProcessAccessMethodNode(
                null, // This should cause ArgumentNullException before addNamespaceAction
                generator,
                nodes,
                statements,
                typesToInstantiate,
                null,
                0,
                false,
                nullSuspiciousNodes,
                null));
    }
}
