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

    private IrExpression? _havingPredicate;
    private IrExpression? _qualifyPredicate;
    private int? _skipValue;
    private int? _takeValue;

    public LogicalPlanBuilder(IReadOnlyDictionary<string, ISchemaColumn[]> inferredColumns)
    {
        _inferredColumns = inferredColumns ?? throw new ArgumentNullException(nameof(inferredColumns));
        _converter = new ExpressionConverter(RegisterWindowFunction);
    }

    public LogicalPlanBuilder()
    {
        _inferredColumns = new Dictionary<string, ISchemaColumn[]>();
        _converter = new ExpressionConverter(RegisterWindowFunction);
    }

    public LogicalNode? Result => _nodeStack.Count > 0 ? _nodeStack.Peek() : null;
}
