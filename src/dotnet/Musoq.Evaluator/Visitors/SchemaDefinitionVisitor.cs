using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Evaluator.Visitors.Helpers;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Visitor that extracts schema definitions from the AST and registers them in a SchemaRegistry.
///     This visitor processes the AST before query execution to collect all schema definitions.
/// </summary>
public class SchemaDefinitionVisitor : NoOpExpressionVisitor
{
    /// <summary>
    ///     Creates a new schema definition visitor.
    /// </summary>
    /// <param name="registry">The registry to populate with schema definitions.</param>
    public SchemaDefinitionVisitor(SchemaRegistry registry)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    ///     Gets the schema registry populated by this visitor.
    /// </summary>
    public SchemaRegistry Registry { get; }

    /// <summary>
    ///     Visits a binary schema node and registers it.
    /// </summary>
    public override void Visit(BinarySchemaNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Registry.Register(node.Name, node);


        var typeParameters = new HashSet<string>(node.TypeParameters);
        var declaredFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddInheritedFields(node.Extends, declaredFields);

        foreach (var field in node.Fields)
            if (field is FieldDefinitionNode parsedField)
            {
                ValidateTypeReferences(parsedField.TypeAnnotation, node.Name, typeParameters);

                if (parsedField.WhenCondition != null)
                {
                    var identifiers = new IdentifierCollector();
                    new IdentifierCollectorTraverse(identifiers).Traverse(parsedField.WhenCondition);
                    foreach (var identifier in identifiers.Names)
                        if (!declaredFields.Contains(identifier))
                            throw new QuerySyntaxException(
                                $"Binary schema '{node.Name}' field '{parsedField.Name}' references unknown field '{identifier}'.",
                                parsedField.WhenCondition.SpanOrEmpty());
                }

                declaredFields.Add(parsedField.Name);
            }
            else
                declaredFields.Add(field.Name);
    }

    /// <summary>
    ///     Visits a text schema node and registers it.
    /// </summary>
    public override void Visit(TextSchemaNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Registry.Register(node.Name, node);
    }

    private void ValidateTypeReferences(TypeAnnotationNode typeNode, string currentSchemaName,
        HashSet<string> typeParameters)
    {
        switch (typeNode)
        {
            case SchemaReferenceTypeNode refNode:

                if (!typeParameters.Contains(refNode.SchemaName))
                    Registry.ValidateReference(refNode.SchemaName, currentSchemaName);

                foreach (var typeArgument in refNode.TypeArguments)
                    foreach (var dependency in InterpretationSchemaTypeDependencyExtractor.Extract(
                                 typeArgument,
                                 typeParameters))
                        Registry.ValidateReference(dependency, currentSchemaName);
                break;

            case ArrayTypeNode arrayNode:
                ValidateTypeReferences(arrayNode.ElementType, currentSchemaName, typeParameters);
                break;
        }
    }

    private void AddInheritedFields(string? parentName, HashSet<string> fields)
    {
        if (string.IsNullOrWhiteSpace(parentName) ||
            !Registry.TryGetSchema(parentName, out var registration) ||
            registration?.Node is not BinarySchemaNode parent)
            return;

        AddInheritedFields(parent.Extends, fields);
        foreach (var field in parent.Fields)
            fields.Add(field.Name);
    }

    private sealed class IdentifierCollector : NoOpExpressionVisitor
    {
        public HashSet<string> Names { get; } = new(StringComparer.OrdinalIgnoreCase);

        public override void Visit(IdentifierNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            Names.Add(node.Name);
        }

        public override void Visit(AccessColumnNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            Names.Add(node.Name);
        }
    }

    private sealed class IdentifierCollectorTraverse(IdentifierCollector visitor)
        : RawTraverseVisitor<IdentifierCollector>(visitor)
    {
        public override void Visit(DotNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            node.Root.Accept(this);
        }

        public void Traverse(Node node)
        {
            ArgumentNullException.ThrowIfNull(node);
            node.Accept(this);
        }
    }
}
