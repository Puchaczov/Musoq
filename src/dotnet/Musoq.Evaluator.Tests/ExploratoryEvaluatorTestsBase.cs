using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Base class for exploratory evaluator tests with shared test data classes.
/// </summary>
public abstract class ExploratoryEvaluatorTestsBase : GenericEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region Test Data Classes

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string[] Tags { get; set; }
        public int[] Scores { get; set; }
        public Address[] Addresses { get; set; }
        public Person Manager { get; set; }
    }

    public class Address
    {
        public string City { get; set; }
        public string Street { get; set; }
        public string[] PhoneNumbers { get; set; }
    }

    public class Order
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public OrderItem[] Items { get; set; }
        public decimal Total { get; set; }
    }

    public class OrderItem
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class TreeNode
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public TreeNode[] Children { get; set; }
    }

    #endregion
}
