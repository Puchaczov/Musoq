param(root: string = './docs/examples/readme/fixtures')

with topics as (
    select m.Path as Path, m.PatternId as Level
    from search.many($root, patterns: array {
        (id: 'ERROR', pattern: 'ERROR'),
        (id: 'WARN', pattern: 'WARN')
    }, options: (scope: (include: array { '*.log' }))) m
    group by m.Path, m.PatternId
)
select t.Path as Path, Count(*) as Levels
from topics t
group by t.Path
having Count(*) = 2
order by Path
