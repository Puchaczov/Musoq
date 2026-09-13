param(patterns: (Id: string, Pattern: string, Mode: string = 'literal')[])

select m.PatternId, m.MatchText
from #inputs.match('TODO FIXME', patterns: $patterns) m
