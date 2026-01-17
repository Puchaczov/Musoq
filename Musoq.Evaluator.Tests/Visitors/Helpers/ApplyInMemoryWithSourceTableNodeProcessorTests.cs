using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class ApplyInMemoryWithSourceTableNodeProcessorTests
{
    [TestMethod]
    public void ProcessApplyInMemoryWithSourceTable_NullNode_ThrowsArgumentNullException()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var scope = new Scope(null, 1, "test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ApplyInMemoryWithSourceTableNodeProcessor.ProcessApplyInMemoryWithSourceTable(
                null, generator, scope, "testQuery",
                alias => null, statements => null, () => null));
    }

    [TestMethod]
    public void ProcessApplyInMemoryWithSourceTable_NullGenerator_ThrowsArgumentNullException()
    {
        // Arrange
        var sourceTable = new InMemoryTableFromNode("sourceTable", "alias1", typeof(object));
        var node = new ApplyInMemoryWithSourceTableFromNode("memTable", sourceTable, ApplyType.Cross, typeof(object));
        var scope = new Scope(null, 1, "test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ApplyInMemoryWithSourceTableNodeProcessor.ProcessApplyInMemoryWithSourceTable(
                node, null, scope, "testQuery",
                alias => null, statements => null, () => null));
    }

    [TestMethod]
    public void ProcessApplyInMemoryWithSourceTable_EmptyQueryAlias_ThrowsArgumentException()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var sourceTable = new InMemoryTableFromNode("sourceTable", "alias1", typeof(object));
        var node = new ApplyInMemoryWithSourceTableFromNode("memTable", sourceTable, ApplyType.Cross, typeof(object));
        var scope = new Scope(null, 1, "test");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ApplyInMemoryWithSourceTableNodeProcessor.ProcessApplyInMemoryWithSourceTable(
                node, generator, scope, "",
                alias => null, statements => null, () => null));
    }

    [TestMethod]
    public void ProcessApplyInMemoryWithSourceTable_UnsupportedApplyType_ThrowsArgumentException()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var sourceTable = new InMemoryTableFromNode("sourceTable", "alias1", typeof(object));
        var node = new ApplyInMemoryWithSourceTableFromNode("memTable", sourceTable, (ApplyType)999, typeof(object));
        var scope = new Scope(null, 1, "test");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ApplyInMemoryWithSourceTableNodeProcessor.ProcessApplyInMemoryWithSourceTable(
                node, generator, scope, "testQuery",
                alias => null, statements => null, () => null));
    }
}