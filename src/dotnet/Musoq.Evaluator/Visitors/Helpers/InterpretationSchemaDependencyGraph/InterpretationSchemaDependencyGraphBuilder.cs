using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors.Helpers.InterpretationSchemaDependencyGraph;

/// <summary>
///     Builds dependency graph for interpretation schemas and marks reachable schemas.
/// </summary>
public sealed class InterpretationSchemaDependencyGraphBuilder
{
    public InterpretationSchemaDependencyGraph Build(RootNode queryTree, SchemaRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(queryTree);
        ArgumentNullException.ThrowIfNull(registry);
        var nodes = registry.Schemas.ToDictionary(
            registration => registration.Name,
            registration => new InterpretationSchemaGraphNode(registration.Name, registration),
            StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes.Values)
            foreach (var dependency in ExtractSchemaDependencies(node.Node))
                if (nodes.TryGetValue(dependency, out var dependencyNode))
                {
                    node.Dependencies.Add(dependencyNode.Name);
                    dependencyNode.Dependents.Add(node.Name);
                }

        var directlyUsedSchemaNames = CollectDirectlyUsedSchemaNames(queryTree, nodes.Keys);

        MarkReachable(nodes, directlyUsedSchemaNames);

        return new InterpretationSchemaDependencyGraph(nodes, directlyUsedSchemaNames);
    }

    private static HashSet<string> CollectDirectlyUsedSchemaNames(
        RootNode queryTree,
        IEnumerable<string> knownSchemaNames)
    {
        var usedSchemas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var knownSchemas = new HashSet<string>(knownSchemaNames, StringComparer.OrdinalIgnoreCase);
        var usageVisitor = new InterpretationSchemaUsageVisitor(usedSchemas, knownSchemas);
        var traverseVisitor = new InterpretationSchemaUsageTraverseVisitor(usageVisitor);

        queryTree.Accept(traverseVisitor);

        return usedSchemas;
    }

    private static void MarkReachable(
        Dictionary<string, InterpretationSchemaGraphNode> nodes,
        IEnumerable<string> directlyUsedSchemaNames)
    {
        var pending = new Stack<string>(directlyUsedSchemaNames);

        while (pending.Count > 0)
        {
            var schemaName = pending.Pop();

            if (!nodes.TryGetValue(schemaName, out var node) || node.IsReachable)
                continue;

            node.IsReachable = true;

            foreach (var dependency in node.Dependencies)
                pending.Push(dependency);
        }
    }

    private static IEnumerable<string> ExtractSchemaDependencies(Node node)
    {
        return node switch
        {
            BinarySchemaNode binarySchema => ExtractBinarySchemaDependencies(binarySchema),
            TextSchemaNode textSchema => ExtractTextSchemaDependencies(textSchema),
            _ => []
        };
    }

    private static IEnumerable<string> ExtractBinarySchemaDependencies(BinarySchemaNode schema)
    {
        if (!string.IsNullOrWhiteSpace(schema.Extends))
            yield return schema.Extends;

        var typeParameters = new HashSet<string>(schema.TypeParameters, StringComparer.OrdinalIgnoreCase);

        foreach (var field in schema.Fields)
            if (field is FieldDefinitionNode fieldDefinition)
                foreach (var dependency in ExtractTypeAnnotationDependencies(fieldDefinition.TypeAnnotation, typeParameters))
                    yield return dependency;
    }

    private static IEnumerable<string> ExtractTextSchemaDependencies(TextSchemaNode schema)
    {
        if (!string.IsNullOrWhiteSpace(schema.Extends))
            yield return schema.Extends;

        foreach (var field in schema.Fields)
        {
            if (field.FieldType == TextFieldType.Switch)
            {
                foreach (var switchCase in field.SwitchCases)
                {
                    if (!string.IsNullOrWhiteSpace(switchCase.TypeName))
                        yield return switchCase.TypeName;
                }

                continue;
            }

            if (field.FieldType == TextFieldType.Repeat && !string.IsNullOrWhiteSpace(field.PrimaryValue))
                yield return field.PrimaryValue;

            if (field.FieldType == TextFieldType.SchemaReference && !string.IsNullOrWhiteSpace(field.PrimaryValue))
                yield return field.PrimaryValue;
        }
    }

