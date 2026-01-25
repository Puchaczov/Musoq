using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Musoq.Evaluator;

public static class AliasGenerator
{
    public static string CreateAliasIfEmpty(string alias, IReadOnlyList<string> usedAliases,
        string seed = "defaultSeed")
    {
        if (!string.IsNullOrEmpty(alias))
            return alias;

        var counter = 0;
        string aliasCandidate;

        do
        {
            aliasCandidate = DeterministicString(6, counter, seed);
            counter++;
        } while (usedAliases.Contains(aliasCandidate) || char.IsDigit(aliasCandidate[0]));

        return aliasCandidate;
    }

    private static string DeterministicString(int length, int counter, string seed,
        string allowedChars = "abcdefghijklmnopqrstuvwxyz0123456789")
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), @"length cannot be less than zero.");
        if (string.IsNullOrEmpty(allowedChars)) throw new ArgumentException("allowedChars may not be empty.");

        const int byteSize = 0x100;
        var allowedCharSet = new HashSet<char>(allowedChars).ToArray();
        if (byteSize < allowedCharSet.Length)
            throw new ArgumentException(
                $"allowedChars may contain no more than {byteSize} characters.");

        var result = new StringBuilder();
        byte[] hashBytes;

        using (var hasher = new HMACSHA256(Encoding.UTF8.GetBytes(seed)))
        {
            hashBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(counter.ToString()));
        }

        for (var i = 0; i < hashBytes.Length && result.Length < length; ++i)
        {
            var outOfRangeStart = byteSize - byteSize % allowedCharSet.Length;
            if (outOfRangeStart <= hashBytes[i]) continue;
            result.Append(allowedCharSet[hashBytes[i] % allowedCharSet.Length]);
        }

        return result.ToString();
    }
}
