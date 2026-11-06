param(root: string = './docs/examples/readme/investigation')
let deployments: string = './docs/examples/readme/investigation/deployments.csv'

text Entry {
    At: until ' ',
    Level: until ' ',
    Service: until ' ',
    Message: rest trim
};

with errors as (
    select
        e.Service as Service,
        e.At::datetime as ErrorTime,
        e.Message as Message
    from search.lines($root, ' ERROR ',
        options: (scope: (include: array { '*.log' }))) s
    cross apply Parse<Entry>(s.LineText) e
), deployments as (
    select d.Service as Service, d.At::datetime as DeployedAt, d.Version as Version
    from separatedvalues.comma($deployments, true, 0) d
)
select e.Service as Service, e.ErrorTime as ErrorTime, d.Version as Version,
    e.Message as Message
from errors e
asof join deployments d
    on e.Service = d.Service and e.ErrorTime >= d.DeployedAt
order by e.Service, e.ErrorTime
