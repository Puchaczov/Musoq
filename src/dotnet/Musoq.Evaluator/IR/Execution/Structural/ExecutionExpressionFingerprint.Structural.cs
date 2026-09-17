using System.Linq;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionExpressionFingerprint
{
    internal static string ForStructuralPlan(ExecutionStructuralConstructionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var builder = new StringBuilder(plan.MetadataFingerprint);
        builder.Append(":shape=").Append(ForStructuralShape(plan.InputShape));
        builder.Append(":defaults=");
        foreach (var value in plan.Defaults)
            builder.Append(value == null ? "-" : ForHoist(value)).Append(';');

        return builder.ToString();
    }

    internal static string ForStructuralShape(ExecutionStructuralShape shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        var builder = new StringBuilder();
        builder.Append(shape.Kind)
            .Append(':')
            .Append(shape.IsNullable)
            .Append(':')
            .Append(shape.ScalarType?.StableId ?? "-")
            .Append(':')
            .Append(shape.ElementType?.StableId ?? "-");

        if (shape.ElementShape is { } elementShape)
            builder.Append("<").Append(ForStructuralShape(elementShape)).Append('>');

        builder.Append('[');
        foreach (var field in shape.Fields)
        {
            builder.Append(field.Name)
                .Append(':')
                .Append(field.Type.StableId)
                .Append(':')
                .Append(field.Required)
                .Append(':')
                .Append(field.HasDefault)
                .Append(':')
                .Append(field.DefaultText ?? "-");
            if (field.Shape is { } fieldShape)
                builder.Append('<').Append(ForStructuralShape(fieldShape)).Append('>');
            builder.Append(';');
        }

        return builder.Append(']').ToString();
    }

    internal static string ForStructuralRecord(ExecutionStructuralRecord record)
    {
        var fields = string.Join(';', record.Fields.Select(field =>
            $"{field.Name}:{field.IsPresent}:{ForHoist(field.Value)}"));
        return $"record:{HoistType(record.ReturnType)}:{fields}:plan=" +
               (record.ConstructionPlan == null ? "-" : ForStructuralPlan(record.ConstructionPlan)) +
               $":limits={FormatLimitBinding(record.LimitBinding)}";
    }

    internal static string ForStructuralArray(ExecutionStructuralArray array)
    {
        return $"array:{HoistType(array.ReturnType)}:{HoistType(array.ElementType)}:" +
               string.Join(';', array.Elements.Select(ForHoist)) +
               $":limits={FormatLimitBinding(array.LimitBinding)}";
    }

    internal static string ForCteCollectionInput(
        ExecutionCteCollectionInput input,
        string rowsFingerprint)
    {
        ArgumentNullException.ThrowIfNull(input);

        return $"cte-input:{input.CteName}:{input.ElementType.StableId}:{rowsFingerprint}:" +
               $"fields={string.Join(';', input.Fields.Select(static field =>
                   $"{field.Name}:{field.Index}:{field.Type.StableId}"))}:" +
               $"plan={(input.ConstructionPlan == null ? "-" : ForStructuralPlan(input.ConstructionPlan))}:" +
               $"limits={FormatLimitBinding(input.LimitBinding)}:" +
               $"ownership={input.Ownership};lifetime={input.Lifetime};metrics={input.MetricsStrategy}";
    }

    internal static string FormatLimitBinding(ExecutionStructuralLimitBinding? binding) =>
        binding == null
            ? "-"
            : $"{binding.Origin}:{binding.Path}:{binding.SourceContextId ?? "-"}:{binding.Limits}";
}
