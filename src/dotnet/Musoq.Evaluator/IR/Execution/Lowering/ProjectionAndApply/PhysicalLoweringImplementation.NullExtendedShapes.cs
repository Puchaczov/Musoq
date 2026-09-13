using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static GeneratedRowShape CreateNullExtendedGeneratedShape(
        string typeName,
        IReadOnlyList<NullExtendedProjectedValue> fields,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        string nullAlias)
    {
        var usedFieldNames = new HashSet<string>(StringComparer.Ordinal);

        return new GeneratedRowShape(
            typeName,
            fields.Select(field => new FieldBinding(
                field.OutputName,
                field.OutputName,
                field.OutputIndex,
                field.ResultType,
                field.Nullability,
                new GeneratedFieldAccess(CreateGeneratedFieldName(field.OutputName, field.OutputIndex, usedFieldNames)),
                sourceReadType: field.ResultType,
                enumType: field.EnumType)).ToArray(),
            CreateContextBindings(sourceLookup, [nullAlias]));
    }

    private static GeneratedRowShape CreateFullOuterNullExtendedGeneratedShape(
        string typeName,
        IReadOnlyList<FullOuterNullExtendedProjectedValue> fields,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        string leftAlias,
        string rightAlias)
    {
        var usedFieldNames = new HashSet<string>(StringComparer.Ordinal);

        return new GeneratedRowShape(
            typeName,
            fields.Select(field => new FieldBinding(
                field.OutputName,
                field.OutputName,
                field.OutputIndex,
                field.ResultType,
                field.Nullability,
                new GeneratedFieldAccess(CreateGeneratedFieldName(field.OutputName, field.OutputIndex, usedFieldNames)),
                sourceReadType: field.ResultType,
                enumType: field.EnumType)).ToArray(),
            CreateContextBindings(sourceLookup, [leftAlias, rightAlias]));
    }
}
