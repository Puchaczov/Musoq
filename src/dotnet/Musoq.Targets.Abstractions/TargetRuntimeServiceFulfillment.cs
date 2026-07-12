namespace Musoq.Targets.Abstractions;

internal sealed record TargetRuntimeServiceFulfillment(
    TargetRuntimeServiceRequirementKind Service,
    TargetRuntimeServiceFulfillmentKind Fulfillment);
