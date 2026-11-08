using Musoq.Evaluator.Tests.External.Contracts;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.External.Rows;

public interface IExternalRowMarker : IExternalMarker
{
}

public sealed class ExternalRow : ExternalBaseRow, IExternalRowMarker
{
    public ExternalPayload Payload { get; init; } = new();

    public ExternalStatus? Status { get; init; }

    public ExternalLeaf[] Leaves { get; init; } = [];

    public List<Dictionary<string, ExternalLeaf>> NestedValues { get; init; } = [];

    public override string Marker => "external-row";
}

public sealed class GenericExternalRow<T> : ExternalBaseRow where T : ExternalPayload, new()
{
    public T Payload { get; init; } = new();

    public override string Marker => "generic-row";
}

public sealed class ExternalLibrary : LibraryBase
{
    [BindableMethod]
    public int GetContractNumber(ExternalPayload payload) => payload.ContractNumber;

    [BindableMethod]
    public ExternalLeaf GetLeaf(ExternalPayload payload) => payload.Nested;

    [BindableMethod]
    public string GetMarker(IExternalMarker marker) => marker.Marker;

    [BindableMethod]
    public T Echo<T>(T value) where T : ExternalPayload, new() => value;
}
