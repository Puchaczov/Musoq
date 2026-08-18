using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Schema;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IEnumerable<MemberDeclarationSyntax> CreateQueryRowShapeFields(ExecutionBlock body)
    {
        return ExecutionIrAnalysis.FlattenNodes(body)
            .OfType<ExecutionSourceScan>()
            .Select(static scan => scan.Binding.QueryRowSourceTransfer)
            .Where(static transfer => transfer != null)
            .GroupBy(static transfer => transfer!.ShapeFingerprint, StringComparer.Ordinal)
            .Select(static group => RenderQueryRowShapeField(group.First()!));
    }

    private static MemberDeclarationSyntax RenderQueryRowShapeField(
        ExecutionQueryRowSourceTransfer transfer)
    {
        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(nameof(QueryRowShape)))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(EscapeIdentifier(
                                QueryRowSourceNaming.CreateShapeFieldName(transfer.ShapeFingerprint)))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                CreateQueryRowShapeExpression(transfer))))))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));
    }

    private static IEnumerable<MemberDeclarationSyntax> CreateQueryRowMaterializers(ExecutionBlock body)
    {
        return ExecutionIrAnalysis.FlattenNodes(body)
            .OfType<ExecutionSourceScan>()
            .Select(static scan => scan.Binding.QueryRowSourceTransfer)
            .Where(static transfer => transfer != null)
            .GroupBy(static transfer => (transfer!.ShapeFingerprint, transfer.Carrier))
            .Select(static group => RenderQueryRowMaterializer(group.First()!));
    }

    private static MemberDeclarationSyntax RenderQueryRowMaterializer(
        ExecutionQueryRowSourceTransfer transfer)
    {
        var carrierTypeName = QueryRowSourceNaming.CreateCarrierTypeName(transfer.ShapeFingerprint, transfer.Carrier);
        var materializerTypeName = QueryRowSourceNaming.CreateMaterializerTypeName(transfer.ShapeFingerprint, transfer.Carrier);
        var arguments = transfer.Fields
            .OrderBy(static field => field.Slot)
            .Select(field =>
                $"reader.Read<{EvaluationHelper.GetCastableType(field.FieldType.RequireClrType())}>({field.Slot})")
            .ToArray();
        var construction = arguments.Length == 0
            ? $"new {carrierTypeName}()"
            : $"new {carrierTypeName}({string.Join(", ", arguments)})";

        var source = new StringBuilder()
            .Append("private readonly struct ")
            .Append(materializerTypeName)
            .Append(" : IQueryRowMaterializer<")
            .Append(carrierTypeName)
            .Append(">")
            .AppendLine()
            .AppendLine("{")
            .Append("    public static ")
            .Append(carrierTypeName)
            .Append(" Materialize<TReader>(scoped ref TReader reader)")
            .AppendLine()
            .AppendLine("        where TReader : IQuerySourceFieldReader, allows ref struct")
            .Append("        => ")
            .Append(construction)
            .AppendLine(";")
            .AppendLine("}")
            .ToString();

        return SyntaxFactory.ParseMemberDeclaration(source)
               ?? throw new InvalidOperationException(
                   $"Could not render query-row materializer '{materializerTypeName}'.");
    }

    private static ExpressionSyntax CreateQueryRowShapeExpression(
        ExecutionQueryRowSourceTransfer transfer)
    {
        var fields = transfer.Fields
            .OrderBy(static field => field.Slot)
            .Select(CreateQueryRowFieldExpression)
            .ToArray();
        return CreateObjectCreation(
            nameof(QueryRowShape),
            CreateArrayCreation(nameof(QueryRowField), fields));
    }

    private static ExpressionSyntax CreateQueryRowFieldExpression(ExecutionQueryRowField field)
    {
        var arguments = new List<ExpressionSyntax>
        {
            CreateIntLiteral(field.Slot),
            CreateIntLiteral(field.SourceColumnIndex),
            CreateStringLiteral(field.Name),
            SyntaxFactory.TypeOfExpression(CreateTypeSyntax(field.FieldType)),
            CreateBooleanLiteral(field.IsNullable)
        };

        if (field.ReadModifiers.Count > 0)
            arguments.Add(CSharpReadModifierMetadata.CreateDictionaryCreation(field.ReadModifiers));

        return CreateObjectCreation(nameof(QueryRowField), arguments.ToArray());
    }
}
