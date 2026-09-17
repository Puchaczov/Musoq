let patterns: (
    Id: string,
    Pattern: string,
    Mode: string = 'literal'
)[] = array {
    (Id: 'todo', Pattern: 'TODO'),
    (Id: 'issue', Pattern: 'ISSUE-[0-9]+', Mode: 'regex'),
};

select m.PatternId, m.MatchText
from #inputs.match('TODO ISSUE-42', patterns: $patterns) m