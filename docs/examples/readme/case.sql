select
    s.Name as Name,
    case when s.Score >= 20 then 'high' else 'low' end as Band
from values {
    (Name: 'Ada', Score: 30),
    (Name: 'Ben', Score: 10)
} s
order by Name
