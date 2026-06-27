using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;
namespace Musoq.Evaluator.IR.Execution;
public sealed partial class ExecutionCSharpRenderer
{
    private const string SerialSingleKeyAggregateRowsParameterName = "rows";
    private const string SerialSingleKeyAggregateGroupsParameterName = "groups";
    private const string SerialSingleKeyAggregateGroupsToFinalizeParameterName = "groupsToFinalize";
    private const string SerialSingleKeyAggregateNullGroupParameterName = "nullGroup";
    private MethodDeclarationSyntax CreateSerialSingleKeyAggregateFunction(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        ValidateParallelSingleKeyAggregateShape(parallelAggregate);
        var captures = CollectSerialSingleKeyAggregateCaptures(parallelAggregate);
        var serialLoop = CreateSerialSingleKeyAggregateHelperLoop(parallelAggregate);
        var previousProfileRecorderInScope = _profileRecorderInScope;
        var previousEmitChunkLoopCancellationChecks = _emitChunkLoopCancellationChecks;
        _profileRecorderInScope = IsInstrumentationEnabled;
        _emitChunkLoopCancellationChecks = true;
        try
        {
            var bodyStatements = new List<StatementSyntax>
            {
                QueryEmitter.GenerateCancellationCheck()
            };
            bodyStatements.AddRange(RenderParallelLoopSerialFallback(serialLoop));

            return SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    CreateSerialSingleKeyAggregateFunctionName(parallelAggregate))
                .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithParameterList(CreateSerialSingleKeyAggregateParameterList(parallelAggregate, captures))
                .WithBody(CreateProfiledHelperBody(bodyStatements));
        }
        finally
        {
            _profileRecorderInScope = previousProfileRecorderInScope;
            _emitChunkLoopCancellationChecks = previousEmitChunkLoopCancellationChecks;
        }
    }
    private ParameterListSyntax CreateSerialSingleKeyAggregateParameterList(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        IReadOnlyList<CapturedLocal> captures)
    {
        var getOrAddGroup = GetSerialSingleKeyAggregateGroupAcquisition(parallelAggregate);
        var groupType = CreateAggregateGroupType(parallelAggregate.GroupShape);
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter(
                SerialSingleKeyAggregateRowsParameterName,
                CreateAggregateRowsParameterType(parallelAggregate.SourceRows, CreateVariableTypeSyntax(parallelAggregate.Source))),
            CreateParameter(
                SerialSingleKeyAggregateGroupsParameterName,
                CreateGroupDictionaryTypeSyntax(parallelAggregate.KeyType, groupType)),
            CreateParameter(
                SerialSingleKeyAggregateGroupsToFinalizeParameterName,
                CreateListTypeSyntax(groupType))
        };

        if (getOrAddGroup.NullGroup is not null)
        {
            parameters.Add(CreateParameter(SerialSingleKeyAggregateNullGroupParameterName, groupType)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword))));
        }
        parameters.Add(CreateParameter("token", CreateTypeSyntax(typeof(CancellationToken))));
        AddProfileRecorderParameter(parameters);
        parameters.AddRange(captures.Select(CreateCapturedLocalParameter));

        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }
    private static ExecutionSourceLoop CreateSerialSingleKeyAggregateHelperLoop(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var getOrAddGroup = GetSerialSingleKeyAggregateGroupAcquisition(parallelAggregate);
        var helperGetOrAddGroup = CreateSerialSingleKeyAggregateHelperGroupAcquisition(getOrAddGroup);
        var replacedGetOrAddGroup = false;
        var nodes = parallelAggregate.SerialLoop.Body.Nodes
            .Select(node =>
            {
                if (node is not ExecutionGetOrAddSingleKeyAggregateGroup candidate || !Equals(candidate, getOrAddGroup))
                    return node;

                replacedGetOrAddGroup = true;
                return helperGetOrAddGroup;
            })
            .ToArray();

        if (!replacedGetOrAddGroup)
        {
            throw new InvalidOperationException(
                $"Parallel single-key aggregate loop for '{parallelAggregate.GroupsToFinalize.Name}' could not normalize its serial group acquisition.");
        }

        var rowsParameter = new ExecutionVariable(
            SerialSingleKeyAggregateRowsParameterName,
            typeof(object),
            parallelAggregate.Source.GeneratedRowTypeName);

        return parallelAggregate.SerialLoop with
        {
            Source = ExecutionRowStreams.RebindLike(parallelAggregate.SourceRows, rowsParameter),
            Body = new ExecutionBlock(nodes)
        };
    }
    private ExpressionStatementSyntax CreateSerialSingleKeyAggregateInvocation(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var getOrAddGroup = GetSerialSingleKeyAggregateGroupAcquisition(parallelAggregate);
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(RenderExpression(parallelAggregate.SerialLoop.Source)),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(getOrAddGroup.Groups.Name)),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(getOrAddGroup.GroupsToFinalize.Name))
        };

        if (getOrAddGroup.NullGroup is not null)
        {
            arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(getOrAddGroup.NullGroup.Name))
                .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword)));
        }

        arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token")));
        AddProfileRecorderArgument(arguments);
        arguments.AddRange(CollectSerialSingleKeyAggregateCaptures(parallelAggregate)
            .Select(capture => SyntaxFactory.Argument(CreateCapturedLocalArgument(capture))));

        return SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName(CreateSerialSingleKeyAggregateFunctionName(parallelAggregate)))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))));
    }
}
