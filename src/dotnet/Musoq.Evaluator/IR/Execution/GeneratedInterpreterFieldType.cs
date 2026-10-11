namespace Musoq.Evaluator.IR.Execution;

internal static class GeneratedInterpreterFieldType
{
    internal static Type? Find(Type? generatedType, string name) =>
        generatedType?.GetProperty(name)?.PropertyType ?? generatedType?.GetField(name)?.FieldType;
}
