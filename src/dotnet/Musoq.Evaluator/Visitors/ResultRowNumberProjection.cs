using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Plugins;

namespace Musoq.Evaluator.Visitors;

internal sealed class ResultRowNumberProjection : CloneQueryVisitor
{
    internal sealed class QualifiedPositionToken(TextSpan span) : FunctionToken(nameof(LibraryBase.RowNumber), span);

    private const string SourceAlias = "__musoqNumbered";
    private readonly List<FieldNode> _inputs = [];
    private AccessColumnNode? _position;

    internal static bool IsRowNumber(AccessMethodNode call) =>
        string.Equals(call.Name, nameof(LibraryBase.RowNumber), StringComparison.OrdinalIgnoreCase)
        && call.Arguments.Args.Length == 0
        && (
            call.Method is null
            || call.Method.DeclaringType == typeof(LibraryBase) && call.Method.ReturnType == typeof(int)
        );

    internal static bool ContainsRowNumber(Node node) =>
        node is not (WindowFunctionNode or ScalarSubqueryNode or ExistsQueryNode)
        && (
            node is AccessMethodNode method && IsRowNumber(method)
            || ParserNodeTraversalRegistry.EnumerateChildren(node).Any(ContainsRowNumber)
        );

    internal static void Split(List<Node> statements, IntermediateSourceRegistry sources)
    {
        if (statements[^1] is not DetailedQueryNode query)
            return;
        var fields = query.Select.Fields;
        var uses = fields.Where(field => ContainsRowNumber(field.Expression)).ToHashSet();
        if (
            uses.Count == 0
            || uses.Count == 1 && uses.First().Expression is AccessMethodNode direct && IsRowNumber(direct)
        )
            return;

        if (query.OrderBy is not null || query.Qualify is not null)
        {
            var ranked = new OrderedPositionProjection(query.OrderBy, query.Qualify is not null);
            var visitor = new CloneTraverseVisitor(ranked);
            var rankedFields = fields
                .Select(field => uses.Contains(field) ? ranked.Rewrite(field, visitor) : field)
                .ToArray();
            statements[^1] = new DetailedQueryNode(
                new SelectNode(rankedFields, query.Select.IsDistinct),
                query.From,
                query.Where,
                query.GroupBy,
                query.OrderBy,
                query.Skip,
                query.Take,
                query.ReturnVariableName,
                query.Window,
                query.Qualify
            );
            return;
        }

        // Expose one ordinal to the existing sort and paging operations. The outer
        // projection reads that final position for every occurrence in the result.
        var projection = new ResultRowNumberProjection();
        var traverser = new CloneTraverseVisitor(projection);
        var output = fields.Select(field => projection.RewriteField(field, uses.Contains(field), traverser)).ToArray();
        var numbered = new DetailedQueryNode(
            new SelectNode([.. projection._inputs]),
            query.From,
            query.Where,
            query.GroupBy,
            query.OrderBy,
            query.Select.IsDistinct ? null : query.Skip,
            query.Select.IsDistinct ? null : query.Take,
            query.ReturnVariableName,
            query.Window,
            query.Qualify
        );
        sources.Register(numbered, SourceAlias);
        var from = new Parser.InMemoryGroupedFromNode(SourceAlias) { IntermediateSource = sources.Get(numbered) };
        statements[^1] = numbered;
        statements.Add(
            new DetailedQueryNode(
                new SelectNode(output, query.Select.IsDistinct),
                new Parser.ExpressionFromNode(from),
                null,
                null,
                null,
                query.Select.IsDistinct ? query.Skip : null,
                query.Select.IsDistinct ? query.Take : null,
                query.ReturnVariableName
            )
        );
    }

    private FieldNode RewriteField(FieldNode field, bool usesPosition, CloneTraverseVisitor traverser)
    {
        Node value;
        if (usesPosition)
        {
            field.Expression.Accept(traverser);
            value = Nodes.Pop();
        }
        else
            value = Capture(field.Expression);
        return new FieldNode(value, field.FieldOrder, field.FieldName, field.HasExplicitFieldName);
    }

    public override void Visit(AccessMethodNode node)
    {
        if (!IsRowNumber(node))
        {
            base.Visit(node);
            return;
        }
        Nodes.Pop();
        _position ??= Capture(node);
        Nodes.Push(_position);
    }

    public override void Visit(AccessColumnNode node) => Nodes.Push(Capture(node));

    public override void Visit(WindowFunctionNode node) => Nodes.Push(Capture(node));

    private AccessColumnNode Capture(Node expression)
    {
        var index = _inputs.Count;
        var name = $"__numberInput{index}";
        _inputs.Add(new FieldNode(expression, index, name));
        return new AccessColumnNode(name, SourceAlias, expression.ReturnType!, expression.Span);
    }

    // Compute the position while sorted source rows are available. Their columns
    // are read by the original projection, including its conditional branches.
    private sealed class OrderedPositionProjection(OrderByNode? orderBy, bool afterQualify) : CloneQueryVisitor
    {
        private static readonly System.Reflection.MethodInfo WindowRowNumber = (
            (Func<long>)new LibraryBase().RowNumber
        ).Method;

        internal FieldNode Rewrite(FieldNode field, CloneTraverseVisitor traverser)
        {
            field.Accept(traverser);
            return (FieldNode)Nodes.Pop();
        }

        public override void Visit(AccessMethodNode node)
        {
            if (!IsRowNumber(node))
            {
                base.Visit(node);
                return;
            }

            Nodes.Pop();
            var window = new WindowFunctionNode(
                new AccessMethodNode(
                    afterQualify ? new QualifiedPositionToken(node.Span) : node.FunctionToken,
                    ArgsListNode.Empty,
                    null,
                    false,
                    WindowRowNumber,
                    node.Alias
                ),
                new WindowSpecificationNode([], orderBy?.Fields ?? [])
            );
            window.SetReturnType(typeof(long));
            Nodes.Push(new CastNode(window, "Int32", typeof(int)));
        }
    }
}
