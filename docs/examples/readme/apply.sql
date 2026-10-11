with source as (
    select s.Name as Name, array { 1, 2 } as Items
    from values { (Name: 'A') } s
)
select s.Name as Name, item.Value as Value
from source s
cross apply s.Items item
order by Value
