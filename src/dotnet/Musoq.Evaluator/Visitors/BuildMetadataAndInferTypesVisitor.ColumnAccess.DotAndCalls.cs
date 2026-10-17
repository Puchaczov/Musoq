using System.Dynamic;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;
using NotSupportedException = System.NotSupportedException;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(DotNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var exp = PopSemanticNode(VisitorOperationNames.VisitDotNodeExpression);
        var root = PopSemanticNode(VisitorOperationNames.VisitDotNodeRoot);
        if (StructuralProjectionBinding.Dot(root, exp, node) is { } structural)
        {
            PushSemanticNode(structural);
            return;
        }

        // A reported child-binding error is represented by a node with no
        // return type. Preserve the parent shape so traversal cannot turn the
        // original bind error into a stack or cast failure.
        if (exp.ReturnType == null && DiagnosticContext?.HasErrors == true)
        {
            PushSemanticNode(new DotNode(root, exp, node.IsTheMostInner, node.Name));
            return;
        }

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
                    var columnAccessArrayNode = _columnPropertyBindingService.TryCreateColumnArrayAccess(
                        tableSymbol,
                        accessColumnNode.Alias,
                        arrayNode2,
                        accessColumnNode.Alias);
                    if (columnAccessArrayNode != null)
                    {
                        PushSemanticNode(columnAccessArrayNode);
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
                        var columnAccessArrayNode = _columnPropertyBindingService.TryCreateColumnArrayAccess(
                            tableSymbol,
                            identifierRoot.Name,
                            arrayNode3,
                            identifierRoot.Name);
                        if (columnAccessArrayNode != null)
                        {
                            PushSemanticNode(columnAccessArrayNode);
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


        if (root.ReturnType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)) || isNestedSchemaReference)
        {
            newNode = InterpretationPropertyBinding.BindDynamicDotNode(root, exp, node, ResolveSchemaFieldFromIntendedTypeName);
        }

        else if (root.ReturnType == typeof(object))
        {
            newNode = _columnPropertyBindingService.CreateObjectBackedDotNode(root, exp, node);
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
                    {
                        PushSemanticNode(CreateUnknownPropertyRecoveryNode(root, propertyName, node));
                        return;
                    }
                    var span = node.SpanOrEmpty();
                    PrepareAndThrowUnknownPropertyExceptionMessage(propertyName,
                        root.ReturnType.GetProperties(), span);
                }

                newNode = new DotNode(root, exp, node.IsTheMostInner, string.Empty, exp.ReturnType);
            }
            else if (exp is IdentifierNode identifierNode)
            {
                var hasProperty = _columnPropertyBindingService.TryResolveTypedProperty(
                    root.ReturnType, identifierNode.Name, out _) != null;

                if (!hasProperty)
                {
                    if (TryReportUnknownPropertyWithSuggestions(identifierNode.Name, root.ReturnType.GetProperties(),
                            node))
                    {
                        PushSemanticNode(CreateUnknownPropertyRecoveryNode(root, identifierNode.Name, node));
                        return;
                    }
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

        PushSemanticNode(PreserveInterpretationPathNullability(newNode));
    }

    private static DotNode CreateUnknownPropertyRecoveryNode(Node root, string propertyName, DotNode node) =>
        new(root, new IdentifierNode(propertyName, typeof(object), node.SpanOrEmpty()), node.IsTheMostInner,
            string.Empty, typeof(object));

    public override void Visit(AccessCallChainNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var chainPretend = PopSemanticNode(VisitorOperationNames.VisitAccessCallChainNode);

        PushSemanticNode(chainPretend is AccessColumnNode
            ? chainPretend
            : new AccessCallChainNode(node.ColumnName, node.ReturnType, node.Props, node.Alias));
    }

    public override void Visit(ArgsListNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var args = new Node[node.Args.Length];

        for (var i = node.Args.Length - 1; i >= 0; --i)
            args[i] = PopSemanticNode(VisitorOperationNames.VisitArgsListNode);

        PushSemanticNode(StructuralNodeRebuildSupport.Rebuild(node, args));
    }
}
