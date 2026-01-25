namespace Musoq.Schema.Reflection;

public class SchemaMethodInfo(string methodName, ConstructorInfo constructorInfo)
{
    public string MethodName { get; } = methodName;

    public ConstructorInfo ConstructorInfo { get; } = constructorInfo;
}
