namespace Musoq.Targets.Abstractions;

public sealed record ExecutionPortableRowFieldDescriptor(
    string Name,
    ExecutionPortableTypeDescriptor Type,
    string Nullability);
