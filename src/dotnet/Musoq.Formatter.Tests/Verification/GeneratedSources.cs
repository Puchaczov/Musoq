using System;
using System.Collections.Generic;

namespace Musoq.Formatter.Tests.Verification;

public static class GeneratedSources
{
    public static IEnumerable<object[]> Cases()
    {
        for (var seed = 0; seed < 128; seed++)
            yield return [seed, Build(seed)];
    }

    private static string Build(int seed)
    {
        var random = new Random(seed);
        var space = random.Next(2) == 0 ? "  " : "\n\t";
        var value = random.Next(1, 200);
        var query = (seed % 6) switch
        {
            0 => $"select case a.Flag when 1 then (a.Value+{value}) else -0xFF end Label from #s.rows() a where a.Value>{value} and (a.Flag=1 or a.Flag=2)",
            1 => $"select array {{ {value}, 0xFF, 2ul }}, (Name: 'Keep AND Text', Value: {value}) from #s.rows() a",
            2 => $"select Sum(a.Value) over (order by a.Name rows between {value} preceding and current row) Total from #s.rows() a",
            3 => $"with c (Label) as (select a.Name from #s.rows() a) select Label from c union (Label) select Label from c",
            4 => $"let threshold: int = {value}; select a.Value from #s.rows() a where a.Value between $threshold and 300 and a.Flag!=0",
            _ => $"binary Packet {{ Count: byte, Values: byte[Count] }}; select p.Count from #s.rows() a cross apply Interpret<Packet>(a.Bytes) p"
        };
        return query.Replace("select ", "sElEcT" + space, StringComparison.Ordinal)
            .Replace(" from ", space + "FrOm" + space, StringComparison.Ordinal);
    }
}
