using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using IrNodes = Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder : IExpressionVisitor
{
    private readonly ExpressionConverter _converter;
    private readonly Stack<LogicalNode> _nodeStack = new();
    private readonly Stack<int> _multiStatementBaseDepths = new();

    private readonly List<ProjectedField> _projectedFields = [];
    private readonly List<OrderField> _orderFields = [];
    private readonly List<WindowRegistration> _windowRegistrations = [];
    private readonly List<IrNodes.CteDefinition> _cteDefinitions = [];
    private readonly Dictionary<string, WindowSpecificationNode> _windowDefinitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RefreshMethodCapture> _refreshMethods = [];

    private bool _selectVisited;
    private bool _selectIsDistinct;

    private readonly IReadOnlyDictionary<string, ISchemaColumn[]> _inferredColumns;
    private readonly Func<string> _subqueryNameGenerator;
    private int _subqueryIndex;

    private IrExpression? _havingPredicate;
    private IrExpression? _qualifyPredicate;
    private PagingCount? _skipValue;
    private PagingCount? _takeValue;

    public LogicalPlanBuilder(IReadOnlyDictionary<string, ISchemaColumn[]> inferredColumns)
        : this(inferredColumns, null)
    {
    }

    private LogicalPlanBuilder(IReadOnlyDictionary<string, ISchemaColumn[]> inferredColumns, Func<string>? subqueryNameGenerator)
    {
        _inferredColumns = inferredColumns ?? throw new ArgumentNullException(nameof(inferredColumns));
        _subqueryNameGenerator = subqueryNameGenerator ?? (() => $"Subquery{_subqueryIndex++}");
        var metadata = new LogicalColumnMetadata(inferredColumns);
        _converter = new ExpressionConverter(RegisterWindowFunction, metadata.ResolveColumnStability, metadata.ResolveColumnEnumType,
            BuildSubquery, _subqueryNameGenerator);
    }

    public LogicalPlanBuilder() : this(new Dictionary<string, ISchemaColumn[]>())
    {
    }

    private LogicalNode BuildSubquery(Node node)
    {
        var builder = new LogicalPlanBuilder(_inferredColumns, _subqueryNameGenerator);
        node.Accept(new LogicalPlanBuildTraverseVisitor(builder));
        return builder.Result ?? throw new InvalidOperationException("Subquery did not produce a logical plan.");
    }

    public LogicalNode? Result => _nodeStack.Count > 0 ? _nodeStack.Peek() : null;

}
