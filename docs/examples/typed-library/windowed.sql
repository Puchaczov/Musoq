select Position, Sum(Amount) over (partition by Customer order by Position
    rows between unbounded preceding and current row) as RunningTotal
from #schema.first()
order by Position
