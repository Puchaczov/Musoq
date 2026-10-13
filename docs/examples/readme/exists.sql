select c.Name as Name
from values { (Id: 1, Name: 'Ada'), (Id: 2, Name: 'Ben') } c
where exists (
    select 1
    from values { (CustomerId: 1) } o
    where o.CustomerId = c.Id
)
order by Name
