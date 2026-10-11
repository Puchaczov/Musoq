with recursive edges as (
    select e.Service as Service, e.Dependency as Dependency
    from values {
        (Service: 'checkout', Dependency: 'payments'),
        (Service: 'payments', Dependency: 'ledger'),
        (Service: 'ledger', Dependency: 'checkout')
    } e
), dependencies (Service, Depth) as (
    select 'checkout', 0 from values { (Seed: 1) } seed
    union (Service)
    select e.Dependency, d.Depth + 1
    from dependencies d
    inner join edges e on d.Service = e.Service
)
select Service, Depth
from dependencies
order by Depth
