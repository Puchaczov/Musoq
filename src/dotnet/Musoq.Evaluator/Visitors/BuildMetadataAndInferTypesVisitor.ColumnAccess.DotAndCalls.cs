using System.Dynamic;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;
using NotSupportedException = System.NotSupportedException;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(DotNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var exp = SafePop(Nodes, VisitorOperationNames.VisitDotNodeExpression);
        var root = SafePop(Nodes, VisitorOperationNames.VisitDotNodeRoot);

        if (root?.ReturnType == null)
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                VisitorOperationNames.VisitDotNode,
                "Root node has no return type for dot access");


        if (root is AccessColumnNode accessColumnNode && exp is AccessObjectArrayNode { IsColumnAccess: false } arrayNode2)
        {
            var tableSymbol = _sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(accessColumnNode.Alias);
            if (tableSymbol != null)
            {
                var column = tableSymbol.GetColumnByAliasAndName(accessColumnNode.Alias, arrayNode2.ObjectName);
                if (column != null && IsIndexableType(column.ColumnType))
                {
                    var elementIntendedTypeName = GetArrayElementIntendedTypeName(column.IntendedTypeName);
                    var columnAccessArrayNode =
                        new AccessObjectArrayNode(arrayNode2.Token, column.ColumnType, accessColumnNode.Alias,
                            elementIntendedTypeName);
                    Nodes.Push(columnAccessArrayNode);
                    return;
                }
            }
        }


        if (root is IdentifierNode identifierRoot && exp is AccessObjectArrayNode { IsColumnAccess: false } arrayNode3)
            if (_sourceBinding.CurrentScope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(identifierRoot.Name))
            {
                var tableSymbol = _sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifierRoot.Name);
                if (tableSymbol != null)
                {
                    var column = tableSymbol.GetColumnByAliasAndName(identifierRoot.Name, arrayNode3.ObjectName);
                    if (column != null && IsIndexableType(column.ColumnType))
                    {
                        var elementIntendedTypeName = GetArrayElementIntendedTypeName(column.IntendedTypeName);
                        var columnAccessArrayNode =
                            new AccessObjectArrayNode(arrayNode3.Token, column.ColumnType, identifierRoot.Name,
                                elementIntendedTypeName);
                        Nodes.Push(columnAccessArrayNode);
                        return;
                    }
                }
            }

        DotNode newNode;


        var isNestedSchemaReference = root.ReturnType == typeof(object) &&
                                      ((root is AccessColumnNode { IntendedTypeName: not null } accessColRootCheck &&
                                        !string.IsNullOrEmpty(accessColRootCheck.IntendedTypeName)) ||
                                       (root is AccessObjectArrayNode { IntendedTypeName: not null } arrayRootCheck &&
                                        !string.IsNullOrEmpty(arrayRootCheck.IntendedTypeName)) ||
                                       (root is DotNode { IntendedTypeName: not null } dotRootCheck &&
                                        !string.IsNullOrEmpty(dotRootCheck.IntendedTypeName)));


        var rootIntendedTypeName = root switch
        {
            AccessColumnNode accessColRoot => accessColRoot.IntendedTypeName,
            AccessObjectArrayNode arrayRoot => arrayRoot.IntendedTypeName,
            DotNode dotRoot => dotRoot.IntendedTypeName,
            _ => null
        };


        if (root.ReturnType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
        {
            newNode = new DotNode(root, exp, node.IsTheMostInner, string.Empty, exp.ReturnType);
        }

        else if (isNestedSchemaReference)
        {
            var expressionNode = exp;
            string? childIntendedTypeName = null;

            if (exp is IdentifierNode identNode)
            {
                var propertyType = typeof(object);


                if (SchemaRegistry != null && !string.IsNullOrEmpty(rootIntendedTypeName))
                {
                    var resolvedProperty = ResolveSchemaFieldFromIntendedTypeName(rootIntendedTypeName, identNode.Name);
                    if (resolvedProperty != null)
                        (propertyType, childIntendedTypeName) = resolvedProperty.Value;
                }

                expressionNode = new PropertyValueNode(identNode.Name,
                    new ExpandoObjectPropertyInfo(identNode.Name, propertyType));
            }


            newNode = new DotNode(root, expressionNode, node.IsTheMostInner, string.Empty, expressionNode.ReturnType,
                childIntendedTypeName);
        }

        else if (root.ReturnType == typeof(object))
        {
            var expressionNode = exp;

            if (exp is IdentifierNode identNode)
                expressionNode = new PropertyValueNode(identNode.Name,
                    new ExpandoObjectPropertyInfo(identNode.Name, typeof(object)));

            newNode = new DotNode(root, expressionNode, node.IsTheMostInner, string.Empty, expressionNode.ReturnType);
        }
        else
        {
            if (exp is AccessObjectArrayNode arrayNode)
            {
                var propertyName = arrayNode.ObjectName;
                var property = root.ReturnType.GetProperty(propertyName);

                if (property == null)
                {
                    if (TryReportUnknownPropertyWithSuggestions(propertyName, root.ReturnType.GetProperties(), node))
                        return;
                    var span = node.SpanOrEmpty();
                    PrepareAndThrowUnknownPropertyExceptionMessage(propertyName,
                        root.ReturnType.GetProperties(), span);
                }

                newNode = new DotNode(root, exp, node.IsTheMostInner, string.Empty, exp.ReturnType);
            }
            else if (exp is IdentifierNode identifierNode)
            {
                var hasProperty = root.ReturnType.GetProperty(identifierNode.Name) != null;

                if (!hasProperty)
                {
                    if (TryReportUnknownPropertyWithSuggestions(identifierNode.Name, root.ReturnType.GetProperties(),
                            node))
                        return;
                    var span = node.SpanOrEmpty();
                    PrepareAndThrowUnknownPropertyExceptionMessage(identifierNode.Name,
                        root.ReturnType.GetProperties(), span);
                }

                newNode = new DotNode(root, exp, node.IsTheMostInner, string.Empty, exp.ReturnType);
            }
            else
            {
                var dotSpan = node.SpanOrEmpty();
                throw new NotSupportedException(
                    $"Unsupported expression type in property access at position {dotSpan.Start}: {exp?.GetType().Name ?? "null"}. Check the query syntax near this location.");
            }
        }

        Nodes.Push(newNode);
    }

    public override void Visit(AccessCallChainNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var chainPretend = SafePop(Nodes, VisitorOperationNames.VisitAccessCallChainNode);

        Nodes.Push(chainPretend is AccessColumnNode
            ? chainPretend
            : new AccessCallChainNode(node.ColumnName, node.ReturnType, node.Props, node.Alias));
    }

    public override void Visit(ArgsListNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var args = new Node[node.Args.Length];

        for (var i = node.Args.Length - 1; i >= 0; --i)
            args[i] = SafePop(Nodes, VisitorOperationNames.VisitArgsListNode);

        Nodes.Push(new ArgsListNode(args));
    }
}
