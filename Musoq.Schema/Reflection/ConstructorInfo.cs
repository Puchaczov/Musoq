using System;

namespace Musoq.Schema.Reflection
{
    public class ConstructorInfo
    {
        public Type OriginType { get; }

        public (string Name, Type Type)[] Arguments { get; }

        public ConstructorInfo(Type originType, params (string Name, Type Type)[] arguments)
        {
            OriginType = originType;
            Arguments = arguments;
        }
    }

    public class SchemaMethodInfo
    {
        public string MethodName { get; }

        public ConstructorInfo ConstructorInfo { get; }

        public SchemaMethodInfo(string methodName, ConstructorInfo constructorInfo)
        {
            MethodName = methodName;
            ConstructorInfo = constructorInfo;
        }
    }
}
