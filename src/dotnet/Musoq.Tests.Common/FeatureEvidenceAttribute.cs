namespace Musoq.Tests.Common;

public enum FeatureEvidenceKind
{
    RuntimePositive,
    RuntimeNegativeDiagnostic,
    InterpreterPositive
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class FeatureEvidenceAttribute(string featureId, FeatureEvidenceKind kind) : Attribute
{
    public string FeatureId { get; } = featureId;

    public FeatureEvidenceKind Kind { get; } = kind;
}
