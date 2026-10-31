using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class SubqueryBindingScope(SemanticAnalysisState state)
{
    private const string VisitorName = "BuildMetadataAndInferTypesVisitor";
    private SourceBindingState _sourceBinding => state.SourceBinding;
    private int _subqueryBindingDepth;
    private int _querySequence;
    internal int NextQueryKey(int current) => _querySequence = Math.Max(_querySequence, current) + 1;
    internal Node[] Captures => _subqueryCaptureBindings.Peek().ToArray();
    internal bool HasCurrentProcessedQueryId() => _subqueryBindingDepth > 0
        ? _sourceBinding.CurrentScope.SnapshotAttributes().ContainsKey(MetaAttributes.ProcessedQueryId)
        : _sourceBinding.CurrentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId);
    internal static bool ContainsNullableSubquery(Node node) =>
        node is SubqueryApplyNode apply && Nullable.GetUnderlyingType(apply.ReturnType) is not null ||
        ParserNodeChildTraversal.EnumerateChildren(node).Any(ContainsNullableSubquery);
    private readonly Stack<List<Node>> _subqueryCaptureBindings = new();
    private readonly Stack<IReadOnlyDictionary<string, (int SchemaFromKey, string SourceContextId)>> _subqueryOuterIdentities = new();
    private readonly Stack<IReadOnlySet<string>> _subqueryOuterAliases = new();

    internal void BindSubqueryScope(Action bind)
    {
        using var frame = new SubqueryBindingFrame(state);
        _subqueryBindingDepth++;
        _subqueryCaptureBindings.Push([]);
        _subqueryOuterIdentities.Push(frame.Identities);
        _subqueryOuterAliases.Push(frame.VisibleAliases);
        try
        {
            bind();
        }
        finally
        {
            _subqueryBindingDepth--;
            _subqueryCaptureBindings.Pop();
            _subqueryOuterIdentities.Pop();
            _subqueryOuterAliases.Pop();
        }
    }

    internal SubqueryParameterNode CaptureSubqueryOuterColumn(AccessColumnNode column)
    {
        var aliases = _subqueryOuterAliases.ToArray();
        var owner = Array.FindIndex(aliases, visible => visible.Contains(column.Alias));
        return CaptureSubqueryOuterColumn(column, _subqueryCaptureBindings.ToArray(), 0, Math.Max(0, owner));
    }

    private static SubqueryParameterNode CaptureSubqueryOuterColumn(AccessColumnNode column, List<Node>[] scopes, int scope, int owner)
    {
        Node value = column;
        if (scope < owner)
            value = CaptureSubqueryOuterColumn(column, scopes, scope + 1, owner);
        var bindings = scopes[scope];
        var index = bindings.FindIndex(binding => binding is AccessColumnNode candidate &&
            value is AccessColumnNode expected && candidate.Alias == expected.Alias && candidate.Name == expected.Name &&
            candidate.ReturnType == expected.ReturnType);
        if (index < 0)
        {
            index = bindings.Count;
            bindings.Add(value);
        }
        return new SubqueryParameterNode(index, column.ReturnType, column.Span, column.ToString());
    }

    internal int ResolveOuterSubquerySourceKey(string alias)
    {
        foreach (var identities in _subqueryOuterIdentities)
            if (identities.TryGetValue(alias, out var identity))
                return identity.SchemaFromKey;
        return _sourceBinding.SchemaFromKey;
    }

    internal string? ResolveUnqualifiedSubqueryOuterAlias(AccessColumnNode column)
    {
        foreach (var aliases in _subqueryOuterAliases)
        {
            var matches = aliases.Where(alias => ResolveSubqueryOuterTable(alias)?.GetTableByAlias(alias).Table
                .GetColumnsByName(column.Name).Length > 0).ToArray();
            if (matches.Length > 1)
                throw new Musoq.Evaluator.Exceptions.AmbiguousColumnException(column.Name, matches[0], matches[1], column.SpanOrEmpty());
            if (matches.Length == 1)
                return matches[0];
        }
        return null;
    }

    internal Musoq.Evaluator.Utils.Symbols.TableSymbol? ResolveSubqueryOuterTable(string alias)
    {
        if (_subqueryBindingDepth == 0 || string.IsNullOrEmpty(alias))
            return null;
        var scope = _sourceBinding.CurrentScope.Parent;
        while (scope is not null)
        {
            if (scope.ScopeSymbolTable.TryGetSymbol<Musoq.Evaluator.Utils.Symbols.TableSymbol>(alias, out var table))
                return table;
            scope = scope.Parent;
        }
        return null;
    }

    internal SubqueryApplyNode BindResult(ScalarSubqueryNode node, Node query, Node? operand,
        Action<Node, Node, BinaryOperatorKind, Node> validate)
    {
        if (node is not SubqueryApplyNode apply)
            throw new InvalidOperationException("Scalar subqueries must have a selected lowering strategy before binding.");
        var fields = GetSubqueryFields(query);
        if (apply.Kind == SubqueryApplyKind.In && fields.Length != 1)
            throw new Musoq.Evaluator.Exceptions.VisitorException(VisitorName, "IN subquery validation",
                "Subquery used with IN must return exactly one column.",
                Musoq.Parser.Diagnostics.DiagnosticCode.MQ3049_InSubqueryMultipleColumns, node.Span);
        if (apply.Kind != SubqueryApplyKind.Exists && fields.Length != 1)
            throw SubqueryDiagnosticFactory.InvalidSubquery("subquery binding", "Subquery must return exactly one column.", node);
        if (apply.Kind is SubqueryApplyKind.In or SubqueryApplyKind.Any or SubqueryApplyKind.All)
            validate(operand!, fields[0].Expression, apply.Comparison switch
            {
                Musoq.Parser.Tokens.TokenType.Equality => BinaryOperatorKind.Equality,
                Musoq.Parser.Tokens.TokenType.Diff => BinaryOperatorKind.Inequality,
                _ => BinaryOperatorKind.Relational
            }, node);
        var type = apply.Kind switch
        {
            SubqueryApplyKind.Exists => typeof(bool),
            SubqueryApplyKind.In or SubqueryApplyKind.Any or SubqueryApplyKind.All => typeof(bool?),
            _ => LiftScalarSubqueryType(fields[0].ReturnType ?? typeof(object))
        };
        return new SubqueryApplyNode(query, apply.Kind, operand, type, Captures) { Comparison = apply.Comparison }.CopySpansFrom(node);
    }

    internal static FieldNode[] GetSubqueryFields(Node node) => node switch
    {
        QueryNode query => query.Select.Fields,
        SingleSetNode set => set.Query.Select.Fields,
        SetOperatorNode set => GetSubqueryFields(set.Left),
        CteExpressionNode cte => GetSubqueryFields(cte.OuterExpression),
        _ => throw new NotSupportedException($"Unsupported bound subquery body '{node.GetType().Name}'.")
    };

    private static Type LiftScalarSubqueryType(Type type) =>
        type.IsValueType && Nullable.GetUnderlyingType(type) is null ? typeof(Nullable<>).MakeGenericType(type) : type;
}
