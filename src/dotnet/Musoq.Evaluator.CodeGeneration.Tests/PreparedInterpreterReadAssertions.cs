using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

internal static class PreparedInterpreterReadAssertions
{
    internal static void LiteralSize(string code, string method, int expected)
    {
        var syntax = CSharpSyntaxTree.ParseText(code).GetRoot();
        var read = syntax
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(call =>
                (
                    call.Expression switch
                    {
                        IdentifierNameSyntax name => name.Identifier.ValueText,
                        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                        _ => null,
                    }
                ) == method
            );
        var size = Assert.IsInstanceOfType<IdentifierNameSyntax>(read.ArgumentList.Arguments[1].Expression);
        var binding = syntax
            .DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Single(variable => variable.Identifier.ValueText == size.Identifier.ValueText);
        Assert.AreEqual(
            expected,
            Assert.IsInstanceOfType<LiteralExpressionSyntax>(binding.Initializer!.Value).Token.Value
        );
        Assert.IsTrue(binding.Span.End < read.SpanStart, "The size must be prepared before the read.");
    }
}
