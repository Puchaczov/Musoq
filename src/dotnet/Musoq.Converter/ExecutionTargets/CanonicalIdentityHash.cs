using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Musoq.Converter.Build;

internal static class CanonicalIdentityHash
{
    private const int EncodingBufferSize = 4096;
    private const int Sha256Size = 32;

    internal static string Compute(string descriptor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var encoder = Encoding.UTF8.GetEncoder();
        var remaining = descriptor.AsSpan();
        Span<byte> buffer = stackalloc byte[EncodingBufferSize];
        bool completed;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            encoder.Convert(remaining, buffer, flush: true, out var characters, out var bytes, out completed);
            hash.AppendData(buffer[..bytes]);
            remaining = remaining[characters..];
        } while (!completed);

        cancellationToken.ThrowIfCancellationRequested();
        Span<byte> digest = stackalloc byte[Sha256Size];
        hash.GetHashAndReset(digest);
        return Convert.ToHexString(digest);
    }
}
