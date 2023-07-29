using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.Multi.First;

public class FirstEntity
{
    public static readonly IDictionary<string, int> TestNameToIndexMap;
    public static readonly IDictionary<int, Func<FirstEntity, object>> TestIndexToObjectAccessMap;
}