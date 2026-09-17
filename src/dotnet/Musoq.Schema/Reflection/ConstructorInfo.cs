using Musoq.Schema.StructuralInputs;

namespace Musoq.Schema.Reflection;

public class ConstructorInfo(
    System.Reflection.ConstructorInfo? originConstructorInfo,
    bool supportsInterCommunicator,
    params (string Name, Type Type)[] arguments)
{
    public System.Reflection.ConstructorInfo? OriginConstructor { get; } = originConstructorInfo;

    public (string Name, Type Type)[] Arguments { get; } = arguments;

    public bool SupportsInterCommunicator { get; } = supportsInterCommunicator;

    /// <summary>Gets the stable identity of an explicitly typed source constructor.</summary>
    public string? SourceStableId { get; set; }

    /// <summary>Gets additive structural metadata for an explicitly typed source.</summary>
    public StructuralInputContract? StructuralContract { get; set; }

    public static ConstructorInfo Empty()
    {
        return new ConstructorInfo(null, false);
    }
}
