using System.Dynamic;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class DynamicLibrary : LibraryBase
{
    [BindableMethod]
    public int Increment(int value)
    {
        return value + 1;
    }

    [BindableMethod]
    public int Increment(dynamic value)
    {
        return value + 1;
    }
    
    [BindableMethod]
    public bool TrueWhenCalled([InjectSpecificSource(typeof(DynamicObject))] DynamicObject item)
    {
        return true;
    }
}