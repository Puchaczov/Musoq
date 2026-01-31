using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Converter.Build;

public class TransformTree(BuildChain successor, ILoggerResolver loggerResolver) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        items.SchemaProvider = new TransitionSchemaProvider(items.SchemaProvider);

        var queryTree = items.RawQueryTree;

        var distinctRewriter = new DistinctToGroupByVisitor();
        var distinctTraverser = new DistinctToGroupByTraverseVisitor(distinctRewriter);
        queryTree.Accept(distinctTraverser);
        queryTree = distinctTraverser.Root;

        var extractColumnsVisitor = new ExtractRawColumnsVisitor();
        var extractRawColumnsTraverseVisitor = new ExtractRawColumnsTraverseVisitor(extractColumnsVisitor);

        queryTree.Accept(extractRawColumnsTraverseVisitor);

        var metadata =
            items.CreateBuildMetadataAndInferTypesVisitor?.Invoke(items.SchemaProvider, extractColumnsVisitor.Columns,
                items.CompilationOptions) ??
            new BuildMetadataAndInferTypesVisitor(items.SchemaProvider, extractColumnsVisitor.Columns,
                loggerResolver.ResolveLogger<BuildMetadataAndInferTypesVisitor>(), items.CompilationOptions,
                items.SchemaRegistry);
        var metadataTraverser = new BuildMetadataAndInferTypesTraverseVisitor(metadata);

        queryTree.Accept(metadataTraverser);
        queryTree = metadata.Root;


        queryTree = EliminateDeadCtes(queryTree);


        if (items.CompilationOptions.UseCteParallelization) items.CteExecutionPlan = ComputeCteExecutionPlan(queryTree);

        var rewriter = new RewriteQueryVisitor();
        var rewriteTraverser = new RewriteQueryTraverseVisitor(rewriter, new ScopeWalker(metadataTraverser.Scope));

        queryTree.Accept(rewriteTraverser);

        queryTree = rewriter.RootScript;

        var csharpRewriter = CreateCSharpRewriter(metadata, items);
        var csharpRewriteTraverser = new ToCSharpRewriteTreeTraverseVisitor(csharpRewriter,
            new ScopeWalker(metadataTraverser.Scope), items.CompilationOptions);

        queryTree.Accept(csharpRewriteTraverser);

        items.UsedColumns = metadata.UsedColumns;
        items.UsedWhereNodes = RewriteWhereNodes(metadata.UsedWhereNodes);
        items.QueryHintsPerSchema = metadata.QueryHintsPerSchema
            .ToDictionary(kvp => kvp.Key.Id, kvp => kvp.Value);
        items.TransformedQueryTree = queryTree;
        items.Compilation = csharpRewriter.Compilation;
        items.AccessToClassPath = csharpRewriter.AccessToClassPath;
        items.PositionalEnvironmentVariables = metadata.PositionalEnvironmentVariables;

        Successor?.Build(items);
    }

    private static RootNode EliminateDeadCtes(RootNode queryTree)
    {
        if (queryTree.Expression is not CteExpressionNode cteExpression)
            return queryTree;

        var result = DeadCteEliminator.Eliminate(cteExpression);


        if (!result.WereCTEsEliminated)
            return queryTree;


        return new RootNode(result.ResultNode);
    }

    private static IToCSharpTranslationExpressionVisitor CreateCSharpRewriter(
        BuildMetadataAndInferTypesVisitor metadata,
        BuildItems items)
    {
        return new ToCSharpRewriteTreeVisitor(
            metadata.Assemblies,
            metadata.SetOperatorFieldPositions,
            metadata.InferredColumns,
            items.AssemblyName,
            items.CompilationOptions,
            items.SchemaRegistry,
            items.InterpreterSourceCode,
            items.CteExecutionPlan);
    }

    private static IReadOnlyDictionary<SchemaFromNode, WhereNode> RewriteWhereNodes(
        IReadOnlyDictionary<SchemaFromNode, WhereNode> whereNodes)
    {
        var result = new Dictionary<SchemaFromNode, WhereNode>();

        foreach (var whereNode in whereNodes)
        {
            var rewriter = new RewriteWhereExpressionToPassItToDataSourceVisitor(whereNode.Key);
            var rewriteTraverser = new RewriteWhereExpressionToPassItToDataSourceTraverseVisitor(rewriter);

            whereNode.Value.Accept(rewriteTraverser);

            result.Add(whereNode.Key, rewriter.WhereNode);
        }

        return result;
    }

    private static CteExecutionPlan? ComputeCteExecutionPlan(RootNode queryTree)
    {
        CteExpressionNode? cteExpression = null;

        if (queryTree.Expression is CteExpressionNode directCte)
            cteExpression = directCte;
        else if (queryTree.Expression is StatementsArrayNode statementsArray)
            foreach (var statement in statementsArray.Statements)
                if (statement.Node is CteExpressionNode nestedCte)
                {
                    cteExpression = nestedCte;
                    break;
                }

        if (cteExpression == null)
            return null;

        return CteParallelizationAnalyzer.CreatePlan(cteExpression);
    }
}
