using System;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class DynamicDictionaryResolver : IObjectResolver
{
    private readonly IDictionary<int, string> _indexToNameMap;
    private readonly IDictionary<string, object> _obj;

    public DynamicDictionaryResolver(IDictionary<string, object> obj, IDictionary<int, string> indexToNameMap)
    {
        _obj = obj ?? throw new InvalidOperationException();
        _indexToNameMap = indexToNameMap;
        Contexts = [obj];
    }

    public bool HasColumn(string name)
    {
        return _obj.ContainsKey(name);
    }

    public object[] Contexts { get; }

    public object this[string name] => _obj[name];

    public object this[int index] => _obj[_indexToNameMap[index]];
}
