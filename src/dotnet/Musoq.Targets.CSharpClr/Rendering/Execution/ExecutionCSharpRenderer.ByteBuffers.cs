using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Plugins;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax RenderByteBufferSnapshot(ExecutionByteBufferSnapshot snapshot, ExecutionRenderContext context)
    {
        if (snapshot.Program is { } program)
            return RenderByteBufferProgram(program, context);
        var method = snapshot.EmptyResult == ExecutionBufferEmptyResult.Null
            ? nameof(ByteBufferSnapshot.CopyOrNull)
            : nameof(ByteBufferSnapshot.Copy);
        var arguments = snapshot.Arguments.Select(argument => RenderExpression(argument, context))
            .Append(RenderLiteral(snapshot.MaximumReadBytes ?? int.MaxValue)).ToArray();
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseExpression($"global::Musoq.Plugins.ByteBufferSnapshot.{method}"))
            .WithArgumentList(CreateArgumentList(arguments));
    }
}
