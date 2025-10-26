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
        
        methodsManager.TryGetMethod(nameof(TestLibraryBase.TestMethod1), [typeof(string)], typeof(TestEntity), out var method);
        
        Assert.IsNotNull(method);

        var parameters = method.GetParameters();
        
        Assert.HasCount(2, parameters);
        Assert.AreEqual(typeof(TestEntity), parameters[0].ParameterType);
        Assert.AreEqual(typeof(string[]), parameters[1].ParameterType);
    }

    private class TestLibraryBase : LibraryBase
    {
        [BindableMethod]
        public int TestMethod1([InjectSpecificSource(typeof(TestEntity))] TestEntity entity, params string[] arguments)
        {
            return 0;
        }
    }
    
    private class TestEntity;
}