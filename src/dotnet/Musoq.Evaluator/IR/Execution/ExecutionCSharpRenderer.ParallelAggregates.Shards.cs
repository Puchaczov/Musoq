using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private LocalDeclarationStatementSyntax CreateParallelAggregateWorkerDeclaration(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string rowsName,
        string workerCountName,
        string shardsName,
        string cancellationTokenName,
        string workerName,
        IReadOnlyList<CapturedLocal> captures)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            workerName,
            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(
                    CreateParallelSingleKeyAggregateWorkerTypeName(parallelAggregate)))
                .WithArgumentList(CreateArgumentList(CreateParallelAggregateWorkerConstructorArguments(
                    rowsName,
                    workerCountName,
                    shardsName,
                    cancellationTokenName,
                    captures))));
    }

    private List<StatementSyntax> CreateParallelAggregateShardStatements(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string rowsName,
        string workerCountName,
        string shardsName,
        string shardIndexName,
        string cancellationTokenName)
    {
        const string startName = "start";
        const string endName = "end";
        const string groupsName = "groups";
        const string orderedGroupsName = "orderedGroups";
        const string indexName = "index";
        const string groupKeyName = "groupKey";

        var groupType = CreateAggregateGroupType(parallelAggregate.GroupShape);
        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                startName,
                CreateShardBoundaryExpression(rowsName, shardIndexName, workerCountName)),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                endName,
                CreateShardBoundaryExpression(
                    rowsName,
                    SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(
                        SyntaxKind.AddExpression,
                        SyntaxFactory.IdentifierName(shardIndexName),
                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)))),
                    workerCountName)),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                groupsName,
                SyntaxFactory.ObjectCreationExpression(CreateGroupDictionaryTypeSyntax(parallelAggregate.KeyType, groupType))
                    .WithArgumentList(SyntaxFactory.ArgumentList())),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                orderedGroupsName,
                SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(groupType))
                    .WithArgumentList(SyntaxFactory.ArgumentList()))
        };

        if (CanBeNull(parallelAggregate.KeyType))
        {
            statements.Add(CreateLocalDeclaration(
                groupType,
                CreateParallelNullGroupName(),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
        }

        statements.Add(CreateParallelAggregateShardRowLoop(
            parallelAggregate,
            rowsName,
            startName,
            endName,
            groupsName,
            orderedGroupsName,
            indexName,
            groupKeyName,
            cancellationTokenName));

        statements.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            CreateElementAccess(SyntaxFactory.IdentifierName(shardsName), SyntaxFactory.IdentifierName(shardIndexName)),
            SyntaxFactory.IdentifierName(orderedGroupsName))));

        return statements;
    }

    private ForStatementSyntax CreateParallelAggregateShardRowLoop(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string rowsName,
        string startName,
        string endName,
        string groupsName,
        string orderedGroupsName,
        string indexName,
        string groupKeyName,
        string cancellationTokenName)
    {
        var aggregateBodyStatements = RenderBlock(parallelAggregate.AggregateBody).Statements.ToList();
        var reuseGroupKeyLet = TryGetReusableGroupKeyLet(parallelAggregate, out var groupKeyLet);
        var body = new List<StatementSyntax>
        {
            CreatePeriodicCancellationCheck(indexName, cancellationTokenName),
            CreateLocalDeclaration(
                CreateVariableTypeSyntax(parallelAggregate.Source),
                parallelAggregate.Source.Name,
                CreateElementAccess(SyntaxFactory.IdentifierName(rowsName), SyntaxFactory.IdentifierName(indexName)))
        };

        if (reuseGroupKeyLet)
        {
            body.Add(RenderLet(groupKeyLet!));
            aggregateBodyStatements.RemoveAt(0);
        }

        body.Add(CreateLocalDeclaration(
            CreateTypeSyntax(parallelAggregate.KeyType),
            groupKeyName,
            reuseGroupKeyLet
                ? SyntaxFactory.IdentifierName(groupKeyLet!.Variable.Name)
                : RenderExpression(parallelAggregate.Key)));

        body.AddRange(CreateParallelAggregateGroupAcquisitionStatements(
            parallelAggregate,
            groupsName,
            orderedGroupsName,
            groupKeyName));
        body.AddRange(aggregateBodyStatements);

        return SyntaxFactory.ForStatement(StatementEmitter.CreateBlock(body))
            .WithDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(indexName)
                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(startName))))))
            .WithCondition(SyntaxFactory.BinaryExpression(
                SyntaxKind.LessThanExpression,
                SyntaxFactory.IdentifierName(indexName),
                SyntaxFactory.IdentifierName(endName)))
            .WithIncrementors(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                SyntaxFactory.PostfixUnaryExpression(
                    SyntaxKind.PostIncrementExpression,
                    SyntaxFactory.IdentifierName(indexName))));
    }

    private static bool TryGetReusableGroupKeyLet(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        out ExecutionLet? groupKeyLet)
    {
        if (parallelAggregate.AggregateBody.Nodes.FirstOrDefault() is ExecutionLet let &&
            let.Value.ReturnType == parallelAggregate.Key.ReturnType &&
            ExecutionExpressionFingerprint.ForHoist(let.Value) == ExecutionExpressionFingerprint.ForHoist(parallelAggregate.Key))
        {
            groupKeyLet = let;
            return true;
        }

        groupKeyLet = null;
        return false;
    }

    private IReadOnlyList<StatementSyntax> CreateParallelAggregateGroupAcquisitionStatements(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string groupsName,
        string orderedGroupsName,
        string groupKeyName)
    {
        return CanBeNull(parallelAggregate.KeyType)
            ? CreateParallelAggregateNullableGroupAcquisitionStatements(
                parallelAggregate,
                groupsName,
                orderedGroupsName,
                groupKeyName)
            : CreateParallelAggregateNonNullGroupAcquisitionStatements(
                parallelAggregate,
                groupsName,
                orderedGroupsName,
                groupKeyName,
                declareGroupVariable: true);
    }

    private StatementSyntax[] CreateParallelAggregateNonNullGroupAcquisitionStatements(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string groupsName,
        string orderedGroupsName,
        string groupKeyName,
        bool declareGroupVariable)
    {
        var groupCreation = CreateAggregateGroupCreation(
            parallelAggregate.GroupShape,
            CreateNoOwnerArguments(parallelAggregate.GroupShape),
            SyntaxFactory.IdentifierName(groupKeyName));
        var groupRefName = CreateGroupRefVariableName(parallelAggregate.Group.Name);

        return CreateDictionaryGroupAcquisitionStatements(
            CreateAggregateGroupType(parallelAggregate.GroupShape),
            parallelAggregate.Group.Name,
            groupsName,
            SyntaxFactory.IdentifierName(groupKeyName),
            groupCreation,
            [CreateCollectionAddStatement(orderedGroupsName, SyntaxFactory.IdentifierName(groupRefName))],
            declareGroupVariable);
    }

    private List<StatementSyntax> CreateParallelAggregateNullableGroupAcquisitionStatements(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string groupsName,
        string orderedGroupsName,
        string groupKeyName)
    {
        var groupType = CreateAggregateGroupType(parallelAggregate.GroupShape);
        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(
                groupType,
                parallelAggregate.Group.Name,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))
