using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class StaticSourceArgumentTests
{
    [TestMethod]
    public void ComputedDefaults_ShouldRespectParameterCaseAndLetScope()
    {
        var parameters = new Dictionary<string, ScriptParameterDefinition>(StringComparer.Ordinal)
        {
            ["root"] = new("root", typeof(string), true, "lower"),
            ["Root"] = new("Root", typeof(string), true, "UPPER")
        };
        var variables = new Dictionary<string, ScriptVariableDefinition>
        {
            ["suffix"] = new("suffix", typeof(string), "/file", true)
        };
        var expression = new AddNode(new AddNode(new ParameterReferenceNode("root", typeof(string)),
            new ParameterReferenceNode("Root", typeof(string))), new ScriptVariableReferenceNode("suffix", typeof(string)));
        var values = SchemaArgumentBinder.BindStaticArguments(new ArgsListNode([expression]), parameters, variables);
        CollectionAssert.AreEqual(new object?[] { "lowerUPPER/file" }, values);
        Assert.HasCount(1, variables, "Metadata binding must not mutate caller-owned LET definitions.");
    }

    [TestMethod]
    public void ComputedRequiredParameter_ShouldStopTheKnownPrefix()
    {
        var parameters = new Dictionary<string, ScriptParameterDefinition>
        {
            ["root"] = new("root", typeof(string), false, null)
        };
        var args = new ArgsListNode([new StringNode("known"),
            new AddNode(new ParameterReferenceNode("root", typeof(string)), new StringNode("/file")), new IntegerNode("42")]);
        CollectionAssert.AreEqual(new object?[] { "known" }, SchemaArgumentBinder.BindStaticArguments(args, parameters));
    }

    [TestMethod]
    public void SafeConstantExpression_ShouldNotRequireLetDeclarations()
    {
        var args = new ArgsListNode([new AddNode(new IntegerNode("40", string.Empty), new IntegerNode("2", string.Empty)), new NullNode()]);
        CollectionAssert.AreEqual(new object?[] { 42, null }, SchemaArgumentBinder.BindStaticArguments(args));
    }

    [TestMethod]
    public void PluginCalls_ShouldRemainOutsideTheSafeStaticSubset()
    {
        var call = new AccessMethodNode(new FunctionToken("MustNeverRun", TextSpan.Empty),
            new ArgsListNode([new StringNode("value")]), null, false);
        Assert.IsEmpty(SchemaArgumentBinder.BindStaticArguments(new ArgsListNode([call, new IntegerNode("42")])));
    }
}
