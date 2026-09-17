using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    /// <summary>
    ///     Resolves a complete bare CTE identifier for a datasource argument. Qualified
    ///     columns and identifiers nested in a record or collection remain ordinary
    ///     expressions and are intentionally not resolved here.
    /// </summary>
    private CteRelationBinding? ResolveCteRelation(Node node)
    {
        if (node is not IdentifierNode identifier)
            return null;

        var scalarColumn = ResolveVisibleScalarColumn(identifier.Name);
        var scope = _sourceBinding.CurrentScope;
        while (scope != null)
        {
            if (scope.Name == "CTE" &&
                scope.ScopeSymbolTable.TryGetSymbol<TableSymbol>(identifier.Name, out var symbol))
            {
                var table = symbol.GetTableByAlias(identifier.Name).Table;
                if (table.Columns.Length == 0)
                    return null;

                var shape = CteRelationShapeFactory.CreateRecordShape(table.Columns);
                return new CteRelationBinding(identifier.Name, table.Columns, shape, scalarColumn?.ColumnType);
            }

            scope = scope.Parent;
        }

        return null;
    }

    private ISchemaColumn? ResolveVisibleScalarColumn(string name)
    {
        var scope = _sourceBinding.CurrentScope;
        while (scope != null)
        {
            // CTE declarations define relations, not unqualified columns in the
            // surrounding query. Only already-bound FROM/JOIN table symbols are
            // candidates for the scalar interpretation.
            if (!string.Equals(scope.Name, "CTE", StringComparison.Ordinal))
            {
                ISchemaColumn? match = null;
                foreach (var tableSymbol in scope.ScopeSymbolTable.GetSymbols<TableSymbol>())
                {
                    var columns = tableSymbol.FullTable.GetColumnsByName(name);
                    if (columns.Length == 0)
                        continue;

                    // More than one visible column is not a valid scalar
                    // interpretation; normal column binding will report its own
                    // ambiguity when the expression is used directly.
                    if (columns.Length != 1 || match != null)
                        return null;

                    match = columns[0];
                }

                if (match != null)
                    return match;
            }

            scope = scope.Parent;
        }

        return null;
    }
}
