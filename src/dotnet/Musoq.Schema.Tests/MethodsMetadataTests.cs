using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadataTests
{
    [TestMethod]
    public void WhenInjectingToMethodEntityAndParamsArguments_ShouldResolve()
    {
        var methodsManager = new MethodsManager();

        methodsManager.RegisterLibraries(new TestLibraryBase());

        methodsManager.TryGetMethod(nameof(TestLibraryBase.TestMethod1), [typeof(string)], typeof(TestEntity),
            out var method);

        Assert.IsNotNull(method);

        var parameters = method.GetParameters();

        Assert.HasCount(2, parameters);
        Assert.AreEqual(typeof(TestEntity), parameters[0].ParameterType);
        Assert.AreEqual(typeof(string[]), parameters[1].ParameterType);
    }

    [TestMethod]
    public void WhenRegisteringDerivedLibrary_ShouldResolveMethodsFromConcreteHierarchy()
    {
        var methodsManager = new MethodsManager();

        methodsManager.RegisterLibraries(new DerivedTestLibraryBase());

        var hasDerivedMethod = methodsManager.TryGetMethod(nameof(DerivedTestLibraryBase.DerivedMethod),
            [typeof(string)], null, out _);
        var hasIntermediateMethod = methodsManager.TryGetMethod(nameof(IntermediateTestLibraryBase.IntermediateMethod),
            [typeof(int)], null, out _);
        var hasLibraryBaseMethod = methodsManager.TryGetMethod(nameof(LibraryBase.GetTypeName), [typeof(object)], null,
            out _);

        Assert.IsTrue(hasDerivedMethod);
        Assert.IsTrue(hasIntermediateMethod);
        Assert.IsTrue(hasLibraryBaseMethod);
    }

    [TestMethod]
    public void WhenRegisteringDerivedLibrary_ShouldNotResolveMethodsOutsideConcreteHierarchy()
    {
        var methodsManager = new MethodsManager();

        methodsManager.RegisterLibraries(new DerivedTestLibraryBase());

        var hasSiblingMethod = methodsManager.TryGetMethod(nameof(SiblingTestLibraryBase.SiblingMethod), [typeof(int)],
            null, out _);

        Assert.IsFalse(hasSiblingMethod);
    }

    private class TestLibraryBase : LibraryBase
    {
        [BindableMethod]
        public int TestMethod1([InjectSpecificSource(typeof(TestEntity))] TestEntity entity, params string[] arguments)
        {
            return 0;
        }
    }

    private class IntermediateTestLibraryBase : LibraryBase
    {
        [BindableMethod]
        public int IntermediateMethod(int value)
        {
            return value;
        }
    }

    private sealed class DerivedTestLibraryBase : IntermediateTestLibraryBase
    {
        [BindableMethod]
        public string DerivedMethod(string value)
        {
            return value;
        }
    }

    private sealed class SiblingTestLibraryBase : LibraryBase
    {
        [BindableMethod]
        public int SiblingMethod(int value)
        {
            return value;
        }
    }

    private class TestEntity;
}
