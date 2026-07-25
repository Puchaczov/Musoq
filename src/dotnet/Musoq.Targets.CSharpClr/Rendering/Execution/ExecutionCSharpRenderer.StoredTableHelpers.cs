using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

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
            var bodyStatements = RenderIsolatedHelperBlock(
                new ExecutionBlock(build.Nodes),
                context,
                IsInstrumentationEnabled,
                emitChunkLoopCancellationChecks: true,
                trailingStatements: [SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(build.Table.Name))]);

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

        foreach (var variableName in CollectDeclaredVariableNames(new ExecutionBlock(build.Nodes)))
            excludedNames.Add(variableName);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHelperCaptures(new ExecutionBlock(build.Nodes), excludedNames, captures);
        return captures.Values.ToArray();
    }
}
