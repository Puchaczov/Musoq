using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.Fixtures;

public sealed class BufferProducerEntity
{
    public byte[] Content { get; set; } = [];
    public int Calls { get; set; }
}

public sealed class BufferProducerLibrary : LibraryBase
{
    [BindableMethod]
    [NonDeterministic]
    public byte[] Capture(
        [InjectSpecificSource(typeof(BufferProducerEntity))] BufferProducerEntity entity,
        byte[] content
    )
    {
        entity.Calls++;
        return content;
    }
}

public sealed class ShadowBufferLibrary : LibraryBase
{
    [BindableMethod]
    public new string? ToBase64(byte[]? value) => value is null ? null : "Bw==";
}
