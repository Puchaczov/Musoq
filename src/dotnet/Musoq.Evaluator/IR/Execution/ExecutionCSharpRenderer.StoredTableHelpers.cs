using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

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

    private ExpressionStatementSyntax CreateStoredTableBuildInvocation(StoredTableBuild build)
    {
        if (_typedStoredTableResults.ContainsKey(build.TableIndex))
        {
            return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateCteRowResultSlotAccess(build.TableIndex),
                CreateRuntimeHelperInvocation(
                    CreateStoredTableBuildFunctionName(build.TableIndex),
                    build.Captures)));
        }

        return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            CreateElementAccess(
                SyntaxFactory.IdentifierName("_tableResults"),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(build.TableIndex))),
            CreateRuntimeHelperInvocation(
                CreateStoredTableBuildFunctionName(build.TableIndex),
                build.Captures)));
    }


    private static string CreateStoredTableBuildFunctionName(int tableIndex)
    {
        return CreateIdentifierCandidate($"BuildCte{tableIndex.ToString(CultureInfo.InvariantCulture)}", 0);
    }

    private IEnumerable<StoredTableBuild> CollectStoredTableBuilds(ExecutionBlock block)
    {
        foreach (var build in StoredTableBuildDiscovery.Collect(block))
        {
            yield return build with
            {
                Captures = CollectStoredTableBuildCaptures(build)
            };
        }
    }

    private MethodDeclarationSyntax CreateStoredTableBuildFunction(StoredTableBuild build)
    {
        var previousTypedRowBufferVariables = _typedRowBufferVariables;
        if (_typedStoredTableResults.TryGetValue(build.TableIndex, out var typedResult))
        {
            _typedRowBufferVariables = new Dictionary<string, GeneratedRowShape>(StringComparer.Ordinal)
            {
                [build.Table.Name] = typedResult.RowShape
            };
        }

        try
        {
            var bodyStatements = RenderIsolatedHelperBlock(
                new ExecutionBlock(build.Nodes),
                IsInstrumentationEnabled,
                emitChunkLoopCancellationChecks: true,
                trailingStatements: [SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(build.Table.Name))]);

            return SyntaxFactory.MethodDeclaration(
                    CreateStoredTableBuildReturnType(build),
                    CreateStoredTableBuildFunctionName(build.TableIndex))
                .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
                .WithModifiers(CreatePrivateStaticModifiers())
                .WithParameterList(CreateRuntimeHelperParameterList(build.Captures))
                .WithBody(StatementEmitter.CreateBlock(bodyStatements));
        }
        finally
        {
            _typedRowBufferVariables = previousTypedRowBufferVariables;
        }
    }

    private TypeSyntax CreateStoredTableBuildReturnType(StoredTableBuild build)
    {
        return _typedStoredTableResults.TryGetValue(build.TableIndex, out var typedResult)
            ? CreateCteRowResultSlotTypeSyntax(typedResult.RowShape)
            : CreateTypeSyntax(typeof(Table));
    }

    private CapturedLocal[] CollectStoredTableBuildCaptures(StoredTableBuild build)
    {
        var excludedNames = new HashSet<string>(CreateRuntimeHelperParameterNames(), StringComparer.Ordinal)
        {
            build.Table.Name
        };

        foreach (var variableName in CollectDeclaredVariableNames(new ExecutionBlock(build.Nodes)))
            excludedNames.Add(variableName);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHelperCaptures(new ExecutionBlock(build.Nodes), excludedNames, captures);
        return captures.Values.ToArray();
    }
}
