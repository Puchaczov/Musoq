using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax RenderStringConcatenation(ExecutionStringConcatenation concatenation,
        ExecutionRenderContext context) => SyntaxFactory.ParseExpression(context.Session.StringConcatenation.Emit(
            concatenation.Expressions.Select(expression => RenderExpression(expression, context).NormalizeWhitespace().ToFullString()).ToArray()));
}
