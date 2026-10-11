with combined as (
    select a.Value as Value
    from values { (Value: 1), (Value: 2) } a
    union all
    select b.Value as Value
    from values { (Value: 2), (Value: 3) } b
)
select Value
from combined
order by Value
