using System;

namespace Musoq.Schema.Reflection;

public class ConstructorInfo(
    System.Reflection.ConstructorInfo originConstructorInfo,
    bool supportsInterCommunicator,
    params (string Name, Type Type)[] arguments)
{
    public System.Reflection.ConstructorInfo OriginConstructor { get; } = originConstructorInfo;

    public (string Name, Type Type)[] Arguments { get; } = arguments;

    public bool SupportsInterCommunicator { get; } = supportsInterCommunicator;

    public static ConstructorInfo Empty()
    {
        return new ConstructorInfo(null, false);
    }
}
