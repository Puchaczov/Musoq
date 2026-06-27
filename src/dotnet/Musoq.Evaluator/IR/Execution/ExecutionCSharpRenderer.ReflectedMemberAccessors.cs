using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static ReflectedMemberAccessor[] CollectReflectedMemberAccessors(ExecutionPlan plan)
    {
        var sourceTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var ambiguousAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var shape in plan.Shapes.OfType<SourceEntityShape>())
        {
            if (!shape.Fields.Any(static field => field.AccessStrategy is ReflectedMemberAccess) ||
                ambiguousAliases.Contains(shape.Alias))
            {
                continue;
            }

            if (!sourceTypes.TryGetValue(shape.Alias, out var existingType))
            {
                sourceTypes.Add(shape.Alias, shape.EntityType);
                continue;
            }

            if (existingType == shape.EntityType)
                continue;

            sourceTypes.Remove(shape.Alias);
            ambiguousAliases.Add(shape.Alias);
        }

        if (sourceTypes.Count == 0)
            return [];

        var accessors = new Dictionary<string, ReflectedMemberAccessor>(StringComparer.Ordinal);
        foreach (var fieldRead in CollectFieldReads(plan.Body))
        {
            if (string.IsNullOrWhiteSpace(fieldRead.Alias) ||
                fieldRead.AccessStrategy is not ReflectedMemberAccess reflectedMember ||
                !sourceTypes.TryGetValue(fieldRead.Alias, out var sourceType))
            {
                continue;
            }

            var key = CreateReflectedMemberAccessorKey(fieldRead.Alias, reflectedMember.PropertyPath);
            if (accessors.ContainsKey(key))
                continue;

            accessors.Add(key, new ReflectedMemberAccessor(
                key,
                CreateReflectedMemberAccessorVariableName(fieldRead.Alias, reflectedMember.PropertyPath, accessors.Count),
                sourceType,
                reflectedMember.PropertyPath));
        }

        return accessors.Values.ToArray();
    }

    private static LocalDeclarationStatementSyntax CreateReflectedMemberAccessorDeclaration(ReflectedMemberAccessor accessor)
    {
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper.GetNestedValueAccessor))))
            .WithArgumentList(CreateArgumentList(
                CreateRequiredTypeExpression(accessor.SourceType),
                CreateStringLiteral(accessor.PropertyPath)));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            accessor.VariableName,
            invocation);
    }

    private static string CreateReflectedMemberAccessorKey(string alias, string propertyPath)
    {
        return $"{alias}\u001F{propertyPath}";
    }

    private static string CreateReflectedMemberAccessorVariableName(string alias, string propertyPath, int index)
    {
        var builder = new StringBuilder(alias.Length + propertyPath.Length + 24);
        builder.Append("__reflected_");
        AppendIdentifierFragment(builder, alias);
        builder.Append('_');
        AppendIdentifierFragment(builder, propertyPath);
        builder.Append('_');
        builder.Append(index.ToString(CultureInfo.InvariantCulture));
        return CreateIdentifierCandidate(builder.ToString(), 0);
    }

    private static void AppendIdentifierFragment(StringBuilder builder, string value)
    {
        foreach (var character in value)
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
    }
}
