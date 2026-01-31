using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Entity with binary content for testing Interpret().
/// </summary>
public class BinaryEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
    {
        { nameof(Name), 0 },
        { nameof(Content), 1 },
        { nameof(Data), 1 } // Alias for Content
    };

    public static readonly IReadOnlyDictionary<int, Func<BinaryEntity, object>> IndexToObjectAccessMap =
        new Dictionary<int, Func<BinaryEntity, object>>
        {
            { 0, e => e.Name },
            { 1, e => e.Content }
        };

    public string Name { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public byte[] Data
    {
        get => Content;
        set => Content = value;
    }
}
