using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

public abstract class MethodsMetadataTestBase
{
    protected static MethodsMetadata CreateMethodsMetadataFor<TTestClass>()
    {
        return new TestMethodsMetadata<TTestClass>();
    }

    protected static MethodInfo RequireResolved(MethodInfo? method)
    {
        return method ?? throw new AssertFailedException("Expected method resolution to return a method.");
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
