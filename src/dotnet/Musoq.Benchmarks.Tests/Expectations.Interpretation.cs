namespace Musoq.Benchmarks.Tests;

public static partial class Expectations
{
    private static void AddInterpretation(Dictionary<string, ExpectedResult> results)
    {
        var id = ("Id", typeof(int));
        results.Add("B23_Parameters", new([id], [[1], [2], [3]]));
        results.Add("B23_StructuredInput", new([id, ("Amount", typeof(decimal))], [[1, 2m], [3, 4m]]));
        results.Add("B25_Couple", new([id, ("Name", typeof(string))], [[0, "item-0"], [1, "item-1"], [2, "item-2"], [3, "item-3"]]));
        results.Add("B26_AcceptedPredicate", new([id], [[1]]));
        results.Add("B26_ResidualPredicate", new([id], [[1]]));
        results.Add("B27_BinaryHeader", new([id, ("Value", typeof(int)), ("Kind", typeof(byte))], [[0, 1, (byte)0], [1, 2, (byte)1], [2, 3, (byte)2], [3, 4, (byte)3]]));
        results.Add("B28_BinarySubstream", new([id, ("Value", typeof(int))], [[0, 1], [1, 2], [2, 3], [3, 4]]));
        results.Add("B28_BinarySwitch", new([id, ("Kind", typeof(string))], [[0, "Known"], [1, "Raw"], [2, "Raw"], [3, "Raw"]]));
        results.Add("B29_BinaryRepeat", new([id, ("Count", typeof(int?))], [[0, 2], [1, 2], [2, 2], [3, 2]]));
        results.Add("B29_InterpretAt", new([id, ("Value", typeof(int))], [[0, 1], [1, 2], [2, 3], [3, 4]]));
        results.Add("B30_TextDelimited", new([id, ("Level", typeof(string)), ("Label", typeof(string))], [[0, "INFO", "item-0"], [1, "INFO", "item-1"], [2, "INFO", "item-2"], [3, "INFO", "item-3"]]));
        results.Add("B31_TextCaptures", new([id, ("Label", typeof(string))], [[0, "item-0"], [1, "item-1"], [2, "item-2"], [3, "item-3"]]));
        results.Add("B32_TryParse", new([id, ("Message", typeof(string))], [[0, "0|item-0"], [1, null], [2, "2|item-2"], [3, null]]));
        results.Add("B32_Partial", new([id, ("Kind", typeof(byte)), ("Consumed", typeof(int)), ("Error", typeof(string))], [[0, (byte)0, 1, "Value"], [1, (byte)1, 1, "Value"], [2, (byte)2, 1, "Value"], [3, (byte)3, 1, "Value"]]));
    }
}
