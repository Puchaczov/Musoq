using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Traversal;

namespace Musoq.Evaluator.Visitors;

public partial class SchemaDefinitionVisitor
{
    private Node? _fieldReferenceScope;

    private void ValidateExpressionReferences(
        string fieldName,
        Node? expression,
        string schemaName,
        HashSet<string> declaredFields)
    {
        if (expression == null)
            return;

        var identifiers = new IdentifierCollector();
        new IdentifierCollectorTraverse(identifiers).Traverse(expression);

        foreach (var reference in identifiers.References)
        {
            if (declaredFields.Contains(reference.Name) || reference.Name.Equals("parent", StringComparison.OrdinalIgnoreCase))
                continue;

            throw new QuerySyntaxException(
                DescribeUnresolvedField(schemaName, fieldName, reference.Name),
                reference.Node.SpanOrEmpty(),
                DiagnosticCode.MQ2030_UnsupportedSyntax);
        }
    }

    private string DescribeUnresolvedField(string schemaName, string fieldName, string referenceName)
    {
        if (IsForwardFieldReference(schemaName, referenceName))
            return $"Binary schema '{schemaName}' field '{fieldName}' references field '{referenceName}' before it is declared.";

        var message = $"Binary schema '{schemaName}' field '{fieldName}' references unknown field '{referenceName}' in its local scope.";
        if (_fieldReferenceScope is not BinarySchemaNode)
            return message;

        return message + $" Bare field names in a named schema do not search enclosing records. " +
            $"If this is an enclosing field, use 'parent.{referenceName}' explicitly.";
    }

    private bool IsForwardFieldReference(string schemaName, string referenceName)
    {
        if (ContainsField(ScopeFields(_fieldReferenceScope), referenceName))
            return true;
        if (_fieldReferenceScope is not InlineSchemaTypeNode inline ||
            !Registry.TryGetSchema(schemaName, out var registration))
            return false;

        var lookup = new EnclosingFieldLookup(inline, referenceName);
        lookup.Walk(registration!.Node);
        return lookup.IsForward;
    }

    private static SchemaFieldNode[] ScopeFields(Node? scope) => scope switch
    {
        BinarySchemaNode binary => binary.Fields,
        InlineSchemaTypeNode inline => inline.Fields,
        _ => []
    };

    private sealed class EnclosingFieldLookup(InlineSchemaTypeNode target, string name) : AstWalker
    {
        private readonly Stack<SchemaFieldNode[]> _scopes = new();
        private bool _found;
        public bool IsForward { get; private set; }

        protected override bool Enter(Node node)
        {
            if (_found)
                return false;
            if (ReferenceEquals(node, target))
            {
                _found = true;
                foreach (var fields in _scopes)
                    IsForward |= ContainsField(fields, name);
                return false;
            }
            if (node is BinarySchemaNode or InlineSchemaTypeNode)
                _scopes.Push(ScopeFields(node));
            return true;
        }

        protected override void Leave(Node node)
        {
            if (node is BinarySchemaNode or InlineSchemaTypeNode)
                _scopes.Pop();
        }
    }

    private static bool ContainsField(SchemaFieldNode[] fields, string name)
    {
        foreach (var field in fields)
            if (field.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private void ValidateInlineFieldReferences(
        InlineSchemaTypeNode inlineSchema,
        string fieldName,
        string schemaName,
        HashSet<string> outerFields,
        IReadOnlyDictionary<string, Type?> outerFieldTypes)
    {
        var previousScope = _fieldReferenceScope;
        _fieldReferenceScope = inlineSchema;
        try
        {
            ValidateInlineFields(inlineSchema, fieldName, schemaName, outerFields, outerFieldTypes);
        }
        finally
        {
            _fieldReferenceScope = previousScope;
        }
    }

    private void ValidateInlineFields(
        InlineSchemaTypeNode inlineSchema,
        string fieldName,
        string schemaName,
        HashSet<string> outerFields,
        IReadOnlyDictionary<string, Type?> outerFieldTypes)
    {
        var declaredFields = new HashSet<string>(outerFields, StringComparer.OrdinalIgnoreCase);
        var declaredFieldTypes = new Dictionary<string, Type?>(outerFieldTypes, StringComparer.OrdinalIgnoreCase);
        var localFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in inlineSchema.Fields)
        {
            ValidateDuplicateField(field, $"{schemaName}.{fieldName}", localFields);

            if (field is FieldDefinitionNode parsedField)
                ValidateFieldReferences(parsedField, schemaName, declaredFields, declaredFieldTypes);
            else if (field is ComputedFieldNode computedField)
                ValidateExpressionReferences(fieldName, computedField.Expression, schemaName, declaredFields);

            declaredFields.Add(field.Name);
            declaredFieldTypes[field.Name] = field is ComputedFieldNode computed
                ? ResolveExpressionType(computed.Expression, declaredFieldTypes)
                : field.ReturnType;
        }
    }
}
