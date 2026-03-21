using System.Reflection;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

public abstract class MethodsMetadataTestBase
{
    protected static MethodsMetadata CreateMethodsMetadataFor<TTestClass>()
    {
        return new TestMethodsMetadata<TTestClass>();
    }

    private sealed class TestMethodsMetadata<TTestClass> : MethodsMetadata
    {
        public TestMethodsMetadata()
        {
            var testClass = typeof(TTestClass);

            foreach (var method in testClass.GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                RegisterMethod(method);
        }

        private new void RegisterMethod(MethodInfo methodInfo)
        {
            base.RegisterMethod(methodInfo);
        }
    }
}
