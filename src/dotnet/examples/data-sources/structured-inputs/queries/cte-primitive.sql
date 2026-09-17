with numbers as (
    select p.Value
    from values {
        (Value: 1),
        (Value: 2),
        (Value: 2),
    } p
)
select n.Value
from #inputs.numbers(values: numbers) n