,SyntaxFactory.IfStatement(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                SyntaxFactory.IdentifierName(groupKeyName),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            StatementEmitter.CreateBlock(CreateParallelAggregateNonNullGroupAcquisitionStatements(
                parallelAggregate,
                groupsName,
                orderedGroupsName,
                groupKeyName,
                declareGroupVariable: false)),
            SyntaxFactory.ElseClause(StatementEmitter.CreateBlock(CreateParallelAggregateNullGroupAcquisitionStatements(
                parallelAggregate,
                orderedGroupsName,
                groupKeyName))))        };

        return statements;
    }

    private IReadOnlyList<StatementSyntax> CreateParallelAggregateNullGroupAcquisitionStatements(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string orderedGroupsName,
        string groupKeyName)
    {
        var nullGroupName = CreateParallelNullGroupName();
        var nullGroup = SyntaxFactory.IdentifierName(nullGroupName);

        return
        [
            SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    nullGroup,
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                StatementEmitter.CreateBlock(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        nullGroup,
                        CreateAggregateGroupCreation(
                            parallelAggregate.GroupShape,
                            CreateNoOwnerArguments(parallelAggregate.GroupShape),
                            SyntaxFactory.IdentifierName(groupKeyName)))),
                    CreateCollectionAddStatement(orderedGroupsName, nullGroup))),
            SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(parallelAggregate.Group.Name),
                nullGroup))
        ];
    }
}
