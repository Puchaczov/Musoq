with errors as (
    select e.Service as Service, e.At::datetime as ErrorTime
    from values {
        (Service: 'checkout', At: '2026-10-11T10:05:00'),
        (Service: 'checkout', At: '2026-10-11T10:20:00')
    } e
), deployments as (
    select
        d.Service as Service,
        d.At::datetime as DeployedAt,
        d.Version as Version
    from values {
        (Service: 'checkout', At: '2026-10-11T10:00:00', Version: 'v1'),
        (Service: 'checkout', At: '2026-10-11T10:15:00', Version: 'v2')
    } d
)
select e.ErrorTime as ErrorTime, d.Version as Version
from errors e
asof join deployments d
    on e.Service = d.Service and e.ErrorTime >= d.DeployedAt
order by e.ErrorTime
