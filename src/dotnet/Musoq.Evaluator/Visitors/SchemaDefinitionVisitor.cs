#nullable enable

using System;
using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Nodes.InterpretationSchema;

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
        Registry.Register(node.Name, node);


        var typeParameters = new HashSet<string>(node.TypeParameters);

        foreach (var field in node.Fields)
            if (field is FieldDefinitionNode parsedField)
                ValidateTypeReferences(parsedField.TypeAnnotation, node.Name, typeParameters);
    }

    /// <summary>
    ///     Visits a text schema node and registers it.
    /// </summary>
    public override void Visit(TextSchemaNode node)
    {
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
                break;

            case ArrayTypeNode arrayNode:
                ValidateTypeReferences(arrayNode.ElementType, currentSchemaName, typeParameters);
                break;
        }
    }
}
