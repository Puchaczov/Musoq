using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Evaluator.Tests.Schema.Multi.Second;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.Schema.Multi;

public class MultiLibrary : LibraryBase
{
    [BindableMethod]
    public int MethodA([InjectSpecificSource(typeof(FirstEntity))] FirstEntity entity)
    {
        return 0;
    }

    [BindableMethod]
    public int MethodA([InjectSpecificSource(typeof(SecondEntity))] SecondEntity entity)
    {
        return 1;
    }
    
    [BindableMethod]
    public int MethodB([InjectSpecificSource(typeof(FirstEntity))] FirstEntity entity, string arg1)
    {
        return 0;
    }
    
    [BindableMethod]
    public int MethodB([InjectSpecificSource(typeof(SecondEntity))] SecondEntity entity, string arg1)
    {
        return 1;
    }
}