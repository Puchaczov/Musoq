using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.Basic;

/// <summary>
/// Test entity representing sales data for PIVOT testing scenarios
/// </summary>
public class SalesEntity
{
    public static readonly IDictionary<string, int> TestNameToIndexMap;
    public static readonly IDictionary<int, Func<SalesEntity, object>> TestIndexToObjectAccessMap;

    static SalesEntity()
    {
        TestNameToIndexMap = new Dictionary<string, int>
        {
            {nameof(Category), 0},
            {nameof(Product), 1},
            {nameof(Month), 2},
            {nameof(Quarter), 3},
            {nameof(Year), 4},
            {nameof(Quantity), 5},
            {nameof(Revenue), 6},
            {nameof(SalesDate), 7},
            {nameof(Region), 8},
            {nameof(Salesperson), 9}
        };

        TestIndexToObjectAccessMap = new Dictionary<int, Func<SalesEntity, object>>
        {
            {0, arg => arg.Category},
            {1, arg => arg.Product},
            {2, arg => arg.Month},
            {3, arg => arg.Quarter},
            {4, arg => arg.Year},
            {5, arg => arg.Quantity},
            {6, arg => arg.Revenue},
            {7, arg => arg.SalesDate},
            {8, arg => arg.Region},
            {9, arg => arg.Salesperson}
        };
    }

    public SalesEntity()
    {
    }

    public SalesEntity(string category, string product, int quantity, decimal revenue)
    {
        Category = category;
        Product = product;
        Quantity = quantity;
        Revenue = revenue;
    }

    public SalesEntity(string category, int quantity, decimal revenue) // Simplified constructor without product/month
    {
        Category = category;
        Quantity = quantity;
        Revenue = revenue;
    }

    public SalesEntity(string category, string product, string month, int quantity, decimal revenue)
    {
        Category = category;
        Product = product;
        Month = month;
        Quantity = quantity;
        Revenue = revenue;
    }

    public SalesEntity(string category, string product, string month, string region, int quantity, decimal revenue, DateTime salesDate)
    {
        Category = category;
        Product = product;
        Month = month;
        Region = region;
        Quantity = quantity;
        Revenue = revenue;
        SalesDate = salesDate;
    }

    public string Category { get; set; }
    public string Product { get; set; }
    public string Month { get; set; }
    public string Quarter { get; set; }
    public int Year { get; set; }
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
    public DateTime SalesDate { get; set; }
    public string Region { get; set; }
    public string Salesperson { get; set; }
}