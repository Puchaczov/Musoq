params(
    patterns: (
        Id: string,
        Pattern: string,
        Mode: string = 'literal'
    )[] = array {
        (Id: 'todo', Pattern: 'TODO'),
    },
    emptyNumbers: int[] = array {},
    nullableNumbers: int?[] = array { 1, null }
)

select m.PatternId, m.MatchText
from #inputs.match('TODO', patterns: $patterns) m
