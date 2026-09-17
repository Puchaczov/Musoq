namespace Musoq.Evaluator.Tests.Schema.CandidatePayload;

public enum CandidatePayloadMode
{
    RejectAll,
    CandidateMetadata,
    RowFiltering,
    UnknownVersion,
    PayloadOnlyCapability,
    MalformedMissingApplication,
    MalformedDuplicateApplication,
    MalformedAlteredApplication,
    MalformedUnadvertisedPhase,
    MalformedUnknownVersionApplication
}
