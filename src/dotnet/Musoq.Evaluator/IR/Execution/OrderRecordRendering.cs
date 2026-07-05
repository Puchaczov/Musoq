using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private LocalDeclarationStatementSyntax RenderCreateRecordList(
        ExecutionCreateRecordList createList,
        ExecutionRenderContext context)
    {
        var argumentList = createList.CapacityHint == null
            ? SyntaxFactory.ArgumentList()
            : CreateArgumentList(RenderCapacityHint(createList.CapacityHint, context));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            createList.List.Name,
            SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(createList.RecordShape.TypeName))
                .WithArgumentList(argumentList));
    }

    private static LocalDeclarationStatementSyntax RenderCreateBoundedRecordList(ExecutionCreateBoundedRecordList createList)
    {
        var comparer = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(CreateOrderRecordComparerTypeName(createList.RecordShape)),
            SyntaxFactory.IdentifierName("Instance"));

        var arguments = createList.Selection switch
        {
            ExecutionTakeOrderRecordSelection take => new ExpressionSyntax[]
            {
                CreateIntLiteral(take.Count),
                comparer
            },
            ExecutionSkipTakeOrderRecordSelection skipTake => new ExpressionSyntax[]
            {
                CreateIntLiteral(skipTake.SkipCount),
                CreateIntLiteral(skipTake.TakeCount),
                comparer
            },
            _ => throw UnsupportedShape.Of(
                $"Bounded ORDER BY record selection '{createList.Selection.GetType().Name}'")
        };

        var listType = SyntaxFactory.ParseTypeName(
            $"{nameof(EvaluationHelper)}.BoundedTopRecordList<{createList.RecordShape.TypeName}>");

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            createList.List.Name,
            SyntaxFactory.ObjectCreationExpression(listType)
                .WithArgumentList(CreateArgumentList(arguments)));
    }

    private ExpressionStatementSyntax RenderAppendRecord(ExecutionAppendRecord appendRecord)
    {
        var rowValues = appendRecord.Values
            .Select((value, index) => RenderRowConstructorValue(
                value.Value,
                appendRecord.RecordShape.Fields[index].Type))
            .ToList();

        if (appendRecord.RecordShape.Fields.Count == appendRecord.Values.Count + 1 &&
            string.Equals(
                GetGeneratedFieldName(appendRecord.RecordShape.Fields[^1]),
                "__ordinal",
                StringComparison.Ordinal))
        {
            rowValues.Add(CreateCollectionCountRead(appendRecord.List.Name));
        }

        var recordCreation = CreateObjectCreation(appendRecord.RecordShape.TypeName, rowValues.ToArray());
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(appendRecord.List.Name),
                    SyntaxFactory.IdentifierName(nameof(List<>.Add))))
            .WithArgumentList(CreateArgumentList(recordCreation));

        return SyntaxFactory.ExpressionStatement(invocation);
    }

    private static ExpressionStatementSyntax RenderOrderRecordList(ExecutionOrderRecordList orderRecords)
    {
        var comparer = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(CreateOrderRecordComparerTypeName(orderRecords.RecordShape)),
            SyntaxFactory.IdentifierName("Instance"));

        return orderRecords.Selection switch
        {
            ExecutionFullOrderRecordSelection => SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(orderRecords.Source.Name),
                            SyntaxFactory.IdentifierName(nameof(List<>.Sort))))
                    .WithArgumentList(CreateArgumentList(comparer))),
            ExecutionTakeOrderRecordSelection take => SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(orderRecords.Source.Name),
                    CreateEvaluationHelperInvocation(
                        nameof(EvaluationHelper.SelectTopRecords),
                        SyntaxFactory.IdentifierName(orderRecords.Source.Name),
                        CreateIntLiteral(take.Count),
                        comparer))),
            ExecutionSkipTakeOrderRecordSelection skipTake => SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(orderRecords.Source.Name),
                    CreateEvaluationHelperInvocation(
                        nameof(EvaluationHelper.SelectTopOffsetRecords),
                        SyntaxFactory.IdentifierName(orderRecords.Source.Name),
                        CreateIntLiteral(skipTake.SkipCount),
                        CreateIntLiteral(skipTake.TakeCount),
                        comparer))),
            _ => throw UnsupportedShape.Of(
                $"ORDER BY record selection '{orderRecords.Selection.GetType().Name}'")
        };
    }
}
