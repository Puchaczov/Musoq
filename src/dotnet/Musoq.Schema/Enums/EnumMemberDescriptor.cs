namespace Musoq.Schema;

/// <summary>
///     Portable description of one declared enum member.
/// </summary>
public sealed record EnumMemberDescriptor
{
    public EnumMemberDescriptor(string name, EnumScalarValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public EnumScalarValue Value { get; }
}
