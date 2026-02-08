using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.NegativeTests;

public class TypesEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap;
    public static readonly IReadOnlyDictionary<int, Func<TypesEntity, object>> IndexToObjectAccessMap;

    static TypesEntity()
    {
        NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(IntCol), 0 },
            { nameof(LongCol), 1 },
            { nameof(ShortCol), 2 },
            { nameof(ByteCol), 3 },
            { nameof(DecimalCol), 4 },
            { nameof(DoubleCol), 5 },
            { nameof(FloatCol), 6 },
            { nameof(BoolCol), 7 },
            { nameof(StringCol), 8 },
            { nameof(DateTimeCol), 9 },
            { nameof(DateTimeOffsetCol), 10 },
            { nameof(GuidCol), 11 },
            { nameof(NullableIntCol), 12 }
        };

        IndexToObjectAccessMap = new Dictionary<int, Func<TypesEntity, object>>
        {
            { 0, entity => entity.IntCol },
            { 1, entity => entity.LongCol },
            { 2, entity => entity.ShortCol },
            { 3, entity => entity.ByteCol },
            { 4, entity => entity.DecimalCol },
            { 5, entity => entity.DoubleCol },
            { 6, entity => entity.FloatCol },
            { 7, entity => entity.BoolCol },
            { 8, entity => entity.StringCol },
            { 9, entity => entity.DateTimeCol },
            { 10, entity => entity.DateTimeOffsetCol },
            { 11, entity => entity.GuidCol },
            { 12, entity => entity.NullableIntCol }
        };
    }

    public int IntCol { get; set; }
    
    public long LongCol { get; set; }
    
    public short ShortCol { get; set; }
    
    public byte ByteCol { get; set; }
    
    public decimal DecimalCol { get; set; }
    
    public double DoubleCol { get; set; }
    
    public float FloatCol { get; set; }
    
    public bool BoolCol { get; set; }
    
    public string StringCol { get; set; }
    
    public DateTime DateTimeCol { get; set; }
    
    public DateTimeOffset DateTimeOffsetCol { get; set; }
    
    public Guid GuidCol { get; set; }
    
    public int? NullableIntCol { get; set; }
}
