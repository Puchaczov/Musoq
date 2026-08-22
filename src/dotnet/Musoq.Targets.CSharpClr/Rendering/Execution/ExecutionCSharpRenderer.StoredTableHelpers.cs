using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static ValueTupleAggregateHelper? CreateValueTupleAggregateHelper(
        IReadOnlyList<ExecutionNode> nodes,
        int startIndex)
    {
        if (startIndex + 3 >= nodes.Count ||
            nodes[startIndex] is not ExecutionCreateValueTupleAggregateContext context ||
            nodes[startIndex + 1] is not ExecutionSourceLoop accumulationLoop ||
            nodes[startIndex + 2] is not ExecutionEnsureTableCapacity ensureCapacity ||
            nodes[startIndex + 3] is not ExecutionForEach finalizationLoop)
        {
            return null;
        }

        return new ValueTupleAggregateHelper(
            CreateValueTuplePopulateFunctionName(ensureCapacity.Table.Name),
            CreateValueTupleFinalizeFunctionName(ensureCapacity.Table.Name),
            context,
            accumulationLoop,
            ensureCapacity,
            finalizationLoop);
    }

    private static ExpressionStatementSyntax CreateHelperInvocation(
        string functionName,
        IReadOnlyList<ExpressionSyntax> arguments)
    {
        var invocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(functionName))
            .WithArgumentList(CreateArgumentList(arguments));
        return SyntaxFactory.ExpressionStatement(
            CodegenHelperExtractionMetadata.AnnotateCallSite(invocation, functionName));
    }

    private static bool TryCreateStoredTableBuild(
        IReadOnlyList<ExecutionNode> nodes,
        int storeIndex,
        List<ExecutionNode> pendingNodes,
        ExecutionStoreTable store,
        out StoredTableBuild build)
    {
        return StoredTableBuildDiscovery.TryCreate(nodes, storeIndex, pendingNodes, store, out build);
    }

    private ExpressionStatementSyntax CreateStoredTableBuildInvocation(
        StoredTableBuild build,
        ExecutionRenderContext context)
    {
        if (context.Session.TypedStoredTableResults.ContainsKey(build.TableIndex))
        {
            return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateCteRowResultSlotAccess(build.TableIndex),
                CreateRuntimeHelperInvocation(
                    CreateStoredTableBuildFunctionName(build.TableIndex),
                    build.Captures,
                    context)));
        }

        return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            CreateElementAccess(
                SyntaxFactory.IdentifierName("_tableResults"),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(build.TableIndex))),
            CreateRuntimeHelperInvocation(
                CreateStoredTableBuildFunctionName(build.TableIndex),
                build.Captures,
                context)));
    }


    private static string CreateStoredTableBuildFunctionName(int tableIndex)
    {
        return CreateIdentifierCandidate($"BuildCte{tableIndex.ToString(CultureInfo.InvariantCulture)}", 0);
    }

    private IEnumerable<StoredTableBuild> CollectStoredTableBuilds(
        ExecutionBlock block,
        ExecutionRenderContext context)
    {
        foreach (var build in StoredTableBuildDiscovery.Collect(block))
        {
            yield return build with
            {
                Captures = CollectStoredTableBuildCaptures(build, context)
            };
        }
    }

    private MethodDeclarationSyntax CreateStoredTableBuildFunction(
        StoredTableBuild build,
        ExecutionRenderContext context)
    {
        var previousTypedRowBufferVariables = context.Session.TypedRowBufferVariables;
        if (context.Session.TypedStoredTableResults.TryGetValue(build.TableIndex, out var typedResult))
        {
            context.Session.TypedRowBufferVariables = new Dictionary<string, GeneratedRowShape>(StringComparer.Ordinal)
            {
                [build.Table.Name] = typedResult.RowShape
            };
        }

        try
        {
            var helperNodes = build.Nodes
                .Concat(build.TrailingPhaseNodes)
                .ToArray();
            var bodyStatements = RenderStoredTableBuildBody(
                helperNodes,
                build.Table.Name,
                context);

            return SyntaxFactory.MethodDeclaration(
                    CreateStoredTableBuildReturnType(build, context),
                    CreateStoredTableBuildFunctionName(build.TableIndex))
                .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
                .WithModifiers(CreatePrivateStaticModifiers())
                .WithParameterList(CreateRuntimeHelperParameterList(build.Captures, context))
                .WithBody(StatementEmitter.CreateBlock(bodyStatements));
        }
        finally
        {
            context.Session.TypedRowBufferVariables = previousTypedRowBufferVariables;
        }
    }

    private StatementSyntax[] RenderStoredTableBuildBody(
        IReadOnlyList<ExecutionNode> helperNodes,
        string tableName,
        ExecutionRenderContext context)
    {
        var beginIndex = -1;
        var endIndex = -1;
        string? suffix = null;
        for (var index = 0; index < helperNodes.Count; index++)
        {
            if (helperNodes[index] is ExecutionPhaseBoundary beginBoundary &&
                beginBoundary.Phase == QueryPhase.Begin &&
                !string.IsNullOrEmpty(beginBoundary.QueryIdSuffix))
            {
                beginIndex = index;
                suffix = beginBoundary.QueryIdSuffix;
                break;
            }
        }

        if (beginIndex >= 0)
        {
            for (var index = beginIndex + 1; index < helperNodes.Count; index++)
            {
                if (helperNodes[index] is ExecutionPhaseBoundary endBoundary &&
                    endBoundary.Phase == QueryPhase.End &&
                    string.Equals(endBoundary.QueryIdSuffix, suffix, StringComparison.Ordinal))
                {
                    endIndex = index;
                    break;
                }
            }
        }

        if (beginIndex < 0 || endIndex < 0)
        {
            return RenderIsolatedHelperBlock(
                new ExecutionBlock(helperNodes),
                context,
                IsInstrumentationEnabled,
                emitChunkLoopCancellationChecks: true,
                trailingStatements: [SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(tableName))]);
        }

        var bodyNodes = helperNodes
            .Where((_, index) => index != beginIndex && index != endIndex)
            .Where(static node => node is not ExecutionPhaseBoundary)
            .ToArray();
        var bodyStatements = RenderIsolatedHelperBlock(
            new ExecutionBlock(bodyNodes),
            context,
            IsInstrumentationEnabled,
            emitChunkLoopCancellationChecks: true,
            trailingStatements: [SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(tableName))]);
        var beginStatements = RenderNode(helperNodes[beginIndex], context).ToArray();
        var endStatements = RenderNode(helperNodes[endIndex], context).ToArray();
        var guardedBody = SyntaxFactory.TryStatement()
            .WithBlock(StatementEmitter.CreateBlock(bodyStatements))
            .WithFinally(SyntaxFactory.FinallyClause(StatementEmitter.CreateBlock(endStatements)));

        return [..beginStatements, guardedBody];
    }

    private TypeSyntax CreateStoredTableBuildReturnType(
        StoredTableBuild build,
        ExecutionRenderContext context)
    {
        return context.Session.TypedStoredTableResults.TryGetValue(build.TableIndex, out var typedResult)
            ? CreateCteRowResultSlotTypeSyntax(typedResult.RowShape)
            : CreateTypeSyntax(typeof(Table));
    }

    private CapturedLocal[] CollectStoredTableBuildCaptures(
        StoredTableBuild build,
        ExecutionRenderContext context)
    {
        var excludedNames = new HashSet<string>(CreateRuntimeHelperParameterNames(context), StringComparer.Ordinal)
        {
            build.Table.Name
        };

        var helperNodes = build.Nodes
            .Concat(build.TrailingPhaseNodes)
            .ToArray();
        foreach (var variableName in CollectDeclaredVariableNames(new ExecutionBlock(helperNodes)))
            excludedNames.Add(variableName);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHelperCaptures(new ExecutionBlock(helperNodes), excludedNames, captures);
        return captures.Values.ToArray();
    }
}
