namespace Musoq.Evaluator.Tests.External.Contracts;

public enum ExternalStatus
{
    Unknown,
    Ready,
    Closed
}

public interface IExternalMarker
{
    string Marker { get; }
}

public interface IExternalConstraint : IExternalMarker
{
    int ContractNumber { get; }
}

public abstract class ExternalBaseRow : IExternalMarker
{
    public string InheritedName { get; init; } = string.Empty;

    public ExternalBasePayload BasePayload { get; init; } = new();

    public abstract string Marker { get; }
}

public sealed class ExternalBasePayload
{
    public ExternalLeaf Leaf { get; init; } = new();

    public int BaseNumber { get; init; }
}

public sealed class ExternalLeaf
{
    public int Value { get; init; }

    public string Label { get; init; } = string.Empty;
}

public class ExternalPayload : IExternalConstraint
{
    public int ContractNumber { get; init; }

    public Uri ExternalUri { get; init; } = new("https://example.invalid");

    public ExternalLeaf Nested { get; init; } = new();

    public ExternalStatus Status { get; init; }

    public ExternalStatus? NullableStatus { get; init; }

    public ExternalLeaf[] Leaves { get; init; } = [];

    public Dictionary<string, ExternalLeaf> LeafMap { get; init; } = [];

    public string Marker => "external-payload";
}

public sealed class ConstrainedEnvelope<T> where T : IExternalConstraint
{
    public T Value { get; init; } = default!;
}

public sealed record ExternalProjection(int Number, string Name);
