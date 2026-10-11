using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    private bool TryVisitNamedObjectKey(DotNode node, IdentifierNode identifier)
    {
        if (node.Expression is not AccessObjectKeyNode key)
            return false;

        var table = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifier.Name);
        var column = table?.GetColumnByAliasAndName(identifier.Name, key.ObjectName, key.SpanOrEmpty());
        if (column == null)
            return false;

        var access = new AccessObjectKeyNode(key.Token, column.ColumnType, identifier.Name)
        {
            DestinationKind = key.DestinationKind
        };
        access.Accept(Visitor);
        return true;
    }
}
