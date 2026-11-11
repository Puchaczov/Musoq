using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static ExpressionSyntax CreateComputedProjectionMetadata(ExecutionSourceComputedProjection projection)
        => CreateObjectCreation("SourceComputedProjection", CreateStringLiteral(projection.Name),
            CreateSourceScalarMetadata(projection.Expression), SyntaxFactory.TypeOfExpression(CreateTypeSyntax(projection.ResultType)));

    private static ExpressionSyntax CreateSourceScalarMetadata(ExecutionSourceScalar expression)
    {
        var type = SyntaxFactory.TypeOfExpression(CreateTypeSyntax(expression.ResultType));
        return expression switch
        {
            ExecutionSourceLiteral literal => CreateObjectCreation("SourceScalarLiteral", SourceLiteralMetadata(literal), type),
            ExecutionSourceColumn column => CreateObjectCreation("SourceScalarColumn", CreateStringLiteral(column.Name), type),
            ExecutionSourceUnary unary => CreateObjectCreation("SourceScalarUnary",
                SyntaxFactory.ParseExpression("SourceScalarUnaryOperator." + unary.Operator), CreateSourceScalarMetadata(unary.Operand), type),
            ExecutionSourceBinary binary => CreateSourceBinaryMetadata(binary),
            ExecutionSourceCast cast => CreateObjectCreation("SourceScalarCast", CreateSourceScalarMetadata(cast.Operand), type),
            ExecutionSourceNullCheck check => CreateObjectCreation("SourceScalarNullCheck", CreateSourceScalarMetadata(check.Operand), CreateBooleanLiteral(check.Negated)),
            ExecutionSourceCoalesce coalesce => CreateObjectCreation("SourceScalarCoalesce",
                CreateArrayCreation("SourceScalarExpression", coalesce.Expressions.Select(CreateSourceScalarMetadata).ToArray()), type),
            _ => throw new InvalidOperationException("Unsupported portable source scalar metadata.")
        };
    }

    private static ExpressionSyntax CreateSourceBinaryMetadata(ExecutionSourceBinary binary)
    {
        var creation = CreateObjectCreation("SourceScalarBinary",
            SyntaxFactory.ParseExpression("SourceScalarBinaryOperator." + binary.Operator), CreateSourceScalarMetadata(binary.Left),
            CreateSourceScalarMetadata(binary.Right), SyntaxFactory.TypeOfExpression(CreateTypeSyntax(binary.ResultType)));
        if (!binary.UsesSqlNullSemantics)
            return creation;
        return creation.WithInitializer(SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression,
            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("UsesSqlNullSemantics"), CreateBooleanLiteral(true)))));
    }

    private static ExpressionSyntax SourceLiteralMetadata(ExecutionSourceLiteral literal)
    {
        var value = literal.Value;
        var rendered = value.Kind switch
        {
            ExecutionConstantKind.DateTime => SyntaxFactory.ParseExpression(FormattableString.Invariant($"new System.DateTime({value.Ticks}L, System.DateTimeKind.{value.DateTimeKind})")),
            ExecutionConstantKind.DateTimeOffset => SyntaxFactory.ParseExpression(FormattableString.Invariant($"new System.DateTimeOffset({value.Ticks}L, System.TimeSpan.FromMinutes({value.OffsetMinutes}))")),
            ExecutionConstantKind.TimeSpan => SyntaxFactory.ParseExpression(FormattableString.Invariant($"new System.TimeSpan({value.Ticks}L)")),
            ExecutionConstantKind.Guid => SyntaxFactory.ParseExpression("new System.Guid(new byte[] { " +
                string.Join(",", value.GuidBytes) + " }, bigEndian: true)"),
            _ => RenderLiteral(value)
        };
        return value.Kind == ExecutionConstantKind.Null ? rendered : SyntaxFactory.CastExpression(
            CreateTypeSyntax(literal.ResultType), SyntaxFactory.ParenthesizedExpression(rendered));
    }
}
