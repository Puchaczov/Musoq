using System;
using System.Collections.Generic;
using Musoq.Evaluator.Tests.Schema.Multi.Second;

namespace Musoq.Evaluator.Tests.Schema.Multi.First;

public class FirstEntity : ICommonInterface
{
    public static readonly IDictionary<string, int> TestNameToIndexMap;
    public static readonly IDictionary<int, Func<FirstEntity, object>> TestIndexToObjectAccessMap;
}