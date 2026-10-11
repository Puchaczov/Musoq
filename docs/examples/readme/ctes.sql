with totals as (
    select s.Name as Name, Sum(s.Amount) as Total
    from values {
        (Name: 'A', Amount: 10),
        (Name: 'A', Amount: 15),
        (Name: 'B', Amount: 5)
    } s
    group by s.Name
)
select Name, Total
from totals
where Total >= 20
order by Name
