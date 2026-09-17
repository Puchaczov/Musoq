using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Targets.CSharpClr;

/// <summary>
/// Creates direct typed loops for structural CTE arguments.  The generated
/// methods deliberately have no conversion delegate: the planner-selected row
/// shape is read by member and the receiving constructor is called directly.
/// </summary>
internal static class StructuralCtePreparationMembers
{
    private const string HelperPrefix = "__musoqPrepareCte_";

    internal static string GetHelperName(ExecutionCteCollectionInput input)
    {
        var signature = new StringBuilder(input.CteName)
            .Append('|')
            .Append(input.Rows is ExecutionStoredTableRows storedRows ? storedRows.TableIndex : -1)
            .Append('|')
            .Append(input.ElementType.StableId);
        foreach (var field in input.Fields)
            signature.Append('|').Append(field.Name).Append(':').Append(field.Index).Append(':').Append(field.Type.StableId);

        signature
            .Append("|ownership=").Append(input.Ownership)
            .Append("|lifetime=").Append(input.Lifetime)
            .Append("|metrics=").Append(input.MetricsStrategy);

        if (input.LimitBinding is { } binding)
        {
            signature
                .Append("|limits=").Append(binding.Limits)
                .Append('|').Append(binding.Origin)
                .Append('|').Append(binding.Path)
                .Append('|').Append(binding.SourceContextId ?? "-");
        }

        if (input.ConstructionPlan is { } plan)
        {
            signature.Append('|').Append(plan.TargetType.StableId).Append('|').Append(plan.Constructor.StableId);
            foreach (var index in plan.SourceFieldIndexes)
                signature.Append('|').Append(index);
            foreach (var @default in plan.Defaults)
                signature.Append('|').Append(@default?.ToString() ?? "<absent>");
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(signature.ToString()));
        return HelperPrefix + Convert.ToHexString(digest.AsSpan(0, 8)).ToLowerInvariant();
    }

    internal static IReadOnlyList<MemberDeclarationSyntax> CreateMembers(
        ExecutionBlock body,
        ExecutionRenderContext context,
        ExecutionCSharpRenderer renderer)
    {
        var inputs = ExecutionIrAnalysis.CollectExpressions<ExecutionCteCollectionInput>(body)
            .Distinct()
            .ToArray();
        return inputs
            .GroupBy(GetHelperName, StringComparer.Ordinal)
            .Select(group => CreateMember(group.First(), group.Key, context, renderer))
            .ToArray();
    }

