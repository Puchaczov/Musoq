using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private static (Type ClrType, string? IntendedTypeName) ApplyBinaryFieldIntendedTypeName(
        FieldDefinitionNode field,
        (Type ClrType, string? IntendedTypeName) resolvedType)
    {
        var intended = BinaryCompositionTypes.IntendedTypeName(field.Name, field.TypeAnnotation) ?? resolvedType.IntendedTypeName;
        var type = field.TypeAnnotation is BinarySwitchTypeNode ? typeof(object) : resolvedType.ClrType;
        return (BinarySchemaExpressionTypes.Lift(type, field.IsConditional), intended);
    }

    private DynamicTable? TurnTypeIntoTableWithIntendedTypeName(Type type, string? intendedTypeName, Node? node)
    {
        Type? nestedType;
        if (type.IsArray)
        {
            nestedType = type.GetElementType();
        }
        else if (IsGenericEnumerable(type, out nestedType))
        {
        }
        else
        {
            if (TryReportColumnMustBeArray(node))
                return null;
            throw new ColumnMustBeAnArrayOrImplementIEnumerableException();
        }

        if (nestedType == null) throw new InvalidOperationException("Element type is null.");

        if (IsPrimitiveType(nestedType))
            return new DynamicTable([new SchemaColumn(nameof(PrimitiveTypeEntity<object>.Value), 0, nestedType)]);

        var inlineSchemaFields = ResolveInlineSchemaFields(intendedTypeName);
        if (nestedType == typeof(object) && inlineSchemaFields != null)
        {
            return new DynamicTable(CreateBinarySchemaColumns(
                inlineSchemaFields).ToArray());
        }

        if (nestedType == typeof(object) && !string.IsNullOrEmpty(intendedTypeName) && SchemaRegistry != null)
        {
            var elementIntendedTypeName = intendedTypeName.EndsWith("[]", StringComparison.Ordinal)
                ? intendedTypeName.Substring(0, intendedTypeName.Length - 2)
                : intendedTypeName;
            var schemaReference = ParseSchemaReferenceFromTypeName(elementIntendedTypeName);
            if (SchemaRegistry.TryGetSchema(schemaReference.SchemaName, out var schemaRegistration))
            {
                if (schemaRegistration?.Node is BinarySchemaNode binaryNode)
                {
                    var allFields = GetAllBinarySchemaFields(binaryNode);
                    var typeParamMap = CreateTypeParameterMap(binaryNode, schemaReference.TypeArguments);
                    return new DynamicTable(CreateBinarySchemaColumns(
                        allFields,
                        typeParamMap).ToArray());
                }

                if (schemaRegistration?.Node is TextSchemaNode textNode)
                {
                    var columns = new List<ISchemaColumn>();
                    var columnIndex = 0;

                    foreach (var field in textNode.Fields)
                    {
                        if (field.Name.StartsWith('_')) continue;
                        columns.Add(new SchemaColumn(field.Name, columnIndex++, typeof(string)));
                    }

                    return new DynamicTable(columns.ToArray());
                }
            }
        }


        var _columns = new List<ISchemaColumn>();
        foreach (var property in nestedType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            _columns.Add(new SchemaColumn(property.Name, _columns.Count, property.PropertyType));

        return new DynamicTable(_columns.ToArray(), nestedType);
    }
}
