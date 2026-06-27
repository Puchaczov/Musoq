using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static InvocationExpressionSyntax CreateScalarToArrayWrapper(
        ExpressionSyntax sourceExpression,
        string typeName)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.GenericName(nameof(EvaluationHelper.WrapScalarForCrossApply))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ParseTypeName(typeName))))))
            .WithArgumentList(CreateArgumentList(sourceExpression));
    }

    private static InvocationExpressionSyntax WrapInTryCatchReturningNull(
        ExpressionSyntax expression,
        string typeName)
    {
        var tryBlock = SyntaxFactory.Block(SyntaxFactory.ReturnStatement(expression));
        var catchClause = SyntaxFactory.CatchClause()
            .WithBlock(SyntaxFactory.Block(
                SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))));
        var lambda = SyntaxFactory.ParenthesizedLambdaExpression()
            .WithBlock(SyntaxFactory.Block(SyntaxFactory.TryStatement()
                .WithBlock(tryBlock)
                .WithCatches(SyntaxFactory.SingletonList(catchClause))));
        var funcType = SyntaxFactory.GenericName("Func")
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                    SyntaxFactory.NullableType(SyntaxFactory.ParseTypeName(typeName)))));

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.ObjectCreationExpression(funcType)
                    .WithArgumentList(CreateArgumentList(lambda))));
    }

    private static string ResolveInterpretMethodName(InterpretSourceKind kind)
    {
        return kind switch
        {
            InterpretSourceKind.Interpret => "Interpret",
            InterpretSourceKind.InterpretAt => "InterpretAt",
            InterpretSourceKind.Parse => "Parse",
            InterpretSourceKind.TryInterpret => "Interpret",
            InterpretSourceKind.TryParse => "Parse",
            InterpretSourceKind.PartialInterpret => "PartialInterpret",
            InterpretSourceKind.PartialParse => "PartialParse",
            _ => throw UnsupportedShape.Of($"Interpret source kind {kind}")
        };
    }

    private static string CreateInterpreterVariableName(string schemaName)
    {
        var builder = new StringBuilder(schemaName.Length);

        foreach (var character in schemaName)
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');

        if (builder.Length == 0 || (!char.IsLetter(builder[0]) && builder[0] != '_'))
            builder.Insert(0, '_');

        return $"_interpreter_{builder}";
    }

    private static string CreateSourceScanLocalName(ExecutionSourceScan sourceScan)
    {
        const string rowsSuffix = "Rows";

        if (sourceScan.Rows.Name.EndsWith(rowsSuffix, StringComparison.Ordinal))
            return sourceScan.Rows.Name[..^rowsSuffix.Length];

        return sourceScan.Rows.Name;
    }
}
