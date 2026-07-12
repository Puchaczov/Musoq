using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Diagnostics;
using Musoq.Schema.Diagnostics;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static LocalDeclarationStatementSyntax CreateSourceProfileRecorderDeclaration(
        string sourceProfileName,
        string sourceName,
        bool useAdaptiveTiming)
    {
        var invocation = SyntaxFactory.ConditionalAccessExpression(
            SyntaxFactory.IdentifierName(ProfileRecorderVariableName),
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberBindingExpression(
                        SyntaxFactory.IdentifierName(useAdaptiveTiming
                            ? nameof(QueryProfileRecorder.CreateAdaptiveSourceRecorder)
                            : nameof(QueryProfileRecorder.CreateSourceRecorder))))
                .WithArgumentList(CreateArgumentList(CreateStringLiteral(sourceName))));

        return CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), sourceProfileName, invocation);
    }

    private static ConditionalExpressionSyntax CreateSourceDiagnosticsExpression(string sourceProfileName)
    {
        return SyntaxFactory.ConditionalExpression(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                SyntaxFactory.IdentifierName(sourceProfileName),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(nameof(SourceDiagnostics)),
                SyntaxFactory.IdentifierName(nameof(SourceDiagnostics.None))),
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(sourceProfileName),
                    SyntaxFactory.IdentifierName(nameof(SourceProfileRecorder.CreateDiagnostics)))));
    }

    private static ConditionalExpressionSyntax CreateProfiledRowsExpression(
        ExpressionSyntax rows,
        string sourceProfileName,
        TypeSyntax rowType)
    {
        return SyntaxFactory.ConditionalExpression(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                SyntaxFactory.IdentifierName(sourceProfileName),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            rows,
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.GenericName(nameof(ProfiledEnumerable<object>))
                            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(rowType))),
                        SyntaxFactory.IdentifierName(nameof(ProfiledEnumerable<object>.Create))))
                .WithArgumentList(CreateArgumentList(rows, SyntaxFactory.IdentifierName(sourceProfileName))));
    }

    private static ConditionalExpressionSyntax CreateProfiledChunksExpression(
        ExpressionSyntax chunks,
        string sourceProfileName,
        TypeSyntax rowType)
    {
        return SyntaxFactory.ConditionalExpression(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                SyntaxFactory.IdentifierName(sourceProfileName),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            chunks,
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.GenericName(nameof(ProfiledChunkedEnumerable<object>))
                            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(rowType))),
                        SyntaxFactory.IdentifierName(nameof(ProfiledChunkedEnumerable<object>.Create))))
                .WithArgumentList(CreateArgumentList(chunks, SyntaxFactory.IdentifierName(sourceProfileName))));
    }

    private static string CreateSourceProfileRecorderVariableName(string rowsVariableName)
    {
        return $"{rowsVariableName}Profile";
    }
}
