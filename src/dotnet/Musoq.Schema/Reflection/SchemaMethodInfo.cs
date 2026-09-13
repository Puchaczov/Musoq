using Musoq.Schema.StructuralInputs;

namespace Musoq.Schema.Reflection;

public class SchemaMethodInfo(string methodName, ConstructorInfo constructorInfo)
{
    public string MethodName { get; } = methodName;

    public ConstructorInfo ConstructorInfo { get; } = constructorInfo;

    /// <summary>Gets additive structural metadata when this method was explicitly typed-registered.</summary>
    public StructuralInputContract? StructuralContract => ConstructorInfo.StructuralContract;
}
