using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

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

    private static IndexerDeclarationSyntax CreateGeneratedRowIntIndexer(IReadOnlyList<FieldBinding> fields)
    {
        var arms = fields
            .Select((field, index) => SyntaxFactory.SwitchExpressionArm(
                SyntaxFactory.ConstantPattern(SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(index))),
                BoxExpression(CreateIdentifierName(GetGeneratedFieldName(field)))))
            .Concat(
            [
                SyntaxFactory.SwitchExpressionArm(
                    SyntaxFactory.DiscardPattern(),
                    SyntaxFactory.ThrowExpression(CreateObjectCreation(nameof(IndexOutOfRangeException))))
            ]);

        return SyntaxFactory.IndexerDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
            .WithParameterList(SyntaxFactory.BracketedParameterList(SyntaxFactory.SingletonSeparatedList(
                CreateParameter("columnNumber", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))))))
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.SwitchExpression(
                SyntaxFactory.IdentifierName("columnNumber"),
                SyntaxFactory.SeparatedList(arms))))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private static IndexerDeclarationSyntax CreateGeneratedRowStringIndexer(IReadOnlyList<FieldBinding> fields)
    {
        var arms = CreateGeneratedRowStringKeys(fields)
            .Select(key => SyntaxFactory.SwitchExpressionArm(
                SyntaxFactory.ConstantPattern(CreateStringLiteral(key.Key)),
                BoxExpression(CreateIdentifierName(GetGeneratedFieldName(fields[key.Index])))))
            .Concat(
            [
                SyntaxFactory.SwitchExpressionArm(
                    SyntaxFactory.DiscardPattern(),
                    SyntaxFactory.ThrowExpression(CreateObjectCreation(
                        nameof(KeyNotFoundException),
                        SyntaxFactory.IdentifierName("name"))))
            ]);

        return SyntaxFactory.IndexerDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
            .WithParameterList(SyntaxFactory.BracketedParameterList(SyntaxFactory.SingletonSeparatedList(
                CreateParameter("name", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))))))
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.SwitchExpression(
                SyntaxFactory.IdentifierName("name"),
                SyntaxFactory.SeparatedList(arms))))
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

    private static IndexerDeclarationSyntax CreateWideGeneratedRowStringIndexer()
    {
        return (IndexerDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration("""
            public override object this[string name] => __columnIndexes.TryGetValue(name, out var columnIndex)
                ? this[columnIndex]
                : throw new KeyNotFoundException(name);
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

    private static PropertyDeclarationSyntax CreateGeneratedRowContextsProperty(
        IReadOnlySet<GeneratedRowContextConstructor>? usedConstructors,
        int contextCount)
    {
        var allConstructors = GetGeneratedRowConstructors(usedConstructors);
        var contextsExpression = UsesDirectContextStorage(allConstructors)
            ? SyntaxFactory.IdentifierName("__contexts")
            : CreateGeneratedRowContextMaterialization(allConstructors.Single(), contextCount);

        return SyntaxFactory.PropertyDeclaration(SyntaxHelper.ObjectArrayTypeSyntax, nameof(Row.Contexts))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(contextsExpression))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private static MethodDeclarationSyntax CreateGeneratedRowAssignValueMethod(IReadOnlyList<FieldBinding> fields)
    {
        var sections = fields
            .Select((field, index) => SyntaxFactory.SwitchSection()
                .WithLabels(SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.CaseSwitchLabel(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(index)))))
                .WithStatements(SyntaxFactory.List<StatementSyntax>(
                [
                    SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        CreateIdentifierName(GetGeneratedFieldName(field)),
                        CastValueForAssignment(field.Type))),
                    SyntaxFactory.BreakStatement()
                ])))
            .Concat(
            [
                SyntaxFactory.SwitchSection()
                    .WithLabels(SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.DefaultSwitchLabel()))
                    .WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(
                        SyntaxFactory.ThrowStatement(CreateObjectCreation(nameof(IndexOutOfRangeException)))))
            ]);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                nameof(Row.AssignValue))
            .AddModifiers(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
            [
                CreateParameter("columnNumber", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))),
                CreateParameter("value", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)))
            ])))
            .WithBody(StatementEmitter.CreateBlock(SyntaxFactory.SwitchStatement(
                    SyntaxFactory.IdentifierName("columnNumber"))
                .WithSections(SyntaxFactory.List(sections))));
    }

    private static MemberDeclarationSyntax CreateWideGeneratedRowAssignersField(
        string rowTypeName,
        IReadOnlyList<FieldBinding> fields)
    {
        var assigners = fields.Select(field =>
        {
            var fieldName = EscapeIdentifier(GetGeneratedFieldName(field));
            var value = field.Type == typeof(object)
                ? "value"
                : $"({CreateTypeSyntax(field.Type).ToFullString()})value";
            return $"                static (row, value) => row.{fieldName} = {value}";
        });
        var code =
            $"private static readonly Action<{rowTypeName}, object>[] __assigners = new Action<{rowTypeName}, object>[]{Environment.NewLine}" +
            $"{{{Environment.NewLine}" +
            string.Join("," + Environment.NewLine, assigners) + Environment.NewLine +
            "};";

        return SyntaxFactory.ParseMemberDeclaration(code)!;
    }

    private static MethodDeclarationSyntax CreateWideGeneratedRowAssignValueMethod()
    {
        return (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration("""
            public override void AssignValue(int columnNumber, object value)
            {
                if ((uint)columnNumber >= (uint)__assigners.Length)
                    throw new IndexOutOfRangeException();
                __assigners[columnNumber](this, value);
            }
            """)!;
    }

    private static ExpressionSyntax CastValueForAssignment(Type targetType)
    {
        var value = SyntaxFactory.IdentifierName("value");
        return targetType == typeof(object)
            ? value
            : SyntaxFactory.CastExpression(CreateTypeSyntax(targetType), value);
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

    private static CastExpressionSyntax BoxExpression(ExpressionSyntax expression)
    {
        return SyntaxFactory.CastExpression(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
            expression);
    }
}
