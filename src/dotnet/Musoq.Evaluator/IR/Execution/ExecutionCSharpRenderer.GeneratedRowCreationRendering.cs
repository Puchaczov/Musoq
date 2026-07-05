using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private LocalDeclarationStatementSyntax RenderCreateGeneratedRow(
        ExecutionCreateGeneratedRow createRow,
        ExecutionRenderContext context)
    {
        createRow = NormalizeLazyContextSegments(createRow);

        return CreateLocalDeclaration(
            CreateVariableTypeSyntax(createRow.Row),
            createRow.Row.Name,
            CreateGeneratedRowCreation(
                createRow.RowShape,
                createRow.Values,
                createRow.Contexts,
                createRow.ContextLayout,
                context));
    }

    private ObjectCreationExpressionSyntax CreateGeneratedRowCreation(
        GeneratedRowShape rowShape,
        IReadOnlyList<ExecutionRowValue> values,
        IReadOnlyList<ExecutionExpression> contexts,
        ExecutionContextLayout? contextLayout,
        ExecutionRenderContext context)
    {
        var rowValues = values
            .Select((value, index) => RenderRowConstructorValue(
                value.Value,
                rowShape.Fields[index].Type,
                context));

        if (rowShape.Contexts.Count == 0 || !GeneratedRowTypeUsesContextConstructor(rowShape.TypeName, context))
            return CreateObjectCreation(rowShape.TypeName, rowValues.ToArray());

        if (TryCreateContextLayoutArguments(contextLayout, contexts.Count, context, out var contextArguments))
            return CreateObjectCreation(rowShape.TypeName, [.. rowValues, .. contextArguments]);

        if (contexts.Count == 0)
            return CreateObjectCreation(rowShape.TypeName, rowValues.ToArray());

        return CreateObjectCreation(
            rowShape.TypeName,
            [.. rowValues, CreateArrayCreation("object", contexts.Select(item => RenderExpression(item, context)))]);
    }

    private static bool GeneratedRowTypeUsesContextConstructor(string typeName, ExecutionRenderContext context)
    {
        return context.Session.GeneratedRowConstructorUsagesByType.TryGetValue(typeName, out var usedConstructors) &&
               RequiresGeneratedRowContextOverride(usedConstructors);
    }
}
