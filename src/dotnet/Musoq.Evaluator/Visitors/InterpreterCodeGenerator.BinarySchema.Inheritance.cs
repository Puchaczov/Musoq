using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private List<SchemaFieldNode> GetAllFieldsIncludingInherited(BinarySchemaNode schema)
    {
        if (string.IsNullOrEmpty(schema.Extends))
            return schema.Fields.ToList();

        var allFields = new List<SchemaFieldNode>();

        if (!string.IsNullOrEmpty(schema.Extends))
        {
            var parentSchema = _registry.Schemas
                .FirstOrDefault(s => s.Name.Equals(schema.Extends, StringComparison.OrdinalIgnoreCase));

            if (parentSchema?.Node is BinarySchemaNode parentBinarySchema)
                allFields.AddRange(GetAllFieldsIncludingInherited(parentBinarySchema));
        }

        var overriddenParentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var childField in schema.Fields)
            for (var i = 0; i < allFields.Count; i++)
                if (string.Equals(allFields[i].Name, childField.Name, StringComparison.OrdinalIgnoreCase))
                {
                    allFields[i] = childField;
                    overriddenParentNames.Add(childField.Name);
                    break;
                }

        foreach (var childField in schema.Fields)
            if (!overriddenParentNames.Contains(childField.Name))
                allFields.Add(childField);

        return allFields;
    }
}
