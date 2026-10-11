param(minScore: int = 20)
let category: string = 'Books'

select s.Name as Name
from values {
    (Category: 'Books', Name: 'A', Score: 30),
    (Category: 'Books', Name: 'B', Score: 10)
} s
where s.Category = $category and s.Score >= $minScore
order by Name
