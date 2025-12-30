using System;
using System.Collections.Generic;
using System.Text;
using DiffPlex;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

/// <summary>
/// Provides character-level text diff functionality.
/// </summary>
public partial class LibraryBase
{
    private const string KindUnchanged = "Unchanged";
    private const string KindDeleted = "Deleted";
    private const string KindInserted = "Inserted";
    
    /// <summary>
    /// Compares two strings at the character level and returns a human-readable diff string.
    /// </summary>
    /// <param name="first">The first (original) string.</param>
    /// <param name="second">The second (modified) string.</param>
    /// <param name="mode">
    /// The output mode:
    /// - "full" (default): show all unchanged text literally
    /// - "compact": collapse unchanged regions to [=N]
    /// - "full:N": like full, but collapse unchanged regions longer than N chars
    /// </param>
    /// <returns>
    /// A diff string with markers:
    /// - [-text] for deleted text
    /// - [+text] for inserted text
    /// - [=N] for N unchanged characters (in compact mode or when threshold exceeded)
    /// Returns null if both inputs are null or if mode is invalid.
    /// </returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Diff(string? first, string? second, string? mode = "full")
    {
        if (first == null && second == null)
            return null;

        var parsedMode = ParseDiffMode(mode);
        if (parsedMode == null)
            return null;

        var (modeName, threshold) = parsedMode.Value;

        if (first == null)
            return $"[+{second}]";
        if (second == null)
            return $"[-{first}]";

        if (first.Length == 0 && second.Length == 0)
            return string.Empty;

        if (first.Length == 0)
            return $"[+{second}]";

        if (second.Length == 0)
            return $"[-{first}]";

        if (first == second)
        {
            if (modeName == "compact")
                return $"[={first.Length}]";
            return first;
        }

        var segments = ComputeCharacterDiff(first, second);

        return BuildDiffString(segments, modeName, threshold);
    }

    /// <summary>
    /// Compares two strings at the character level and returns structured segments.
    /// Useful for cross apply queries to filter and aggregate changes.
    /// </summary>
    /// <param name="first">The first (original) string.</param>
    /// <param name="second">The second (modified) string.</param>
    /// <returns>
    /// An enumerable of DiffSegmentEntity objects, each containing:
    /// - Text: the segment content
    /// - Kind: "Unchanged", "Deleted", or "Inserted"
    /// - Position: start position (in source for Deleted/Unchanged, in target for Inserted)
    /// - Length: character count
    /// Returns an empty enumerable if both inputs are null or both are empty.
    /// </returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public IEnumerable<DiffSegmentEntity> DiffSegments(string? first, string? second)
    {
        if (first == null && second == null)
            return [];

        if (first == null)
            return [new DiffSegmentEntity(second!, KindInserted, 0, second!.Length)];

        if (second == null)
            return [new DiffSegmentEntity(first, KindDeleted, 0, first.Length)];

        if (first.Length == 0 && second.Length == 0)
            return [];

        if (first.Length == 0)
            return [new DiffSegmentEntity(second, KindInserted, 0, second.Length)];

        if (second.Length == 0)
            return [new DiffSegmentEntity(first, KindDeleted, 0, first.Length)];

        if (first == second)
            return [new DiffSegmentEntity(first, KindUnchanged, 0, first.Length)];

        return ComputeCharacterDiff(first, second);
    }

    private static (string modeName, int? threshold)? ParseDiffMode(string? mode)
    {
        if (mode == null)
            return ("full", null);

        if (mode.Length == 0)
            return null;

        if (mode == "full")
            return ("full", null);

        if (mode == "compact")
            return ("compact", null);

        if (mode.StartsWith("full:", StringComparison.Ordinal))
        {
            var thresholdStr = mode.Substring(5);
            if (int.TryParse(thresholdStr, out var threshold) && threshold > 0)
                return ("full:N", threshold);
            return null;
        }

        return null;
    }

