using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderFusedCteProducer(
        ExecutionFusedCteProducer producer,
        ExecutionRenderContext context)
    {
        var session = context.Session;
        var previousTypedRowBufferVariables = session.TypedRowBufferVariables;
        var typedRowBufferVariables = new Dictionary<string, GeneratedRowShape>(
            previousTypedRowBufferVariables,
            StringComparer.Ordinal);

        foreach (var output in producer.Outputs)
            typedRowBufferVariables[output.Table.Name] = output.RowShape;

        session.TypedRowBufferVariables = typedRowBufferVariables;

        try
        {
            foreach (var statement in RenderBlock(producer.Body, context).Statements)
                yield return statement;
        }
        finally
        {
            session.TypedRowBufferVariables = previousTypedRowBufferVariables;
        }

        foreach (var output in producer.Outputs.Where(static output => output.StoreRows))
            yield return CreateFusedCteOutputAssignment(output);
    }

    private StatementSyntax CreateFusedCteOutputAssignment(ExecutionFusedCteOutput output)
    {
        ExpressionSyntax target = RenderSession.TypedStoredTableResults.ContainsKey(output.TableIndex)
            ? CreateCteRowResultSlotAccess(output.TableIndex)
            : CreateElementAccess(
                SyntaxFactory.IdentifierName("_tableResults"),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(output.TableIndex)));

        return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            target,
            SyntaxFactory.IdentifierName(output.Table.Name)));
    }
}
