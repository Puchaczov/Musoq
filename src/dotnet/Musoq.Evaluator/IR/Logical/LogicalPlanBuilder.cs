using System.Collections.Generic;
using System.Linq;
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
        _converter = new ExpressionConverter(RegisterWindowFunction, ResolveColumnStability, ResolveColumnEnumType);
    }

    public LogicalPlanBuilder()
    {
        _inferredColumns = new Dictionary<string, ISchemaColumn[]>();
        _converter = new ExpressionConverter(RegisterWindowFunction);
    }

    private ColumnStability ResolveColumnStability(string alias, string columnName)
    {
        if (!string.IsNullOrWhiteSpace(alias) && _inferredColumns.TryGetValue(alias, out var aliasedColumns))
            return FindColumnStability(aliasedColumns, columnName);

        var matches = _inferredColumns.Values
            .SelectMany(static columns => columns)
            .Where(column => string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return matches.Length == 1 ? matches[0].Stability : ColumnStability.Stable;
    }

    private static ColumnStability FindColumnStability(
        IEnumerable<ISchemaColumn> columns,
        string columnName)
    {
        return columns.FirstOrDefault(column =>
                   string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))?.Stability
               ?? ColumnStability.Stable;
    }

    private EnumTypeDescriptor? ResolveColumnEnumType(string alias, string columnName)
    {
        if (!string.IsNullOrWhiteSpace(alias) && _inferredColumns.TryGetValue(alias, out var aliasedColumns))
            return FindColumnEnumType(aliasedColumns, columnName);

        var matches = _inferredColumns.Values
            .SelectMany(static columns => columns)
            .Where(column => string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return matches.Length == 1 ? matches[0].EnumType : null;
    }

    private static EnumTypeDescriptor? FindColumnEnumType(
        IEnumerable<ISchemaColumn> columns,
        string columnName)
    {
        return columns.FirstOrDefault(column =>
            string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))?.EnumType;
    }

    public LogicalNode? Result => _nodeStack.Count > 0 ? _nodeStack.Peek() : null;

}
