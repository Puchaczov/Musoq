using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class QueryPhaseTrackingTests : BasicEntityTestBase
{
    [TestMethod]
    public void SimpleQuery_ShouldFireAllPhases()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var phases = new List<(string QueryId, QueryPhase Phase)>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => phases.Add((args.QueryId, args.Phase));

        _ = vm.Run().Count;

        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.Begin));
        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.From));
        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.Select));
        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.End));
    }

    [TestMethod]
    public void QueryWithWhere_ShouldFireWherePhase()
    {
        var query = "select Name from #A.Entities() where Name = '001'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var phases = new List<(string QueryId, QueryPhase Phase)>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => phases.Add((args.QueryId, args.Phase));

        _ = vm.Run().Count;

        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.Begin));
        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.From));
        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.Where));
        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.Select));
        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.End));
    }

    [TestMethod]
    public void QueryWithGroupBy_ShouldFireGroupByPhase()
    {
        var query = "select Name, Count(Name) from #A.Entities() group by Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("001"), new BasicEntity("002")] }
        };

        var phases = new List<(string QueryId, QueryPhase Phase)>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => phases.Add((args.QueryId, args.Phase));

        _ = vm.Run().Count;

        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.Begin));
        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.GroupBy));
        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.End));
    }

    [TestMethod]
    public void QueryWithGroupByAndWhere_ShouldFireBothPhases()
    {
        var query = "select Name, Count(Name) from #A.Entities() where Name <> 'skip' group by Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("skip"), new BasicEntity("002")] }
        };

        var phases = new List<(string QueryId, QueryPhase Phase)>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => phases.Add((args.QueryId, args.Phase));

        _ = vm.Run().Count;

        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.Begin));
        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.Where));
        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.GroupBy));
        Assert.IsTrue(phases.Any(p => p.Phase == QueryPhase.End));
    }

    [TestMethod]
    public void AggregateOnlyQuery_ShouldNotReportGroupBy()
    {
        var query = "select Count(Name) from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var phases = new List<QueryPhase>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => phases.Add(args.Phase);

        _ = vm.Run().Count;

        Assert.IsFalse(
            phases.Contains(QueryPhase.GroupBy),
            string.Join(", ", phases));
        Assert.IsTrue(phases.Contains(QueryPhase.Select));
        Assert.IsTrue(phases.Contains(QueryPhase.End));
    }

    [TestMethod]
    public void ProjectionCaseGuard_ShouldNotReportWhere()
    {
        var query = "select case when Name = '001' then 'match' else 'other' end from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var phases = new List<QueryPhase>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => phases.Add(args.Phase);

        _ = vm.Run().Count;

        Assert.IsFalse(phases.Contains(QueryPhase.Where));
        Assert.IsTrue(phases.Contains(QueryPhase.Select));
        Assert.IsTrue(phases.Contains(QueryPhase.End));
    }

    [TestMethod]
    public void FailedProjection_ShouldStillTerminateTheQueryScope()
    {
        var query = "select e.ThrowException() from #A.Entities() e";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var phases = new List<QueryPhase>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => phases.Add(args.Phase);

        Assert.Throws<Exception>(() => _ = vm.Run().Count);

        Assert.AreEqual(1, phases.Count(phase => phase == QueryPhase.Begin));
        Assert.AreEqual(1, phases.Count(phase => phase == QueryPhase.End));
    }

    [TestMethod]
    public void HavingFilter_ShouldNotReportWhere()
    {
        var query = "select Name, Count(Name) from #A.Entities() group by Name having Count(Name) > 1";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("001"), new BasicEntity("002")] }
        };

        var phases = new List<QueryPhase>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => phases.Add(args.Phase);

        _ = vm.Run().Count;

        Assert.IsFalse(phases.Contains(QueryPhase.Where));
        Assert.IsTrue(phases.Contains(QueryPhase.GroupBy));
        Assert.IsTrue(phases.Contains(QueryPhase.Select));
        Assert.IsTrue(phases.Contains(QueryPhase.End));
    }

    [TestMethod]
    public void PhasesFireInCorrectOrder_SimpleQuery()
    {
        var query = "select Name from #A.Entities() where Name = '001'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var phases = new List<QueryPhase>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => phases.Add(args.Phase);

        _ = vm.Run().Count;

        var beginIndex = phases.IndexOf(QueryPhase.Begin);
        var fromIndex = phases.IndexOf(QueryPhase.From);
        var whereIndex = phases.IndexOf(QueryPhase.Where);
        var selectIndex = phases.IndexOf(QueryPhase.Select);
        var endIndex = phases.IndexOf(QueryPhase.End);

        Assert.IsLessThan(fromIndex, beginIndex);
        Assert.IsLessThan(whereIndex, fromIndex);
        Assert.IsLessThan(selectIndex, whereIndex);
        Assert.IsLessThan(endIndex, selectIndex);
    }

    [TestMethod]
    public void QueryIdIsProvided()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var queryIds = new HashSet<string>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => queryIds.Add(args.QueryId);

        _ = vm.Run().Count;

        Assert.IsNotEmpty(queryIds);
        Assert.IsTrue(queryIds.All(id => !string.IsNullOrEmpty(id)));
    }

    [TestMethod]
    public void NoHandlerAttached_QueryStillRuns()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var result = vm.Run();

        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public void UnionQuery_ShouldFirePhasesForBothQueries()
    {
        var query = @"
select Name from #A.Entities() where Name = '001'
union (Name)
select Name from #A.Entities() where Name = '002'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var phases = new List<(string QueryId, QueryPhase Phase)>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => phases.Add((args.QueryId, args.Phase));

        _ = vm.Run().Count;

        var beginCount = phases.Count(p => p.Phase == QueryPhase.Begin);
        var endCount = phases.Count(p => p.Phase == QueryPhase.End);

        Assert.IsGreaterThanOrEqualTo(2, beginCount);
        Assert.IsGreaterThanOrEqualTo(2, endCount);
    }

    [TestMethod]
    public void UnionQuery_ShouldHaveUniqueQueryIds()
    {
        var query = @"
select Name from #A.Entities() where Name = '001'
union (Name)
select Name from #A.Entities() where Name = '002'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var queryIdsByPhase = new Dictionary<QueryPhase, HashSet<string>>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) =>
        {
            if (!queryIdsByPhase.ContainsKey(args.Phase))
                queryIdsByPhase[args.Phase] = [];
            queryIdsByPhase[args.Phase].Add(args.QueryId);
        };

        _ = vm.Run().Count;

        var beginQueryIds = queryIdsByPhase.GetValueOrDefault(QueryPhase.Begin, []);
        Assert.IsGreaterThanOrEqualTo(2, beginQueryIds.Count,
            "UNION should have at least 2 unique query IDs for Begin phase");
    }

    [TestMethod]
    public void CteQuery_ShouldFirePhasesWithDistinctQueryIds()
    {
        var query = @"
with cte as (
    select Name from #A.Entities()
)
select Name from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var phases = new List<(string QueryId, QueryPhase Phase)>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => phases.Add((args.QueryId, args.Phase));

        _ = vm.Run().Count;

        var beginCount = phases.Count(p => p.Phase == QueryPhase.Begin);
        var endCount = phases.Count(p => p.Phase == QueryPhase.End);

        Assert.IsGreaterThanOrEqualTo(1, beginCount);
        Assert.IsGreaterThanOrEqualTo(1, endCount);
    }

    [TestMethod]
    public void CteQuery_ShouldHaveUniqueQueryIds()
    {
        var query = @"
with cte as (
    select Name from #A.Entities()
)
select Name from cte";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var allQueryIds = new HashSet<string>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => allQueryIds.Add(args.QueryId);

        _ = vm.Run().Count;

        Assert.IsGreaterThanOrEqualTo(2, allQueryIds.Count,
            "CTE query should have at least 2 unique query IDs (one for CTE, one for main query)");
    }

    [TestMethod]
    public void MultipleCtesQuery_ShouldHaveUniqueQueryIdsForEachCte()
    {
        var query = @"
with cte1 as (
    select Name from #A.Entities() where Name = '001'
), cte2 as (
    select Name from #A.Entities() where Name = '002'
)
select c1.Name from cte1 c1 inner join cte2 c2 on c1.Name = c2.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var allQueryIds = new HashSet<string>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) => allQueryIds.Add(args.QueryId);

        _ = vm.Run().Count;

        Assert.IsGreaterThanOrEqualTo(3, allQueryIds.Count,
            "Multiple CTEs query should have at least 3 unique query IDs (one for each CTE and one for main query)");
    }

    [TestMethod]
    public void EventArgsContainCorrectData()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        QueryPhaseEventArgs? capturedArgs = null;
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, args) =>
        {
            if (args.Phase == QueryPhase.Begin)
                capturedArgs = args;
        };

        _ = vm.Run().Count;

        Assert.IsNotNull(capturedArgs);
        var phaseArgs = capturedArgs ?? throw new AssertFailedException("Expected begin phase args.");
        Assert.AreEqual(QueryPhase.Begin, phaseArgs.Phase);
        Assert.IsFalse(string.IsNullOrEmpty(phaseArgs.QueryId));
    }

    [TestMethod]
    public void SenderIsRunnable()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        object? capturedSender = null;
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (sender, _) =>
        {
            if (capturedSender == null)
                capturedSender = sender;
        };

        _ = vm.Run().Count;

        Assert.IsNotNull(capturedSender);
        Assert.IsInstanceOfType<ITableRunnable>(capturedSender);
    }

    [TestMethod]
    public void MultipleHandlers_AllReceiveEvents()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var handler1Count = 0;
        var handler2Count = 0;

        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += (_, _) => handler1Count++;
        vm.PhaseChanged += (_, _) => handler2Count++;

        _ = vm.Run().Count;

        Assert.IsGreaterThan(0, handler1Count);
        Assert.AreEqual(handler1Count, handler2Count);
    }

    [TestMethod]
    public void RemoveHandler_StopsReceivingEvents()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var handlerCount = 0;
        QueryPhaseEventHandler handler = (_, _) => handlerCount++;

        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.PhaseChanged += handler;
        vm.PhaseChanged -= handler;

        _ = vm.Run().Count;

        Assert.AreEqual(0, handlerCount);
    }
}
