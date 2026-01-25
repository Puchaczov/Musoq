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
    public string[] MethodArrayOfStringsWithoutParameters<TEntity>(
        [InjectSpecificSource(typeof(object))] TEntity entity)
    {
        return
        [
            "one",
            "two"
        ];
    }

    [BindableMethod]
    public string[] MethodArrayOfStringsWithDefaultParameter<TEntity>(
        [InjectSpecificSource(typeof(object))] TEntity entity, bool param = true)
    {
        return
        [
            "one",
            "two"
        ];
    }

    [BindableMethod]
    public string[] MethodArrayOfStringsWithOneParamAndDefaultParameter<TEntity>(
        [InjectSpecificSource(typeof(object))] TEntity entity, string someValue, bool param = true)
    {
        return
        [
            "one",
            "two"
        ];
    }

    [BindableMethod]
    public string[] MethodArrayOfStrings<TEntity>([InjectSpecificSource(typeof(object))] TEntity entity, string name1,
        string name2)
    {
        return
        [
            name1,
            name2
        ];
    }
}
