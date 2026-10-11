select a.Name as Name
from values { (Id: 1, Name: 'A'), (Id: 2, Name: 'B') } a
anti join values { (Id: 1) } b on a.Id = b.Id
order by Name
