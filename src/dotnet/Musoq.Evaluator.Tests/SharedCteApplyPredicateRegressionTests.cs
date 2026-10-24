using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SharedCteApplyPredicateRegressionTests
{
    [TestMethod]
    [DataRow("with base as (select x.Value as K, ToString(x.Value) + ',1' as V from values { (Value: 0), (Value: 1), (Value: 2), (Value: 3) } x), a as (select b.K as K, AggregateValues(c.Value, '') as T from base b cross apply Split(b.V, ',') c where b.K < 2 group by b.K), p as (select b.K as K, b.V as T from base b where b.K >= 2) select a.K, a.T, p.T as PT from a a left outer join p p on a.K = p.K", "0|01|NULL;1|11|NULL")]
    [DataRow("with base as (select x.Value as K, ToString(x.Value) + ',9' as V from values { (Value: 0), (Value: 1) } x), d as (select b.K as K from base b where b.K = 0), h as (select b.K as K, c.Value as N from base b cross apply Split(b.V, ',') c) select h.K, h.N, d.K as DK from h h left outer join d d on h.K = d.K", "0|0|0;0|9|0;1|1|NULL;1|9|NULL")]
    [DataRow("with base as (select x.Value as K from values { (Value: 0), (Value: 1) } x), d as (select b.K as K from base b where b.K = 0), h as (select b.K as K from base b cross apply Split('x', ',') c) select h.K, d.K as DK from h h left outer join d d on h.K = d.K", "0|0;1|NULL")]
    [DataRow("with base as (select x.Value as K from values { (Value: 0), (Value: 1), (Value: 2), (Value: 3) } x), a as (select b.K as K from base b cross apply Split(ToString(b.K), ',') c where b.K < 2), p as (select b.K as K from base b where b.K >= 2) select a.K, p.K as PK from a a left outer join p p on a.K = p.K", "0|NULL;1|NULL")]
    public void SharedBaseCte_ShouldKeepSiblingPredicatesIndependent(string sql, string expected)
    {
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), new TestSchemaProvider(),
            new TestsLoggerResolver(), new CompilationOptions());
        using var table = query.Run();
        var values = string.Join(";", table.Select(row => string.Join("|", row.ReadValues().Select(value =>
            value is null ? "NULL" : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture))))
            .OrderBy(value => value, StringComparer.Ordinal));
        Assert.AreEqual(expected, values);
    }
}
