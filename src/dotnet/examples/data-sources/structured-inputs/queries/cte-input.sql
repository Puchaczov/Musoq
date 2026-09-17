with patterns as (
    select p.Id, p.Pattern
    from values {
        (Id: 'todo', Pattern: 'TODO'),
        (Id: 'fixme', Pattern: 'FIXME'),
    } p
)
select m.PatternId, m.MatchText
from #inputs.match('TODO FIXME', patterns: patterns) m