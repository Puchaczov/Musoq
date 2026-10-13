select s.Name as Name
from values {
    (Name: 'api.cs', Kind: 'source'),
    (Name: 'app.cs', Kind: 'source'),
    (Name: 'notes.txt', Kind: 'document')
} s
where s.Name like 'a%.cs' and s.Kind in ('source', 'test')
order by Name
