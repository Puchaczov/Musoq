select
    s.Component as Component,
    Count(*) as Total,
    Count(*) filter (where s.Level = 'ERROR') as Errors
from values {
    (Component: 'api', Level: 'INFO'),
    (Component: 'api', Level: 'ERROR'),
    (Component: 'worker', Level: 'INFO')
} s
group by s.Component
order by Component
