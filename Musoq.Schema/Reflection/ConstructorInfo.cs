using System;

namespace Musoq.Schema.Reflection
{
    public class ConstructorInfo
    {
        public System.Reflection.ConstructorInfo OriginConstructor { get; }

        public Type OriginType { get; }

        public (string Name, Type Type)[] Arguments { get; }

        public bool SupportsInterCommunicator { get; }

        public ConstructorInfo(System.Reflection.ConstructorInfo originConstructorInfo, Type originType, bool supportsInterCommunicator, params (string Name, Type Type)[] arguments)
        {
            OriginConstructor = originConstructorInfo;
            OriginType = originType;
            Arguments = arguments;
            SupportsInterCommunicator = supportsInterCommunicator;
        }

        public static ConstructorInfo Empty<T>()
        {
            return new ConstructorInfo(null, typeof(T), false, Array.Empty<(string, Type)>());
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
