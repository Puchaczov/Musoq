using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Entity with text content for testing Parse().
/// </summary>
public class TextEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
    {
        { nameof(Name), 0 },
        { nameof(Text), 1 },
        { nameof(Line), 1 } // Alias for Text
    };

    public static readonly IReadOnlyDictionary<int, Func<TextEntity, object>> IndexToObjectAccessMap =
        new Dictionary<int, Func<TextEntity, object>>
        {
            { 0, e => e.Name },
            { 1, e => e.Text }
        };

    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Line => Text;
}
