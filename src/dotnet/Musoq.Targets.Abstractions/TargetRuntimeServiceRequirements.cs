using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Musoq.Targets.Abstractions;

internal sealed record TargetRuntimeServiceRequirements
{
    public TargetRuntimeServiceRequirements(IEnumerable<TargetRuntimeServiceRequirementKind> requiredServices)
        : this((requiredServices ?? throw new ArgumentNullException(nameof(requiredServices)))
            .Select(static service => new TargetRuntimeServiceFulfillment(
                service,
                TargetRuntimeServiceFulfillmentKind.HostImport)))
    {
    }

    public TargetRuntimeServiceRequirements(IEnumerable<TargetRuntimeServiceFulfillment> fulfillments)
    {
        ArgumentNullException.ThrowIfNull(fulfillments);

        var values = fulfillments.ToArray();
        var duplicate = values
            .GroupBy(static fulfillment => fulfillment.Service)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate != null)
        {
            throw new ArgumentException(
                $"Runtime service '{duplicate.Key}' has more than one fulfillment.",
                nameof(fulfillments));
        }

        Fulfillments = new ReadOnlyDictionary<TargetRuntimeServiceRequirementKind, TargetRuntimeServiceFulfillmentKind>(
            values.ToDictionary(static value => value.Service, static value => value.Fulfillment));
        RequiredServices = new ReadOnlySet<TargetRuntimeServiceRequirementKind>(
            new HashSet<TargetRuntimeServiceRequirementKind>(Fulfillments.Keys));
    }

    public IReadOnlySet<TargetRuntimeServiceRequirementKind> RequiredServices { get; }

    public IReadOnlyDictionary<TargetRuntimeServiceRequirementKind, TargetRuntimeServiceFulfillmentKind> Fulfillments { get; }

    public static TargetRuntimeServiceRequirements Empty { get; } =
        new(Array.Empty<TargetRuntimeServiceFulfillment>());

    public static TargetRuntimeServiceRequirements Create(
        params TargetRuntimeServiceRequirementKind[] requiredServices)
    {
        return new TargetRuntimeServiceRequirements(requiredServices);
    }

    public static TargetRuntimeServiceRequirements CreateTargetProvided(
        params TargetRuntimeServiceRequirementKind[] requiredServices)
    {
        return new TargetRuntimeServiceRequirements(requiredServices.Select(static service =>
            new TargetRuntimeServiceFulfillment(service, TargetRuntimeServiceFulfillmentKind.TargetProvided)));
    }

    public static TargetRuntimeServiceRequirements Create(
        params TargetRuntimeServiceFulfillment[] fulfillments)
    {
        return new TargetRuntimeServiceRequirements(fulfillments);
    }

    public bool Requires(TargetRuntimeServiceRequirementKind service)
    {
        return RequiredServices.Contains(service);
    }

    public TargetRuntimeServiceFulfillmentKind GetFulfillment(TargetRuntimeServiceRequirementKind service)
    {
        return Fulfillments.TryGetValue(service, out var fulfillment)
            ? fulfillment
            : throw new InvalidOperationException($"Runtime service '{service}' is not required.");
    }
}
