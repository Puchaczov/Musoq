using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IEnumerable<MemberDeclarationSyntax> CreateGeneratedRowContextFields(
        IReadOnlySet<GeneratedRowContextConstructor>? usedConstructors,
        int contextCount)
    {
        var constructors = GetGeneratedRowConstructors(usedConstructors);
        if (!RequiresGeneratedRowContextOverride(constructors))
            yield break;

        if (UsesDirectContextStorage(constructors))
        {
            yield return CreateGeneratedRowField(SyntaxHelper.ObjectArrayTypeSyntax, "__contexts", isReadonly: true);
            yield break;
        }

        var constructor = constructors.Single();
        var objectType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));
        var rowType = CreateTypeSyntax(typeof(Row));

        foreach (var contextParameter in CreateGeneratedRowContextParameters(constructor, objectType, rowType, contextCount)
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

    private static bool UsesDirectContextStorage(
        IReadOnlyList<GeneratedRowContextConstructor> constructors)
    {
        return constructors.Count > 1 ||
               constructors.Contains(GeneratedRowContextConstructor.ContextArray);
    }

    private static IReadOnlyList<GeneratedRowContextParameter> CreateGeneratedRowContextParameters(
        GeneratedRowContextConstructor constructor,
        TypeSyntax objectType,
        TypeSyntax rowType,
        int contextCount)
    {
        return constructor switch
        {
            GeneratedRowContextConstructor.NoContext => [],
            GeneratedRowContextConstructor.ContextArray => [new("__contexts", "__contexts", SyntaxHelper.ObjectArrayTypeSyntax)],
            GeneratedRowContextConstructor.SingleContext => [new("__context", "__leftContext", objectType)],
            GeneratedRowContextConstructor.SingleContexts => CreateSingleContextParameters(objectType, contextCount),
            GeneratedRowContextConstructor.ContextRow => [new("__contextsRow", "__leftContextsRow", rowType)],
            GeneratedRowContextConstructor.TwoSingleContexts =>
            [
                new("__leftContext", "__leftContext", objectType),
                new("__rightContext", "__rightContext", objectType)
            ],
            GeneratedRowContextConstructor.TwoContextRows =>
            [
                new("__leftContextsRow", "__leftContextsRow", rowType),
                new("__rightContextsRow", "__rightContextsRow", rowType)
            ],
            GeneratedRowContextConstructor.TwoContextArrays =>
            [
                new("__leftContexts", "__leftContexts", SyntaxHelper.ObjectArrayTypeSyntax),
                new("__rightContexts", "__rightContexts", SyntaxHelper.ObjectArrayTypeSyntax)
            ],
            GeneratedRowContextConstructor.ContextArrayAndSingleContext =>
            [
                new("__leftContexts", "__leftContexts", SyntaxHelper.ObjectArrayTypeSyntax),
                new("__rightContext", "__rightContext", objectType)
            ],
            GeneratedRowContextConstructor.ContextRowAndSingleContext =>
            [
                new("__leftContextsRow", "__leftContextsRow", rowType),
                new("__rightContext", "__rightContext", objectType)
            ],
            GeneratedRowContextConstructor.SingleContextAndContextArray =>
            [
                new("__leftContext", "__leftContext", objectType),
                new("__rightContexts", "__rightContexts", SyntaxHelper.ObjectArrayTypeSyntax)
            ],
            GeneratedRowContextConstructor.SingleContextAndContextRow =>
            [
                new("__leftContext", "__leftContext", objectType),
                new("__rightContextsRow", "__rightContextsRow", rowType)
            ],
            GeneratedRowContextConstructor.ContextRowAndContextArray =>
            [
                new("__leftContextsRow", "__leftContextsRow", rowType),
                new("__rightContexts", "__rightContexts", SyntaxHelper.ObjectArrayTypeSyntax)
            ],
            GeneratedRowContextConstructor.ContextArrayAndContextRow =>
            [
                new("__leftContexts", "__leftContexts", SyntaxHelper.ObjectArrayTypeSyntax),
                new("__rightContextsRow", "__rightContextsRow", rowType)
            ],
            _ => throw UnsupportedShape.Of($"Generated row constructor {constructor}")
        };
    }

    private static GeneratedRowContextParameter[] CreateSingleContextParameters(
        TypeSyntax objectType,
        int contextCount)
    {
        return Enumerable
            .Range(0, contextCount)
            .Select(index =>
            {
                var name = CreateSingleContextFieldName(index);
                return new GeneratedRowContextParameter(name, name, objectType);
            })
            .ToArray();
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

    private static ExpressionSyntax CreateGeneratedRowContextMaterialization(
        GeneratedRowContextConstructor constructor,
        int contextCount)
    {
        return CreateGeneratedRowContextMaterialization(
            constructor,
            static fieldName => SyntaxFactory.IdentifierName(fieldName),
            contextCount);
    }

    private static ExpressionSyntax CreateGeneratedRowContextMaterialization(
        GeneratedRowContextConstructor constructor,
        Func<string, ExpressionSyntax> contextValue,
        int contextCount)
    {
        return constructor switch
        {
            GeneratedRowContextConstructor.ContextArray => contextValue("__contexts"),
            GeneratedRowContextConstructor.SingleContext => CreateArrayCreation("object", [contextValue("__leftContext")]),
            GeneratedRowContextConstructor.SingleContexts => CreateArrayCreation(
                "object",
                Enumerable.Range(0, contextCount).Select(index => contextValue(CreateSingleContextFieldName(index)))),
            GeneratedRowContextConstructor.ContextRow => CreateContextRowContextsRead(contextValue("__leftContextsRow")),
            GeneratedRowContextConstructor.TwoSingleContexts => CreateArrayCreation(
                "object",
                [contextValue("__leftContext"), contextValue("__rightContext")]),
            GeneratedRowContextConstructor.TwoContextRows => CreateContextMaterializerCall(
                nameof(ContextMaterializer.Merge),
                CreateContextRowContextsRead(contextValue("__leftContextsRow")),
                CreateContextRowContextsRead(contextValue("__rightContextsRow"))),
            GeneratedRowContextConstructor.TwoContextArrays => CreateContextMaterializerCall(
                nameof(ContextMaterializer.MergePreservingNullSegments),
                contextValue("__leftContexts"),
                contextValue("__rightContexts")),
            GeneratedRowContextConstructor.ContextArrayAndSingleContext => CreateContextMaterializerCall(
                nameof(ContextMaterializer.AppendPreservingNullSegment),
                contextValue("__leftContexts"),
                contextValue("__rightContext")),
            GeneratedRowContextConstructor.ContextRowAndSingleContext => CreateContextMaterializerCall(
                nameof(ContextMaterializer.Append),
                CreateContextRowContextsRead(contextValue("__leftContextsRow")),
                contextValue("__rightContext")),
            GeneratedRowContextConstructor.SingleContextAndContextArray => CreateContextMaterializerCall(
                nameof(ContextMaterializer.PrependPreservingNullSegment),
                contextValue("__leftContext"),
                contextValue("__rightContexts")),
            GeneratedRowContextConstructor.SingleContextAndContextRow => CreateContextMaterializerCall(
                nameof(ContextMaterializer.Prepend),
                contextValue("__leftContext"),
                CreateContextRowContextsRead(contextValue("__rightContextsRow"))),
            GeneratedRowContextConstructor.ContextRowAndContextArray => CreateContextMaterializerCall(
                nameof(ContextMaterializer.Merge),
                CreateContextRowContextsRead(contextValue("__leftContextsRow")),
                contextValue("__rightContexts")),
            GeneratedRowContextConstructor.ContextArrayAndContextRow => CreateContextMaterializerCall(
                nameof(ContextMaterializer.Merge),
                contextValue("__leftContexts"),
                CreateContextRowContextsRead(contextValue("__rightContextsRow"))),
            _ => throw UnsupportedShape.Of($"Generated row constructor {constructor}")
        };
    }

    private static InvocationExpressionSyntax CreateContextRowContextsRead(ExpressionSyntax row)
    {
        return CreateContextMaterializerCall(
            nameof(ContextMaterializer.Read),
            row);
    }

    private static InvocationExpressionSyntax CreateContextMaterializerCall(
        string methodName,
        params ExpressionSyntax[] arguments)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(ContextMaterializer)),
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(CreateArgumentList(arguments));
    }

    private ExpressionSyntax RenderContextArray(
        ExecutionContextArray contextArray,
        ExecutionRenderContext context)
    {
        var segmentArrays = contextArray.Segments
            .Select(segment => RenderContextArraySegment(segment, context))
            .ToArray();

        if (segmentArrays.Length == 0)
            return CreateArrayCreation("object", []);

        return segmentArrays.Aggregate(static (left, right) => CreateContextMaterializerCall(
            nameof(ContextMaterializer.Merge),
            left,
            right));
    }

    private ExpressionSyntax RenderContextArraySegment(
        ExecutionContextSegment segment,
        ExecutionRenderContext context)
    {
        return segment.Kind switch
        {
            ExecutionContextSegmentKind.Single => CreateArrayCreation("object", [RenderContextSegmentValue(segment.Value, context)]),
            ExecutionContextSegmentKind.Row => CreateContextRowContextsRead(RenderExpression(segment.Value, context)),
            ExecutionContextSegmentKind.Array => RenderExpression(segment.Value, context),
            _ => throw new InvalidOperationException($"Execution context segment kind {segment.Kind} is not supported.")
        };
    }
}
