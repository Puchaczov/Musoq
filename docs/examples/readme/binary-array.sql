binary Batch {
    Count: byte,
    Items: short le[Count]
};

with input as (
    select FromHexToBytes('0207002A00') as Data
    from system.dual()
)
select item.Value as Value
from input i
cross apply Interpret<Batch>(i.Data) b
cross apply b.Items item
order by Value
