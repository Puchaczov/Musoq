using System.Collections.Generic;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor(IAwareExpressionVisitor visitor)
    : InterpretationSchemaDefinitionSkippingTraverseVisitor<IAwareExpressionVisitor>(visitor), IQueryPartAwareExpressionVisitor
{
    private readonly Stack<Scope> _scopes = new();

    private IdentifierNode? _theMostInnerIdentifier;

    public Scope Scope { get; private set; } = new(null, -1, "Root");

    public override void Visit(GroupSelectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public override void Visit(RowPresenceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public override void Visit(DotNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var self = node;
        var theMostOuter = self;
        while (self is not null)
        {
            theMostOuter = self;
            self = self.Root as DotNode;
        }

        var ident = theMostOuter.Root as IdentifierNode;

        if (ident != null && node == theMostOuter && Scope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(ident.Name))
        {
            if (theMostOuter.Expression is AccessObjectArrayNode arrayNode)
            {
                var tableSymbol = Scope.ScopeSymbolTable.GetSymbol<TableSymbol>(ident.Name);
                var columnInfo = tableSymbol?.GetColumnByAliasAndName(ident.Name, arrayNode.ObjectName);

                if (columnInfo != null)
                {
                    string? elementIntendedTypeName = null;
                    if (!string.IsNullOrEmpty(columnInfo.IntendedTypeName) &&
                        columnInfo.IntendedTypeName.EndsWith("[]", StringComparison.Ordinal))
                        elementIntendedTypeName =
                            columnInfo.IntendedTypeName.Substring(0, columnInfo.IntendedTypeName.Length - 2);

                    var enhancedArrayNode = new AccessObjectArrayNode(
                        arrayNode.Token,
                        columnInfo.ColumnType,
                        ident.Name,
                        elementIntendedTypeName
                    );
                    enhancedArrayNode.Accept(Visitor);
                    return;
                }
            }

            IdentifierNode? column = null;
            if (theMostOuter.Expression is DotNode dotNode)
                column = dotNode.Root as IdentifierNode;
            else
                column = theMostOuter.Expression as IdentifierNode;

            if (column != null)
            {
                Visit(new AccessColumnNode(column.Name, ident.Name, node.Span));
                return;
            }
        }

        if (ident != null && node == theMostOuter &&
            !Scope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(ident.Name) &&
            !Visitor.IsCurrentContextColumn(ident.Name))
        {
            var column = theMostOuter.Expression as IdentifierNode;
            if (column != null)
            {
                Visit(new AccessColumnNode(column.Name, ident.Name,
                    ident.SpanOrEmpty()));
                return;
            }
        }

        var setTheMostInnerIdentifier = false;
        if (_theMostInnerIdentifier is null)
        {
            _theMostInnerIdentifier = node.Expression as IdentifierNode;
            if (_theMostInnerIdentifier != null) setTheMostInnerIdentifier = true;
        }

        if (_theMostInnerIdentifier is not null && setTheMostInnerIdentifier)
            Visitor.SetTheMostInnerIdentifierOfDotNode(_theMostInnerIdentifier);

        self = node;

        while (self is not null)
        {
            self.Root.Accept(this);
            self.Expression.Accept(this);
            self.Accept(Visitor);

            self = self.Expression as DotNode;
        }

        if (_theMostInnerIdentifier is not null && setTheMostInnerIdentifier)
        {
            Visitor.SetTheMostInnerIdentifierOfDotNode(null);
            _theMostInnerIdentifier = null;
        }
    }
}
