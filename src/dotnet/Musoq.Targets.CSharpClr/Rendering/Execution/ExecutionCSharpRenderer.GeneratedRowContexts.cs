using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IEnumerable<MemberDeclarationSyntax> CreateGeneratedRowContextFields(
        IReadOnlySet<GeneratedRowContextConstructor>? usedConstructors,
        IReadOnlyList<FieldBinding> contexts)
    {
        var constructors = GetGeneratedRowConstructors(usedConstructors);
        if (!RequiresGeneratedRowContextOverride(constructors))
            yield break;

        var valueConstructors = constructors.Where(static constructor =>
            constructor != GeneratedRowContextConstructor.NoContext).ToArray();
        if (valueConstructors.Length != 1)
            throw UnsupportedShape.Of("A context carrier must have one concrete field layout.");
        var constructor = valueConstructors[0];

        foreach (var contextParameter in CreateGeneratedRowContextParameters(constructor, contexts)
                     .OrderBy(static contextParameter => GetGeneratedRowContextFieldOrder(contextParameter.FieldName)))
            yield return CreateGeneratedRowField(contextParameter.Type, contextParameter.FieldName, isReadonly: true);
    }

    private static FieldDeclarationSyntax CreateGeneratedRowField(
        TypeSyntax type,
        string name,
        bool isReadonly)
    {
        var declaration = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(type)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(name))))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        return isReadonly
            ? declaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
            : declaration;
    }

    private static bool RequiresGeneratedRowContextOverride(
        IReadOnlySet<GeneratedRowContextConstructor>? usedConstructors)
    {
        return RequiresGeneratedRowContextOverride(GetGeneratedRowConstructors(usedConstructors));
    }

    private static bool RequiresGeneratedRowContextOverride(
        IReadOnlyList<GeneratedRowContextConstructor> constructors)
    {
        return constructors.Any(static constructor => constructor != GeneratedRowContextConstructor.NoContext);
    }

    private static IReadOnlyList<GeneratedRowContextParameter> CreateGeneratedRowContextParameters(
        GeneratedRowContextConstructor constructor,
        IReadOnlyList<FieldBinding> contexts)
    {
        return constructor switch
        {
            GeneratedRowContextConstructor.NoContext => [],
            GeneratedRowContextConstructor.SingleContext => [new("__context", "__leftContext", CreateContextTypeSyntax(contexts, 0))],
            GeneratedRowContextConstructor.SingleContexts => CreateSingleContextParameters(contexts),
            GeneratedRowContextConstructor.TwoSingleContexts =>
            [
                new("__leftContext", "__leftContext", CreateContextTypeSyntax(contexts, 0)),
                new("__rightContext", "__rightContext", CreateContextTypeSyntax(contexts, contexts.Count - 1))
            ],
            _ => throw UnsupportedShape.Of($"Generated row constructor {constructor}")
        };
    }

    private static GeneratedRowContextParameter[] CreateSingleContextParameters(
        IReadOnlyList<FieldBinding> contexts)
    {
        return Enumerable
            .Range(0, contexts.Count)
            .Select(index =>
            {
                var name = CreateSingleContextFieldName(index);
                return new GeneratedRowContextParameter(name, name, CreateGeneratedFieldTypeSyntax(contexts[index]));
            })
            .ToArray();
    }

    private static TypeSyntax CreateContextTypeSyntax(
        IReadOnlyList<FieldBinding> contexts,
        int index)
    {
        if (index < 0 || index >= contexts.Count)
            throw UnsupportedShape.Of($"Context slot {index} has no concrete field binding.");

        return CreateGeneratedFieldTypeSyntax(contexts[index]);
    }

    private static string CreateSingleContextFieldName(int index)
    {
        return $"__context{index.ToString(CultureInfo.InvariantCulture)}";
    }

    private static int GetGeneratedRowContextFieldOrder(string fieldName)
    {
        if (fieldName.StartsWith("__context", StringComparison.Ordinal) &&
            int.TryParse(fieldName["__context".Length..], out var contextIndex))
        {
            return contextIndex;
        }

        return fieldName switch
        {
            "__leftContext" => 0,
            "__rightContext" => 1,
            "__leftContexts" => 2,
            "__rightContexts" => 3,
            "__leftContextsRow" => 4,
            "__rightContextsRow" => 5,
            _ => throw new InvalidOperationException($"Generated row context field {fieldName} is not supported.")
        };
    }

    private ExpressionSyntax RenderContextArray(ExecutionContextArray contextArray, ExecutionRenderContext context) =>
        throw UnsupportedShape.Of("Context arrays must be lowered to concrete fields before rendering.");
}
