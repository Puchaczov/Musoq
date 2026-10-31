using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static PropertyDeclarationSyntax CreateGeneratedRowCountProperty(int fieldCount)
    {
        return SyntaxFactory.PropertyDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)), nameof(Row.Count))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(CreateIntLiteral(fieldCount)))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }





    private static bool RequiresWideGeneratedRowColumnMap(IReadOnlyList<FieldBinding> fields)
    {
        return fields.Count >= WideGeneratedRowColumnMapThreshold;
    }

    private static IEnumerable<MemberDeclarationSyntax> CreateWideGeneratedRowColumnMapMembers(
        IReadOnlyList<FieldBinding> fields)
    {
        var encodedPairs = string.Join(
            "\n",
            CreateGeneratedRowStringKeys(fields)
                .SelectMany(static key => new[]
                {
                    key.Key,
                    key.Index.ToString(System.Globalization.CultureInfo.InvariantCulture)
                }));

        yield return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator("__columnIndexPairs")
                            .WithInitializer(SyntaxFactory.EqualsValueClause(CreateStringLiteral(encodedPairs))))))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.ConstKeyword));

        yield return SyntaxFactory.ParseMemberDeclaration(
            "private static readonly Dictionary<string, int> __columnIndexes = CreateColumnIndexes();")!;

        yield return SyntaxFactory.ParseMemberDeclaration("""
            private static Dictionary<string, int> CreateColumnIndexes()
            {
                var pairs = __columnIndexPairs.Split('\n');
                var indexes = new Dictionary<string, int>(pairs.Length / 2, StringComparer.Ordinal);
                for (var index = 0; index < pairs.Length; index += 2)
                    indexes.Add(pairs[index], int.Parse(pairs[index + 1], System.Globalization.CultureInfo.InvariantCulture));

                return indexes;
            }
            """)!;
    }



    private static MethodDeclarationSyntax CreateWideGeneratedRowHasColumnMethod()
    {
        return (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration("""
            public override bool HasColumn(string name) => __columnIndexes.ContainsKey(name);
            """)!;
    }

    private static MethodDeclarationSyntax CreateGeneratedRowHasColumnMethod(IReadOnlyList<FieldBinding> fields)
    {
        var arms = CreateGeneratedRowStringKeys(fields)
            .Select(key => SyntaxFactory.SwitchExpressionArm(
                SyntaxFactory.ConstantPattern(CreateStringLiteral(key.Key)),
                CreateBooleanLiteral(true)))
            .Concat(
            [
                SyntaxFactory.SwitchExpressionArm(
                    SyntaxFactory.DiscardPattern(),
                    CreateBooleanLiteral(false))
            ]);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                nameof(Row.HasColumn))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                CreateParameter("name", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))))))
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.SwitchExpression(
                SyntaxFactory.IdentifierName("name"),
                SyntaxFactory.SeparatedList(arms))))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private static IEnumerable<(string Key, int Index)> CreateGeneratedRowStringKeys(
        IReadOnlyList<FieldBinding> fields)
    {
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var unqualifiedNameCounts = fields
            .Select(static field => ExtractUnqualifiedColumnName(field.Name))
            .Where(static name => name != null)
            .GroupBy(static name => name!, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);

        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            if (seenKeys.Add(field.Name))
                yield return (field.Name, index);

            var generatedFieldName = GetGeneratedFieldName(field);
            if (seenKeys.Add(generatedFieldName))
                yield return (generatedFieldName, index);

            var unqualifiedName = ExtractUnqualifiedColumnName(field.Name);
            if (unqualifiedName != null &&
                unqualifiedNameCounts.TryGetValue(unqualifiedName, out var count) &&
                count == 1 &&
                seenKeys.Add(unqualifiedName))
                yield return (unqualifiedName, index);
        }
    }

    private static string? ExtractUnqualifiedColumnName(string outputName)
    {
        var separatorIndex = outputName.LastIndexOf('.');
        if (separatorIndex < 0 || separatorIndex >= outputName.Length - 1)
            return null;

        return outputName[(separatorIndex + 1)..];
    }
}
