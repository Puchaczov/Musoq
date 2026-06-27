using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private static ColumnRef ConvertColumnAccess(AccessColumnNode node)
    {
        var (alias, name) = NormalizeAccessColumn(node);
        return new ColumnRef(alias, name, RequireReturnType(node));
    }
}
