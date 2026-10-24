using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>A closed interpretation value shape, including alternatives and nested values.</summary>
public sealed record ExecutionInterpretationShape
{
    internal ExecutionInterpretationShape(TextResultShape shape)
    {
        Type = ExecutionClrBindingFactory.FromClr(shape.ClrType);
        TypeName = shape.TypeName;
        Members = ExecutionIrCollections.Freeze(shape.Members.ToDictionary(pair => pair.Key, pair => new ExecutionInterpretationShape(pair.Value)));
        Alternatives = ExecutionIrCollections.Freeze(shape.Alternatives.Select(arm => new ExecutionInterpretationShape(arm)));
        Element = shape.Element is { } element ? new ExecutionInterpretationShape(element) : null;
    }

    private ExecutionInterpretationShape(IReadOnlyList<ExecutionInterpretationShape> alternatives)
    {
        Type = ExecutionClrBindingFactory.FromClr(typeof(object));
        Alternatives = ExecutionIrCollections.Freeze(alternatives);
        Members = ExecutionIrCollections.Freeze(new Dictionary<string, ExecutionInterpretationShape>());
    }

    internal ExecutionInterpretationShape? Member(string name)
    {
        if (Alternatives.Count == 0) return Members.GetValueOrDefault(name);
        var values = Alternatives.Select(arm => arm.Member(name)).OfType<ExecutionInterpretationShape>().ToArray();
        if (values.Length == 0) return null;
        if (values.Length != Alternatives.Count)
            return Merge([.. values, new ExecutionInterpretationShape(new TextResultShape(typeof(object), null))]);
        return Merge(values);
    }

    internal static ExecutionInterpretationShape Merge(IReadOnlyList<ExecutionInterpretationShape> arms)
    {
        if (arms.All(arm => arm.Alternatives.Count == 0 && arm.Type == arms[0].Type && arm.TypeName == arms[0].TypeName))
        {
            if (arms[0].Element != null)
                return arms[0] with { Element = Merge(arms.Select(arm => arm.Element!).ToArray()) };
            return arms[0];
        }
        return new(arms.SelectMany(arm => arm.Alternatives.Count > 0 ? arm.Alternatives : [arm]).ToArray());
    }

    public ExecutionTypeRef Type { get; }
    public string? TypeName { get; }
    public IReadOnlyDictionary<string, ExecutionInterpretationShape> Members { get; }
    public IReadOnlyList<ExecutionInterpretationShape> Alternatives { get; }
    public ExecutionInterpretationShape? Element { get; private init; }
}