    private static IEnumerable<string> ExtractTypeAnnotationDependencies(
        TypeAnnotationNode typeAnnotation,
        HashSet<string> typeParameters)
    {
        switch (typeAnnotation)
        {
            case SchemaReferenceTypeNode schemaReference:
            {
                if (!typeParameters.Contains(schemaReference.SchemaName))
                    yield return schemaReference.SchemaName;

                foreach (var typeArgument in schemaReference.TypeArguments)
                    foreach (var dependency in InterpretationSchemaTypeDependencyExtractor.Extract(
                                 typeArgument,
                                 typeParameters))
                        yield return dependency;

                break;
            }
            case ArrayTypeNode arrayType:
            {
                foreach (var dependency in ExtractTypeAnnotationDependencies(arrayType.ElementType, typeParameters))
                    yield return dependency;

                break;
            }
            case StringTypeNode stringType:
            {
                if (!string.IsNullOrWhiteSpace(stringType.AsTextSchemaName))
                    yield return stringType.AsTextSchemaName;

                break;
            }
            case InlineSchemaTypeNode inlineSchema:
            {
                foreach (var field in inlineSchema.Fields)
                    if (field is FieldDefinitionNode fieldDefinition)
                        foreach (var dependency in ExtractTypeAnnotationDependencies(fieldDefinition.TypeAnnotation,
                                     typeParameters))
                            yield return dependency;

                break;
            }
            case RepeatUntilTypeNode repeatUntilType:
            {
                foreach (var dependency in ExtractTypeAnnotationDependencies(repeatUntilType.ElementType, typeParameters))
                    yield return dependency;

                break;
            }
            case BinarySwitchTypeNode switchType:
            {
                foreach (var switchCase in switchType.Cases)
                    foreach (var dependency in ExtractTypeAnnotationDependencies(switchCase.BranchType, typeParameters))
                        yield return dependency;

                break;
            }
            case SubstreamTypeNode { Target: not null } substreamType:
            {
                foreach (var dependency in ExtractTypeAnnotationDependencies(substreamType.Target, typeParameters))
                    yield return dependency;

                break;
            }
        }
    }

    private sealed class InterpretationSchemaUsageVisitor(
        HashSet<string> usedSchemas,
        HashSet<string> knownSchemas)
        : NoOpExpressionVisitor
    {
        public override void Visit(AccessMethodNode node)
        {
            if (!IsInterpretationFunction(node.Name))
                return;

            if (!string.IsNullOrWhiteSpace(node.TypeParameter) && knownSchemas.Contains(node.TypeParameter))
                usedSchemas.Add(node.TypeParameter);
        }

        public override void Visit(AliasedFromNode node)
        {
            if (!IsInterpretationFunction(node.Identifier))
                return;

            if (!string.IsNullOrWhiteSpace(node.TypeParameter) && knownSchemas.Contains(node.TypeParameter))
                usedSchemas.Add(node.TypeParameter);
        }

        public override void Visit(InterpretCallNode node)
        {
            if (knownSchemas.Contains(node.SchemaName))
                usedSchemas.Add(node.SchemaName);
        }

        public override void Visit(ParseCallNode node)
        {
            if (knownSchemas.Contains(node.SchemaName))
                usedSchemas.Add(node.SchemaName);
        }

        public override void Visit(TryInterpretCallNode node)
        {
            if (knownSchemas.Contains(node.SchemaName))
                usedSchemas.Add(node.SchemaName);
        }

        public override void Visit(TryParseCallNode node)
        {
            if (knownSchemas.Contains(node.SchemaName))
                usedSchemas.Add(node.SchemaName);
        }

        public override void Visit(PartialInterpretCallNode node)
        {
            if (knownSchemas.Contains(node.SchemaName))
                usedSchemas.Add(node.SchemaName);
        }

        public override void Visit(PartialParseCallNode node)
        {
            if (knownSchemas.Contains(node.SchemaName))
                usedSchemas.Add(node.SchemaName);
        }

        public override void Visit(InterpretAtCallNode node)
        {
            if (knownSchemas.Contains(node.SchemaName))
                usedSchemas.Add(node.SchemaName);
        }

        private static bool IsInterpretationFunction(string functionName)
        {
            return functionName.Equals("Interpret", StringComparison.OrdinalIgnoreCase)
                   || functionName.Equals("Parse", StringComparison.OrdinalIgnoreCase)
                   || functionName.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase)
                   || functionName.Equals("TryParse", StringComparison.OrdinalIgnoreCase)
                   || functionName.Equals("PartialInterpret", StringComparison.OrdinalIgnoreCase)
                   || functionName.Equals("PartialParse", StringComparison.OrdinalIgnoreCase)
                   || functionName.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class InterpretationSchemaUsageTraverseVisitor(InterpretationSchemaUsageVisitor visitor)
        : RawTraverseVisitor<InterpretationSchemaUsageVisitor>(visitor);
}
