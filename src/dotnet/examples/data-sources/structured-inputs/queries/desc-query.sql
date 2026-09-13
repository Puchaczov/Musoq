desc query (
    with patterns as (
        select p.Id, p.Pattern
        from values {
            (Id: 'todo', Pattern: 'TODO'),
        } p
    )
    select m.PatternId, m.MatchText
    from #inputs.match('TODO', patterns: patterns) m
)
