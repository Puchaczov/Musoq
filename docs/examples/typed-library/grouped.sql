select Customer, Sum(Amount) as Total, PositiveCount(Amount) as Positive,
    Avg(Quantity) as AverageQuantity
from #schema.first()
group by Customer
order by Customer
