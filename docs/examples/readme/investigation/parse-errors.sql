param(root: string = './docs/examples/readme/investigation')

text Entry {
    At: until ' ',
    Level: until ' ',
    Service: until ' ',
    Message: rest trim
};

select
    e.Service as Service,
    e.At::datetime as ErrorTime,
    e.Message as Message
from search.lines($root, ' ERROR ',
    options: (scope: (include: array { '*.log' }))) s
cross apply Parse<Entry>(s.LineText) e
order by Service, ErrorTime
