using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Helpers;

/// <summary>
/// Provides string similarity comparison utilities for suggesting corrections to mistyped keywords.
/// </summary>
public static class StringSimilarity
{
    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// The Levenshtein distance is the minimum number of single-character edits (insertions, deletions, or substitutions)
    /// required to change one string into another.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="target">The target string.</param>
    /// <returns>The Levenshtein distance between the two strings.</returns>
    public static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return string.IsNullOrEmpty(target) ? 0 : target.Length;
        
        if (string.IsNullOrEmpty(target))
            return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;
        var distance = new int[sourceLength + 1, targetLength + 1];

        // Initialize first column and row
        for (var i = 0; i <= sourceLength; i++)
            distance[i, 0] = i;
        
        for (var j = 0; j <= targetLength; j++)
            distance[0, j] = j;

        // Calculate distances
        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[sourceLength, targetLength];
    }

    /// <summary>
    /// Finds the closest matching keyword from a list of candidates based on Levenshtein distance.
    /// </summary>
    /// <param name="input">The input string (potentially mistyped keyword).</param>
    /// <param name="candidates">The list of valid keywords to compare against.</param>
    /// <param name="maxDistance">Maximum allowed distance for a suggestion (default 3).</param>
    /// <returns>A list of suggestions ordered by similarity, or empty if no good matches found.</returns>
    public static List<string> FindClosestMatches(string input, IEnumerable<string> candidates, int maxDistance = 3)
    {
        if (string.IsNullOrEmpty(input) || candidates == null)
            return new List<string>();

        var inputLower = input.ToLowerInvariant();
        var matches = new List<(string keyword, int distance)>();

        foreach (var candidate in candidates)
        {
            var candidateLower = candidate.ToLowerInvariant();
            var distance = LevenshteinDistance(inputLower, candidateLower);
            
            // Only consider reasonable matches
            if (distance <= maxDistance && distance < inputLower.Length)
            {
                matches.Add((candidate, distance));
            }
        }

        return matches
            .OrderBy(m => m.distance)
            .ThenBy(m => m.keyword)
            .Select(m => m.keyword)
            .Take(3)  // Return top 3 suggestions
            .ToList();
    }
}
