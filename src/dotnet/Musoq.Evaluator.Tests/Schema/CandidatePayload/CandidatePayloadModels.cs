using System;
using System.Text;

namespace Musoq.Evaluator.Tests.Schema.CandidatePayload;

public enum CandidatePayloadFault
{
    None,
    Open,
    Decode
}

public sealed record CandidatePayloadSeed(
    int Id,
    string? Path,
    string EncodedPayload,
    CandidatePayloadFault Fault = CandidatePayloadFault.None)
{
    public static CandidatePayloadSeed Create(
        int id,
        string? path,
        string payload,
        CandidatePayloadFault fault = CandidatePayloadFault.None) =>
        new(id, path, Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)), fault);
}

public sealed class CandidatePayloadEntity
{
    public int Id { get; init; }

    public string? Path { get; init; }

    public string Payload { get; init; } = string.Empty;
}
