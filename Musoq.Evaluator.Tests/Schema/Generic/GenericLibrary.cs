using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.Schema.Generic;

public class GenericLibrary : LibraryBase
{ 
    [BindableMethod]
    public string[] JustReturnArrayOfString()
    {
        return ["1", "2", "3"];
    }
        
    [BindableMethod]
    public string TestMethodWithInjectEntityAndParameter(string name)
    {
        return name;
    }
    
    [BindableMethod]
    public string[] MethodArrayOfStrings<TEntity>([InjectSpecificSource(typeof(object))] TEntity entity, string name1, string name2)
    {
        return [
            name1,
            name2
        ];
    }
}