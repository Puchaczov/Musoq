using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private MethodDeclarationSyntax CreateSortedCopyFunction(
        SortedCopyHelper helper,
        ExecutionRenderContext context)
    {
        var bodyStatements = RenderIsolatedHelperBlock(new ExecutionBlock([helper.Sort]), context).ToList();
        bodyStatements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(helper.Sort.Target.Name)));

        return SyntaxFactory.MethodDeclaration(
                CreateTypeSyntax(typeof(Table)),
                helper.FunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateSortedCopyParameterList(helper))
            .WithBody(StatementEmitter.CreateBlock(bodyStatements));
    }

    private static LocalDeclarationStatementSyntax CreateSortedCopyInvocation(SortedCopyHelper helper)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            helper.Sort.Target.Name,
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(helper.FunctionName))
                .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(helper.Sort.Source.Name))));
    }

    private static ParameterListSyntax CreateSortedCopyParameterList(SortedCopyHelper helper)
    {
        return SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
            CreateParameter(helper.Sort.Source.Name, CreateTypeSyntax(typeof(Table)))));
    }

    private static IEnumerable<SortedCopyHelper> CollectSortedCopyHelpers(ExecutionBlock block)
    {
        foreach (var (helper, _) in CollectSortedCopyHelpersWithIndexes(block))
            yield return helper;
    }

    private static IEnumerable<(SortedCopyHelper Helper, int Index)> CollectSortedCopyHelpersWithIndexes(ExecutionBlock block)
    {
        if (!ContainsNode<ExecutionComputeRankingWindow>(block))
            yield break;

        var pending = new List<ExecutionNode>();
        var nodes = block.Nodes;
        var usedFunctionNames = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            if (node is ExecutionStoreTable store &&
                TryCreateStoredTableBuild(nodes, index, pending, store, out _))
            {
                pending.Clear();
                continue;
            }

            if (IsInsidePendingStoredTableBuild(nodes, index, pending))
            {
                pending.Add(node);
                continue;
            }

            if (node is ExecutionSortTable sort && CanExtractSortedCopy(sort))
            {
                var functionName = ReserveFunctionName(CreateSortedCopyFunctionBaseName(sort), usedFunctionNames);
                yield return (new SortedCopyHelper(functionName, sort), index);
            }

            pending.Add(node);
        }
    }

    private static bool CanExtractSortedCopy(ExecutionSortTable sort)
    {
        return string.Equals(sort.Source.Name, "result", StringComparison.Ordinal) &&
               sort.Target.Name.EndsWith("Sorted", StringComparison.Ordinal);
    }

    private static string CreateSortedCopyFunctionBaseName(ExecutionSortTable sort)
    {
        return $"Build{CreatePascalIdentifier(sort.Target.Name)}";
    }
}