    private static List<DiffSegmentEntity> ComputeCharacterDiff(string first, string second)
    {
        var differ = new Differ();
        var diffResult = differ.CreateCharacterDiffs(first, second, false);
        
        var segments = new List<DiffSegmentEntity>();
        var sourcePos = 0;
        var targetPos = 0;
        var unchangedBuilder = new StringBuilder();
        var unchangedStartSourcePos = 0;
        
        var diffBlockIndex = 0;
        var oldIndex = 0;
        var newIndex = 0;
        
        while (oldIndex < first.Length || newIndex < second.Length)
        {
            DiffPlex.Model.DiffBlock? currentBlock = null;
            if (diffBlockIndex < diffResult.DiffBlocks.Count)
            {
                currentBlock = diffResult.DiffBlocks[diffBlockIndex];
            }
            
            if (currentBlock == null)
            {
                if (oldIndex < first.Length)
                {
                    var remainingUnchanged = first.Substring(oldIndex);
                    if (unchangedBuilder.Length == 0)
                        unchangedStartSourcePos = sourcePos;
                    unchangedBuilder.Append(remainingUnchanged);
                    sourcePos += remainingUnchanged.Length;
                    targetPos += remainingUnchanged.Length;
                    oldIndex = first.Length;
                    newIndex = second.Length;
                }
                break;
            }
            
            while (oldIndex < currentBlock.DeleteStartA)
            {
                if (unchangedBuilder.Length == 0)
                    unchangedStartSourcePos = sourcePos;
                unchangedBuilder.Append(first[oldIndex]);
                oldIndex++;
                newIndex++;
                sourcePos++;
                targetPos++;
            }
            
            if (unchangedBuilder.Length > 0)
            {
                segments.Add(new DiffSegmentEntity(
                    unchangedBuilder.ToString(),
                    KindUnchanged,
                    unchangedStartSourcePos,
                    unchangedBuilder.Length));
                unchangedBuilder.Clear();
            }
            
            if (currentBlock.DeleteCountA > 0)
            {
                var deletedText = first.Substring(currentBlock.DeleteStartA, currentBlock.DeleteCountA);
                segments.Add(new DiffSegmentEntity(
                    deletedText,
                    KindDeleted,
                    sourcePos,
                    deletedText.Length));
                sourcePos += currentBlock.DeleteCountA;
                oldIndex += currentBlock.DeleteCountA;
            }
            
            if (currentBlock.InsertCountB > 0)
            {
                var insertedText = second.Substring(currentBlock.InsertStartB, currentBlock.InsertCountB);
                segments.Add(new DiffSegmentEntity(
                    insertedText,
                    KindInserted,
                    targetPos,
                    insertedText.Length));
                targetPos += currentBlock.InsertCountB;
                newIndex += currentBlock.InsertCountB;
            }
            
            diffBlockIndex++;
        }
        
        if (unchangedBuilder.Length > 0)
        {
            segments.Add(new DiffSegmentEntity(
                unchangedBuilder.ToString(),
                KindUnchanged,
                unchangedStartSourcePos,
                unchangedBuilder.Length));
        }
        
        return segments;
    }

    private static string BuildDiffString(List<DiffSegmentEntity> segments, string modeName, int? threshold)
    {
        var result = new StringBuilder();
        
        foreach (var segment in segments)
        {
            switch (segment.Kind)
            {
                case KindUnchanged:
                    if (modeName == "compact")
                    {
                        result.Append($"[={segment.Length}]");
                    }
                    else if (modeName == "full:N" && threshold.HasValue && segment.Length > threshold.Value)
                    {
                        result.Append($"[={segment.Length}]");
                    }
                    else
                    {
                        result.Append(segment.Text);
                    }
                    break;
                    
                case KindDeleted:
                    result.Append($"[-{segment.Text}]");
                    break;
                    
                case KindInserted:
                    result.Append($"[+{segment.Text}]");
                    break;
            }
        }
        
        return result.ToString();
    }
}
