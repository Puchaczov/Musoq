using System;
using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.Fixtures;

public sealed class BufferFlowEntity
{
    public byte[] Content { get; set; } = [1, 2, 3, 4, 5, 6];
    public int Calls { get; set; }
    public int Enumerated { get; set; }
    public List<string> Events { get; } = [];
    public BufferFlowEntity Self => this;
}

public sealed class BufferFlowLibrary : LibraryBase
{
    [BindableMethod]
    public byte[] NextBuffer(BufferFlowEntity entity)
    {
        entity.Calls++;
        if (entity.Calls > 1)
            entity.Content[0] = 99;
        return entity.Content;
    }

    [BindableMethod]
    public IEnumerable<byte> RelaySequence(IEnumerable<byte> values) => values;

    [BindableMethod]
    public byte[] UnmarkedBuffer(BufferFlowEntity entity)
    {
        entity.Calls++;
        return entity.Content;
    }

    [BindableMethod]
    [NonDeterministic]
    public byte[] NullBuffer([InjectSpecificSource(typeof(BufferFlowEntity))] BufferFlowEntity entity)
    {
        entity.Events.Add("null");
        return null!;
    }

    [BindableMethod]
    [NonDeterministic]
    public byte[] Produce([InjectSpecificSource(typeof(BufferFlowEntity))] BufferFlowEntity entity, byte[] value)
    {
        entity.Calls++;
        entity.Events.Add("produce");
        return value;
    }

    [BindableMethod]
    [NonDeterministic]
    public int Mutate([InjectSpecificSource(typeof(BufferFlowEntity))] BufferFlowEntity entity, byte[] value)
    {
        entity.Events.Add("mutate");
        value[0] = 99;
        return 0;
    }

    [BindableMethod]
    [NonDeterministic]
    public byte ReadAfter(byte[] snapshot, int ignored) => snapshot[0];

    [BindableMethod]
    [NonDeterministic]
    public IEnumerable<byte> Sequence([InjectSpecificSource(typeof(BufferFlowEntity))] BufferFlowEntity entity, byte[] value)
    {
        foreach (var item in value)
        {
            entity.Enumerated++;
            yield return item;
        }
    }

    [BindableMethod]
    [NonDeterministic]
    public byte[] Fail([InjectSpecificSource(typeof(BufferFlowEntity))] BufferFlowEntity entity, byte[] value)
    {
        entity.Events.Add("fail");
        throw new InvalidOperationException("buffer producer failure");
    }
}
