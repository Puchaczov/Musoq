using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionBindingInvariantValidator
{
    private static readonly ExecutionTypeRef TableRowType = ExecutionClrBindingFactory.FromClr(typeof(Row));
    private static readonly ExecutionTypeRef ObjectType = ExecutionClrBindingFactory.FromClr(typeof(object));

    private static void ValidateBlockBindings(
        ExecutionBlock block,
        IReadOnlyDictionary<string, RowShape[]> shapesByAlias,
        SourceBindingScope inherited
    )
    {
        var scope = new SourceBindingScope(inherited);
        foreach (var node in block.Nodes)
        {
            var nodeScope = node
                is ExecutionSourceLoop
                    or ExecutionForEachIndexed
                    or ExecutionParallelFilterProjectLoop
                    or ExecutionParallelSingleKeyAggregateLoop
                ? new SourceBindingScope(scope)
                : scope;
            foreach (var variable in ExecutionNodeFacts.GetDeclaredVariables(node))
                nodeScope.Declare(variable);

            var expressionScope = node is ExecutionSourceLoop ? scope : nodeScope;
            foreach (
                var expression in ExecutionIrAnalysis.FlattenExpressions(ExecutionIrAnalysis.GetNodeExpressions(node))
            )
            {
                if (
                    expression is not ExecutionFieldRead { Alias: { Length: > 0 } alias } read
                    || !shapesByAlias.TryGetValue(alias, out var shapes)
                )
                    continue;

                var variable = expressionScope.Find(alias);
                var matchingShapes = variable is null
                    ? shapes
                    : shapes.Where(shape => MatchesVariable(shape, variable)).ToArray();
                ValidateFieldRead(read, matchingShapes.Length > 0 ? matchingShapes : shapes);
            }

            foreach (var child in ExecutionIrAnalysis.GetChildBlocks(node))
                ValidateBlockBindings(child, shapesByAlias, nodeScope);
        }
    }

    private static bool MatchesVariable(RowShape shape, ExecutionVariable variable)
    {
        if (variable.GeneratedRowTypeName is { Length: > 0 } generatedType)
            return shape switch
            {
                GeneratedRowShape generated => generated.TypeName == generatedType,
                TableRowShape table => table.GeneratedRowTypeName == generatedType,
                ValuesRowShape values => values.GeneratedShape.TypeName == generatedType,
                SourceEntityShape source => source.GeneratedTypeName == generatedType,
                _ => false,
            };

        return shape switch
        {
            TableRowShape => variable.Type == TableRowType,
            SourceEntityShape source => variable.Type == source.EntityType || variable.Type == ObjectType,
            _ => false,
        };
    }

    private sealed class SourceBindingScope(SourceBindingScope? parent)
    {
        private readonly Dictionary<string, ExecutionVariable> _variables = new(StringComparer.OrdinalIgnoreCase);

        public void Declare(ExecutionVariable variable) => _variables[variable.Name] = variable;

        public ExecutionVariable? Find(string alias) =>
            _variables.TryGetValue(alias, out var variable) ? variable : parent?.Find(alias);
    }
}
