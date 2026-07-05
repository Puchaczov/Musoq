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
    private MethodDeclarationSyntax CreateWindowAppendRowsFunction(
        WindowAppendRowsHelper helper,
        ExecutionRenderContext context)
    {
        if (TryCreateWindowAppendRowsShardFunction(helper, context, out var shardFunction))
            return shardFunction;

        var item = CreateWindowHelperItem(helper.Loop.Item, helper.BufferItemGeneratedRowTypeName);
        var helperLoop = helper.Loop with
        {
            Item = item,
            Source = helper.Loop.Source with { Name = helper.RowsParameterName }
        };

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.FunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateWindowAppendRowsParameterList(helper, context))
            .WithBody(StatementEmitter.CreateBlock(RenderIsolatedHelperBlock(new ExecutionBlock([helperLoop]), context)));
    }

    private bool TryCreateWindowAppendRowsShardFunction(
        WindowAppendRowsHelper helper,
        ExecutionRenderContext context,
        out MethodDeclarationSyntax shardFunction)
    {
        shardFunction = null!;

        if (helper.AppendTargets.Count != 1 ||
            helper.Loop.Body.Nodes.Count != 1 ||
            helper.Loop.Body.Nodes[0] is not ExecutionAppendRow appendRow ||
            appendRow.AppendMode != ExecutionAppendMode.Direct ||
            TryGetTypedRowBufferShape(appendRow.Table.Name, context, out _))
        {
            return false;
        }

        var item = CreateWindowHelperItem(helper.Loop.Item, helper.BufferItemGeneratedRowTypeName);
        var source = helper.Loop.Source with { Name = helper.RowsParameterName };
        var rowCountName = $"{helper.RowsParameterName}Count";
        var rowShardName = $"{appendRow.Table.Name}RowsShard";
        var indexName = helper.Loop.Index.Name;
        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                rowCountName,
                CreateBufferCountExpression(source)),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                rowShardName,
                SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(appendRow.RowShape.TypeName))
                        .WithRankSpecifiers(SyntaxFactory.SingletonList(
                            SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.IdentifierName(rowCountName)))))))
        };

        var bodyStatements = new List<StatementSyntax>();
        bodyStatements.AddRange(CreateIndexedItemDeclarations(
            item,
            source,
            helper.Loop.Index,
            helper.Loop.RowAccessMode));
        bodyStatements.Add(SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateElementAccess(
                    SyntaxFactory.IdentifierName(rowShardName),
                    SyntaxFactory.IdentifierName(indexName)),
                CreateGeneratedRowCreation(appendRow, context))));

        statements.Add(StatementEmitter.CreateForLoop(
            indexName,
            0,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.LessThanExpression,
                SyntaxFactory.IdentifierName(indexName),
                SyntaxFactory.IdentifierName(rowCountName)),
            SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.PreIncrementExpression,
                SyntaxFactory.IdentifierName(indexName)),
            StatementEmitter.CreateBlock(bodyStatements)));

        statements.Add(CreateAddDirectDeferredRowsStatement(
            appendRow.Table.Name,
            rowShardName,
            rowCountName));

        shardFunction = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                helper.FunctionName)
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(CreatePrivateStaticModifiers())
            .WithParameterList(CreateWindowAppendRowsParameterList(helper, context))
            .WithBody(StatementEmitter.CreateBlock(statements));

        return true;
    }

    private static ExpressionStatementSyntax CreateAddDirectDeferredRowsStatement(
        string tableName,
        string rowShardName,
        string rowCountName)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(tableName),
                        SyntaxFactory.IdentifierName(nameof(Table.AddDirectDeferred))))
                .WithArgumentList(CreateArgumentList(
                    SyntaxFactory.IdentifierName(rowShardName),
                    SyntaxFactory.IdentifierName(rowCountName))));
    }

    private static ExpressionStatementSyntax CreateWindowAppendRowsInvocation(WindowAppendRowsHelper helper)
    {
        return CreateHelperInvocation(helper.FunctionName, CreateWindowAppendRowsArguments(helper));
    }

    private ParameterListSyntax CreateWindowAppendRowsParameterList(
        WindowAppendRowsHelper helper,
        ExecutionRenderContext context)
    {
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter(
                helper.RowsParameterName,
                CreateWindowRowsParameterType(
                    helper.Loop.Source,
                    helper.Loop.Item,
                    helper.BufferItemGeneratedRowTypeName))
        };

        parameters.AddRange(helper.AppendTargets.Select(target => CreateParameter(
            target.Name,
            CreateWindowAppendTargetType(target, context))));
        parameters.AddRange(helper.Captures.Select(CreateCapturedLocalParameter));
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private TypeSyntax CreateWindowAppendTargetType(
        ExecutionVariable target,
        ExecutionRenderContext context)
    {
        if (TryGetFinalShapeSourceBuffer(target.Name, context, out var finalShapeBuffer))
            return CreateListTypeSyntax(finalShapeBuffer.ShapeTypeName);

        return TryGetTypedRowBufferShape(target.Name, context, out var rowShape)
            ? CreateListTypeSyntax(rowShape.TypeName)
            : CreateTypeSyntax(typeof(Table));
    }

    private static List<ExpressionSyntax> CreateWindowAppendRowsArguments(WindowAppendRowsHelper helper)
    {
        var arguments = new List<ExpressionSyntax>
        {
            SyntaxFactory.IdentifierName(helper.Loop.Source.Name)
        };

        arguments.AddRange(helper.AppendTargets.Select(static target => SyntaxFactory.IdentifierName(target.Name)));
        arguments.AddRange(helper.Captures.Select(CreateCapturedLocalArgument));
        return arguments;
    }

    private IEnumerable<WindowAppendRowsHelper> CollectWindowAppendRowsHelpers(ExecutionBlock block)
    {
        foreach (var (helper, _) in CollectWindowAppendRowsHelpersWithIndexes(block))
            yield return helper;
    }

    private IEnumerable<(WindowAppendRowsHelper Helper, int Index)> CollectWindowAppendRowsHelpersWithIndexes(
        ExecutionBlock block)
    {
        var helperIndex = 0;
        var pending = new List<ExecutionNode>();
        var nodes = block.Nodes;
        var usedFunctionNames = new Dictionary<string, int>(StringComparer.Ordinal);
        var materializedRowTypeNames = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            AddMaterializedRowTypeName(node, materializedRowTypeNames);

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

            if (TryCreateWindowAppendRowsHelper(node, helperIndex, usedFunctionNames, materializedRowTypeNames, out var helper))
            {
                yield return (helper, index);
                helperIndex++;
            }

            pending.Add(node);
        }
    }

    private bool TryCreateWindowAppendRowsHelper(
        ExecutionNode node,
        int helperIndex,
        Dictionary<string, int> usedFunctionNames,
        IReadOnlyDictionary<string, string> materializedRowTypeNames,
        out WindowAppendRowsHelper helper)
    {
        helper = null!;

        if (node is not ExecutionForEachIndexed loop || !CanExtractWindowAppendRows(loop))
            return false;

        var appendTargets = CollectWindowAppendTargets(loop.Body);
        if (appendTargets.Length == 0)
            return false;

        var baseName = CreateWindowAppendRowsFunctionBaseName(appendTargets[0], helperIndex);
        helper = new WindowAppendRowsHelper(
            ReserveFunctionName(baseName, usedFunctionNames),
            loop.Source.Name,
            ResolveGeneratedRowTypeName(loop.Source, loop.Item, materializedRowTypeNames),
            loop,
            appendTargets,
            CollectWindowAppendRowsCaptures(loop, appendTargets));
        return true;
    }

    private static bool CanExtractWindowAppendRows(ExecutionForEachIndexed loop)
    {
        return ContainsNode<ExecutionAppendRow>(loop.Body) &&
               (loop.Source.Name.EndsWith("WindowRows", StringComparison.Ordinal) ||
                ContainsWindowValueRead(loop.Body));
    }

    private static ExecutionVariable[] CollectWindowAppendTargets(ExecutionBlock block)
    {
        var targets = new Dictionary<string, ExecutionVariable>(StringComparer.Ordinal);
        AddAppendTargets(block, targets);
        return targets.Values.ToArray();
    }

    private CapturedLocal[] CollectWindowAppendRowsCaptures(
        ExecutionForEachIndexed loop,
        IReadOnlyList<ExecutionVariable> appendTargets)
    {
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            loop.Source.Name,
            loop.Item.Name,
            loop.Index.Name
        };

        foreach (var appendTarget in appendTargets)
            excludedNames.Add(appendTarget.Name);
        foreach (var variableName in CollectDeclaredVariableNames(loop.Body))
            excludedNames.Add(variableName);
        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHashJoinBodyHelperCaptures(loop.Body, excludedNames, captures);
        return captures.Values.ToArray();
    }

    private static string CreateWindowAppendRowsFunctionBaseName(ExecutionVariable target, int helperIndex)
    {
        var suffix = helperIndex == 0
            ? string.Empty
            : helperIndex.ToString(CultureInfo.InvariantCulture);
        return $"Append{CreatePascalIdentifier(target.Name)}WindowRows{suffix}";
    }

    private static bool ContainsWindowValueRead(ExecutionBlock block)
    {
        return ExecutionIrAnalysis.CollectExpressions<ExecutionWindowValueRead>(block).Any();
    }

}
