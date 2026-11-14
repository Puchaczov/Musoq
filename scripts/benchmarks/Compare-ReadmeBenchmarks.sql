-- Generate-ReadmeCharts.py binds this compile-time CSV path after validating campaign compatibility.
let measurementsPath: string = r'__MEASUREMENTS_PATH__'

table MeasurementRows {
    Sequence: int,
    VersionLabel: string,
    CaseId: string,
    MeanNanoseconds: double,
    AllocatedBytes: double
};
couple #csv.file with table MeasurementRows as Measurements;

with measurements as (
    select
        r.Sequence::int as Sequence,
        r.VersionLabel as VersionLabel,
        r.CaseId as CaseId,
        r.MeanNanoseconds::double as MeanNanoseconds,
        r.AllocatedBytes::double as AllocatedBytes
    from Measurements($measurementsPath, true) r
), previous as (
    select
        m.Sequence as Sequence,
        m.VersionLabel as VersionLabel,
        m.CaseId as CaseId,
        m.MeanNanoseconds as MeanNanoseconds,
        m.AllocatedBytes as AllocatedBytes,
        Lag(m.MeanNanoseconds) over (partition by m.CaseId order by m.Sequence) as PreviousMean,
        Lag(m.AllocatedBytes) over (partition by m.CaseId order by m.Sequence) as PreviousAllocated
    from measurements m
)
select
    p.Sequence as Sequence,
    p.VersionLabel as VersionLabel,
    p.CaseId as CaseId,
    p.MeanNanoseconds as MeanNanoseconds,
    p.AllocatedBytes as AllocatedBytes,
    case when p.PreviousMean is null then null
         else (p.MeanNanoseconds / p.PreviousMean - 1::double) * 100::double end as TimeChangePercent,
    case when p.PreviousAllocated is null then null
         else (p.AllocatedBytes / p.PreviousAllocated - 1::double) * 100::double end as AllocationChangePercent
from previous p
order by CaseId, Sequence
