using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Musoq.Converter;
using Musoq.Evaluator;

namespace Musoq.Playground;

public class Program
{
    public static void Main(string[] args)
    {
        var directScript2 = @"
select a.Id, a.Name
from #test.entities() a
inner join #test.entities() b on a.Id = b.Id";

        var directScript4 = @"
select a.Id, a.Name
from #test.entities() a
inner join #test.entities() b on a.Id = b.Id
inner join #test.entities() c on a.Id = c.Id
inner join #test.entities() d on a.Id = d.Id";

        var cteScript4 = @"
with cte1 as (select Id, Name from #test.entities()),
cte2 as (select Id, Name from #test.entities()),
cte3 as (select Id, Name from #test.entities()),
cte4 as (select Id, Name from #test.entities())
select a.Id, a.Name
from cte1 a
inner join cte2 b on a.Id = b.Id
inner join cte3 c on a.Id = c.Id
inner join cte4 d on a.Id = d.Id";

        var entities = Enumerable.Range(0, 100).Select(i => new NonEquiEntity
        {
            Id = i,
            Name = $"Name{i}",
            Population = i
        }).ToList();

        var provider = new NonEquiSchemaProvider(entities, 0);

        Console.WriteLine("=== Testing Join Performance ===");
        Console.WriteLine();


        Console.WriteLine("--- DIRECT JOIN (2 tables, 10k rows each) ---");
        ExpensiveCteCounter.Reset();
        var options = new CompilationOptions(
            useHashJoin: true,
            useSortMergeJoin: true);

        var query2 = InstanceCreator.CompileForExecution(
            directScript2,
            Guid.NewGuid().ToString(),
            provider,
            new MyLoggerResolver(),
            options);

        var sw = Stopwatch.StartNew();
        var result2 = query2.Run();
        sw.Stop();
        Console.WriteLine(
            $"  Time: {sw.ElapsedMilliseconds}ms, Rows: {result2.Count}, Enumerations: {ExpensiveCteCounter.GetCount()}");


        Console.WriteLine("--- DIRECT JOIN (4 tables, 10k rows each) ---");
        ExpensiveCteCounter.Reset();

        var query4 = InstanceCreator.CompileForExecution(
            directScript4,
            Guid.NewGuid().ToString(),
            provider,
            new MyLoggerResolver(),
            options);

        sw.Restart();
        var result4 = query4.Run();
        sw.Stop();
        Console.WriteLine(
            $"  Time: {sw.ElapsedMilliseconds}ms, Rows: {result4.Count}, Enumerations: {ExpensiveCteCounter.GetCount()}");


        Console.WriteLine("--- CTE JOIN (4 CTEs, 10k rows each) ---");
        ExpensiveCteCounter.Reset();

        var queryCte = InstanceCreator.CompileForExecution(
            cteScript4,
            Guid.NewGuid().ToString(),
            provider,
            new MyLoggerResolver(),
            options);

        sw.Restart();
        var resultCte = queryCte.Run();
        sw.Stop();
        Console.WriteLine(
            $"  Time: {sw.ElapsedMilliseconds}ms, Rows: {resultCte.Count}, Enumerations: {ExpensiveCteCounter.GetCount()}");
    }
}

public class NoOpLogger<T> : NoOpLogger, ILogger<T>
{
}
