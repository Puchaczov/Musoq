using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Lexing;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BuildMetadataAndInferTypesVisitorTests
{
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
                {"d1", ["Key", "Value"]},
                {"e1", ["Key", "Value"]}
            },
            new Dictionary<uint, IEnumerable<EnvironmentVariableEntity>>
            {
                {0, Array.Empty<EnvironmentVariableEntity>()},
                {1, [new ("KEY_1", "VALUE_1")]}
            }, logger.Object);
        
        var traverser = new BuildMetadataAndInferTypesTraverseVisitor(visitor);
        
        tree.Accept(traverser);

        var positionalEnvironmentVariables = visitor.PositionalEnvironmentVariables;
        
        Assert.AreEqual(2, positionalEnvironmentVariables.Count);
        
        Assert.AreEqual(0, positionalEnvironmentVariables[0].Count);
        Assert.AreEqual(1, positionalEnvironmentVariables[1].Count);
        
        Assert.AreEqual("VALUE_1", positionalEnvironmentVariables[1]["KEY_1"]);
        
        Assert.AreEqual(1, visitor.PassedSchemaArguments.Count);
        
        Assert.AreEqual(typeof(string), visitor.PassedSchemaArguments[0]);
    }
}