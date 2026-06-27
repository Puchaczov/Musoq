namespace Musoq.Evaluator.Tests;

public partial class CrossApplyUnusedAliasTests
{
    public class CrossApplyClass1
    {
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int Population { get; set; }
        public int[] Values { get; set; } = [];
    }

    public class CrossApplyClass2
    {
        public string Country { get; set; } = string.Empty;
        public decimal Money { get; set; }
        public string Month { get; set; } = string.Empty;
    }

    public class CrossApplyClass3
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class CrossApplyMultiProperty
    {
        public string Name { get; set; } = string.Empty;
        public int[] Values1 { get; set; } = [];
        public int[] Values2 { get; set; } = [];
        public int[] Values3 { get; set; } = [];
    }

    public class CrossApplyNestedProperty
    {
        public string Name { get; set; } = string.Empty;
        public NestedValue[] NestedValues { get; set; } = [];
    }

    public class NestedValue
    {
        public int Value { get; set; }
    }
}
