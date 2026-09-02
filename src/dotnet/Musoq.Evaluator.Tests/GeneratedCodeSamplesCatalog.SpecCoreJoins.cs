namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateSpecificationCoreJoinSamples()
    {
        return
        [
            Basic(
                "Q278_SpecCoreDirectSemiJoin",
                "Join",
                "select a.Name from #A.entities() a semi join #B.entities() b on a.Id = b.Id"),
            Basic(
                "Q279_SpecCoreDirectSemiJoinResidual",
                "Join",
                "select a.Name from #A.entities() a semi join #B.entities() b " +
                "on a.Id = b.Id and b.Population > 100"),
            Basic(
                "Q280_SpecCoreDirectAntiJoin",
                "Join",
                "select a.Name from #A.entities() a anti join #B.entities() b on a.Id = b.Id"),
            Basic(
                "Q281_SpecCoreDirectAntiJoinResidual",
                "Join",
                "select a.Name from #A.entities() a anti join #B.entities() b " +
                "on a.Id = b.Id and b.Population > 100"),
            Basic(
                "Q282_SpecCoreDirectCrossJoin",
                "Join",
                "select a.Name, b.Name from #A.entities() a cross join #B.entities() b"),
            Basic(
                "Q283_SpecCoreFullOuterJoinRowPresence",
                "Join",
                "select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id = b.Id"),
            Basic(
                "Q284_SpecCoreFullOuterJoinNonEqui",
                "Join",
                "select a.Name, b.Name from #A.entities() a full outer join #B.entities() b on a.Id < b.Id"),
            Basic(
                "Q285_SpecCoreAsOfLeftJoinRowPresence",
                "Join",
                "select a.Name, b.Name from #A.entities() a asof left join #B.entities() b " +
                "on a.Population >= b.Population")
        ];
    }
}
