using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private static RowPresence ConvertRowPresence(RowPresenceNode node)
    {
        if (node.Expression is not IdentifierNode identifier)
            throw new InvalidOperationException("Row presence predicates require a source alias identifier.");

        return new RowPresence(identifier.Name, node.IsPresent, RequireReturnType(node));
    }
}
