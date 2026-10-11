select
    s.Category as Category,
    s.Name as Name,
    s.Score as Score,
    RowNumber() over (
        partition by s.Category
        order by s.Score desc, s.Name
    ) as Position
from values {
    (Category: 'Books', Name: 'A', Score: 30),
    (Category: 'Books', Name: 'B', Score: 10),
    (Category: 'Games', Name: 'C', Score: 20),
    (Category: 'Games', Name: 'D', Score: 15)
} s
qualify Position = 1
order by Category
