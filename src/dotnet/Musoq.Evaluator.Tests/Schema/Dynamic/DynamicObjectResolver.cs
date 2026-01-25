using System;
using System.Collections.Generic;
using System.Dynamic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Dynamic;

public class DynamicObjectResolver : IObjectResolver
{
    private readonly DynamicObject _dynamicObject;
    private readonly IReadOnlyDictionary<int, string> _indexToNameMap;

    public DynamicObjectResolver(DynamicObject dynamicObject, IReadOnlyDictionary<int, string> indexToNameMap)
    {
        _dynamicObject = dynamicObject;
        _indexToNameMap = indexToNameMap;
    }

    public object[] Contexts => [_dynamicObject];

    public object this[string name]
    {
        get
        {
            if (_dynamicObject.TryGetMember(new CustomMemberBinder(name), out var result))
                return result;

            throw new InvalidOperationException();
        }
    }

    public object this[int index]
    {
        get
        {
            if (_dynamicObject.TryGetMember(new CustomMemberBinder(_indexToNameMap[index]), out var result))
                return result;

            throw new InvalidOperationException();
        }
    }

    public bool HasColumn(string name)
    {
        return _dynamicObject.TryGetMember(new CustomMemberBinder(name), out _);
    }

    private class CustomMemberBinder : GetMemberBinder
    {
        public CustomMemberBinder(string name)
            : base(name, false)
        {
        }

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }
    }
}
