text Entry {
    Prefix: literal 'OK:',
    Message: rest
};

select s.Id as Id, p.Message as Message
from values {
    (Id: 1, Line: 'OK:ready'),
    (Id: 2, Line: 'bad')
} s
outer apply TryParse<Entry>(s.Line) p
order by s.Id
