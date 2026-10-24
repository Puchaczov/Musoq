using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax RenderUnionMemberRead(ExecutionUnionMemberRead member, ExecutionRenderContext context)
    {
        var receiver = RenderExpression(member.Receiver, context);
        var type = member.InterpretationShape?.TypeName ?? CreateTypeSyntax(member.ReturnType).ToString();
        var cases = member.Cases.Select(arm =>
            $"{arm.TypeName} __unionMember => ({type})__unionMember.{EscapeIdentifier(arm.MemberName)}");
        return SyntaxFactory.ParseExpression($"(({receiver}) switch {{ {string.Join(", ", cases)}, _ => ({type})null }})");
    }
}
