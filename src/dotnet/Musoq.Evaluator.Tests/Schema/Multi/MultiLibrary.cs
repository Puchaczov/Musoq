using System.Collections.Generic;
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

    [AggregateFunction(typeof(AggregateMethodAKernel), Name = nameof(AggregateMethodA), Inline = true)]
    public string AggregateMethodA()
    {
        return AggregateFunction.NotInvoked<string>();
    }

    [BindableMethod]
    public int MethodC([InjectSpecificSource(typeof(ICommonInterface))] ICommonInterface entity)
    {
        return 5;
    }

    public static class AggregateMethodAKernel
    {
        public struct State
        {
            public List<int>? Values;
        }

        public static void Set(ref State state)
        {
            (state.Values ??= []).Add(0);
        }

        public static string Get(in State state)
        {
            return state.Values is { Count: > 0 } values
                ? string.Join(',', values)
                : string.Empty;
        }

        public static void Merge(ref State target, in State source)
        {
            if (source.Values is null || source.Values.Count == 0)
                return;

            var values = target.Values ??= [];
            values.AddRange(source.Values);
        }
    }
}
