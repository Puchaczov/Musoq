namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    public sealed class Person
    {
        public string Name { get; init; } = string.Empty;

        public int Age { get; init; }
    }

    public sealed class Order
    {
        public int PersonAge { get; init; }

        public string Description { get; init; } = string.Empty;
    }

    public sealed class ApplyItem
    {
        public string Name { get; init; } = string.Empty;

        public int[] Numbers { get; init; } = [];
    }
}