    private static MemberDeclarationSyntax CreateMember(
        ExecutionCteCollectionInput input,
        string helperName,
        ExecutionRenderContext context,
        ExecutionCSharpRenderer renderer)
    {
        var sourceShape = ResolveSourceShape(input);
        var sourceType = SyntaxFactory.ParseTypeName(sourceShape.TypeName);
        var targetType = ExecutionSyntaxFactory.CreateTypeSyntax(input.ElementType);
        var targetText = targetType.ToFullString();
        var body = CreateBody(input, sourceShape, targetText, context, renderer);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName($"{targetText}[]"),
                helperName)
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("rows"))
                    .WithType(SyntaxFactory.ParseTypeName($"IReadOnlyList<{sourceShape.TypeName}>?")),
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("token"))
                    .WithType(SyntaxFactory.ParseTypeName("CancellationToken"))
            ])))
            .WithBody(body);
    }

    private static BlockSyntax CreateBody(
        ExecutionCteCollectionInput input,
        GeneratedRowShape sourceShape,
        string targetText,
        ExecutionRenderContext context,
        ExecutionCSharpRenderer renderer)
    {
        var statements = new List<StatementSyntax>();
        if (input.LimitBinding is { } binding)
            statements.Add(CreateMetricsStatement(input, sourceShape, binding, context, renderer));

        statements.Add(SyntaxFactory.ParseStatement(
            $"if (rows is null || rows.Count == 0) return System.Array.Empty<{targetText}>();"));
        statements.Add(SyntaxFactory.ParseStatement("token.ThrowIfCancellationRequested();"));

        if (input.Ownership == ExecutionStructuralOwnershipMode.Transfer &&
            string.Equals(targetText.Trim(), sourceShape.TypeName, StringComparison.Ordinal))
        {
            statements.Add(SyntaxFactory.ParseStatement(
                $"if (rows is {targetText}[] transferred) return transferred;"));
        }

        statements.Add(SyntaxFactory.ParseStatement($"var result = new {targetText}[rows.Count];"));

        var construction = StructuralInputSyntaxFactory.CreateCteElementConstruction(
            input,
            sourceShape,
            "row",
            context,
            renderer);
        var assignment = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateResultElementAccess(),
                construction));
        var loop = SyntaxFactory.ForStatement(
                SyntaxFactory.Block(
                    SyntaxFactory.ParseStatement("token.ThrowIfCancellationRequested();"),
                    SyntaxFactory.ParseStatement("var row = rows[index];"),
                    assignment))
            .WithDeclaration(SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("index"))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))))))
            .WithCondition(SyntaxFactory.BinaryExpression(
                SyntaxKind.LessThanExpression,
                SyntaxFactory.IdentifierName("index"),
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("rows"),
                    SyntaxFactory.IdentifierName("Count"))))
            .WithIncrementors(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                SyntaxFactory.PostfixUnaryExpression(
                    SyntaxKind.PostIncrementExpression,
                    SyntaxFactory.IdentifierName("index"))));

        statements.Add(loop);
        statements.Add(SyntaxFactory.ParseStatement("return result;"));
        return SyntaxFactory.Block(statements);
    }

    private static ElementAccessExpressionSyntax CreateResultElementAccess()
    {
        return SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName("result"))
            .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("index")))));
    }

    private static GeneratedRowShape ResolveSourceShape(
        ExecutionCteCollectionInput input)
    {
        if (input.Rows is ExecutionStoredTableRows { GeneratedRowShape: { } shape })
            return shape;

        throw new InvalidOperationException(
            "Structural CTE argument requires planner-provided generated row shape.");
    }

    private static StatementSyntax CreateMetricsStatement(
        ExecutionCteCollectionInput input,
        GeneratedRowShape sourceShape,
        ExecutionStructuralLimitBinding binding,
        ExecutionRenderContext context,
        ExecutionCSharpRenderer renderer)
    {
        var lines = new List<string>
        {
            "token.ThrowIfCancellationRequested();",
            "long __musoqStructuralNodes = 0L;",
            "long __musoqStructuralStrings = 0L;",
            "int __musoqStructuralMaxDepth = 0;"
        };
        var state = new StructuralInputLimitSyntaxFactory.EmitterState(lines);
        state.AddNode(1);

        var index = state.NextName("cteIndex");
        var row = state.NextName("cteRow");
        lines.Add("if (rows is not null)");
        lines.Add("{");
        lines.Add($"for (var {index} = 0; {index} < rows.Count; {index}++)");
        lines.Add("{");
        lines.Add($"if (({index} & 1023) == 0) token.ThrowIfCancellationRequested();");
        lines.Add($"var {row} = rows[{index}];");
        EmitElementMeasure(input, sourceShape, row, state, context, renderer);
        lines.Add("}");
        lines.Add("}");

        var sourceContext = binding.SourceContextId == null ? "null" : Quote(binding.SourceContextId);
        lines.Add(
            $"global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded(" +
            $"{Quote(binding.Origin.ToString().ToLowerInvariant())}, {Quote(binding.Path)}, " +
            "new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(" +
            "__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), " +
            $"{binding.Limits.MaxDepth.ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
            $"{binding.Limits.MaxNodes.ToString(System.Globalization.CultureInfo.InvariantCulture)}L, " +
            $"{binding.Limits.MaxStringBytes.ToString(System.Globalization.CultureInfo.InvariantCulture)}L, token, {sourceContext});");

        return SyntaxFactory.ParseStatement("{\n" +
            string.Join(Environment.NewLine, lines) +
            "\n}");
    }

    private static void EmitElementMeasure(
        ExecutionCteCollectionInput input,
        GeneratedRowShape sourceShape,
        string rowExpression,
        StructuralInputLimitSyntaxFactory.EmitterState state,
        ExecutionRenderContext context,
        ExecutionCSharpRenderer renderer)
    {
        var elementType = input.ElementType.RequireClrType();
        var descriptor = StructuralTypeDescriptor.FromClrType(elementType);
        if (descriptor.Kind != StructuralTypeKind.Record || input.ConstructionPlan is not { } plan)
        {
            if (input.Fields.Count != 1)
                throw new InvalidOperationException("A primitive or collection CTE argument requires exactly one output field.");

            var expression = StructuralInputSyntaxFactory.RenderCteFieldRead(
                sourceShape,
                input.Fields[0],
                rowExpression,
                elementType).ToFullString();
            StructuralInputLimitSyntaxFactory.EmitMeasure(
                descriptor,
                elementType,
                expression,
                2,
                state,
                "cte[]");
            return;
        }

        // The relation row itself is a logical record occurrence. Its fields
        // are measured in receiver/constructor order, while every expression
        // has already been evaluated by the CTE producer.
        state.AddNode(2);
        if (plan.SourceFieldIndexes.Count != descriptor.Fields.Count)
            throw new InvalidOperationException("CTE structural construction metadata is inconsistent.");

        for (var parameterIndex = 0; parameterIndex < descriptor.Fields.Count; parameterIndex++)
        {
            var field = descriptor.Fields[parameterIndex];
            var sourceFieldIndex = plan.SourceFieldIndexes[parameterIndex];
            var fieldType = StructuralInputLimitSyntaxFactory.GetClrType(field.Type);
            string expression;
            if (sourceFieldIndex >= 0)
            {
                if (sourceFieldIndex >= input.Fields.Count)
                    throw new InvalidOperationException("CTE structural construction metadata is inconsistent.");

                expression = StructuralInputSyntaxFactory.RenderCteFieldRead(
                    sourceShape,
                    input.Fields[sourceFieldIndex],
                    rowExpression,
                    fieldType).ToFullString();
            }
            else
            {
                var defaultExpression = plan.Defaults[parameterIndex];
                expression = defaultExpression == null
                    ? SyntaxFactory.DefaultExpression(ExecutionSyntaxFactory.CreateTypeSyntax(fieldType)).ToFullString()
                    : renderer.RenderExpression(defaultExpression, context).ToFullString();
            }

            StructuralInputLimitSyntaxFactory.EmitMeasure(
                field.Type,
                fieldType,
                expression,
                3,
                state,
                "cte[]." + field.Name);
        }
    }

    private static string Quote(string value) =>
        SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(value))
            .ToFullString();
}
