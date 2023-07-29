using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.Multi.Second;

public class SecondEntity
{
    public static readonly IDictionary<string, int> TestNameToIndexMap;
    public static readonly IDictionary<int, Func<SecondEntity, object>> TestIndexToObjectAccessMap;
}