using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private LocalDeclarationStatementSyntax RenderCreateGeneratedRow(ExecutionCreateGeneratedRow createRow)
    {
        createRow = NormalizeLazyContextSegments(createRow);

        return CreateLocalDeclaration(
            CreateVariableTypeSyntax(createRow.Row),
            createRow.Row.Name,
            CreateGeneratedRowCreation(
                createRow.RowShape,
                createRow.Values,
                createRow.Contexts,
                createRow.ContextLayout));
    }

    private ObjectCreationExpressionSyntax CreateGeneratedRowCreation(
        GeneratedRowShape rowShape,
        IReadOnlyList<ExecutionRowValue> values,
        IReadOnlyList<ExecutionExpression> contexts,
        ExecutionContextLayout? contextLayout)
    {
        var rowValues = values
            .Select((value, index) => RenderRowConstructorValue(
                value.Value,
                rowShape.Fields[index].Type));

        if (rowShape.Contexts.Count == 0 || !GeneratedRowTypeUsesContextConstructor(rowShape.TypeName))
            return CreateObjectCreation(rowShape.TypeName, rowValues.ToArray());

        if (TryCreateContextLayoutArguments(contextLayout, contexts.Count, out var contextArguments))
            return CreateObjectCreation(rowShape.TypeName, [.. rowValues, .. contextArguments]);

        if (contexts.Count == 0)
            return CreateObjectCreation(rowShape.TypeName, rowValues.ToArray());

        return CreateObjectCreation(
            rowShape.TypeName,
            [.. rowValues, CreateArrayCreation("object", contexts.Select(RenderExpression))]);
    }

    private bool GeneratedRowTypeUsesContextConstructor(string typeName)
    {
        return _generatedRowConstructorUsagesByType.TryGetValue(typeName, out var usedConstructors) &&
               RequiresGeneratedRowContextOverride(usedConstructors);
    }
}
