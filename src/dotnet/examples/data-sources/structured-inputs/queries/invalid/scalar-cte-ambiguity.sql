with items as (
    select p.Id, p.Pattern
    from values { (Id: 'todo', Pattern: 'TODO') } p
)
select a.Count
from #inputs.collectioncolumn() x
cross join #inputs.relationambiguity(items: items) a
