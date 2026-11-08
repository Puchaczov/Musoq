using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class InterpretationLimitGeneratedCodeTests
{
    [TestMethod]
    [DataRow("binary S { Records: byte repeat until eof }")]
    [DataRow("binary S { Records: byte repeat until Records[-1] = 0 }")]
    [DataRow("text S { Records: repeat chars[1] until end }")]
    public void SelectedLimit_ShouldBeAnIntegerLiteralInEveryRepeatGuard(string definition)
    {
        var registry = Registry(definition);
        var code = new InterpreterCodeGenerator(registry, new(37)).GenerateAll();
        var guards = CSharpSyntaxTree.ParseText(code).GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression is IdentifierNameSyntax { Identifier.ValueText: "EnsureRepeatIteration" }).ToArray();

        Assert.HasCount(1, guards);
        Assert.HasCount(3, guards[0].ArgumentList.Arguments);
        Assert.IsInstanceOfType<LiteralExpressionSyntax>(guards[0].ArgumentList.Arguments[2].Expression);
        Assert.AreEqual(37, ((LiteralExpressionSyntax)guards[0].ArgumentList.Arguments[2].Expression).Token.Value);
        Assert.DoesNotContain("CompilationOptions", code);
        Assert.DoesNotContain("InterpretationLimits", code);
    }

    [TestMethod]
    public void LegacyGeneratorConstructor_ShouldKeepTheDefaultGeneratedGuard()
    {
        var registry = Registry("binary S { Records: byte repeat until eof }");

        Assert.AreEqual(new InterpreterCodeGenerator(registry).GenerateAll(),
            new InterpreterCodeGenerator(registry, new()).GenerateAll());
    }

    [TestMethod]
    public void NestedInterpreters_ShouldCompileTheSameLimitIntoEveryRepeat()
    {
        var registry = Registry("binary Inner { Values: byte repeat until eof }",
            "binary S { Records: Inner repeat until eof }");
        var code = new InterpreterCodeGenerator(registry, new(37)).GenerateAll();
        var guards = CSharpSyntaxTree.ParseText(code).GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression is IdentifierNameSyntax { Identifier.ValueText: "EnsureRepeatIteration" }).ToArray();

        Assert.HasCount(2, guards);
        Assert.IsTrue(guards.All(call => call.ArgumentList.Arguments[2].Expression is LiteralExpressionSyntax literal &&
            Equals(37, literal.Token.Value)));
    }

    private static SchemaRegistry Registry(params string[] definitions)
    {
        var registry = new SchemaRegistry();
        foreach (var definition in definitions)
        {
            var node = new SchemaParser(new Lexer(definition, true)).ParseSchema();
            var name = node switch { BinarySchemaNode binary => binary.Name, TextSchemaNode text => text.Name,
                _ => throw new InvalidOperationException("Expected an interpretation schema.") };
            registry.Register(name, node);
        }
        return registry;
    }
}
