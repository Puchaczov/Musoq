select a.Name as Name, b.Score as Score
from values { (Id: 1, Name: 'A'), (Id: 2, Name: 'B') } a
left join values { (Id: 1, Score: 30) } b on a.Id = b.Id
order by Name
