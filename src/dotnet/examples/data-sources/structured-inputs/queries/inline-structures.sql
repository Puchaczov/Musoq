select m.PatternId, m.MatchText
from #inputs.match(
    'TODO FIXME ISSUE-42',
    patterns: array {
        (Id: 'todo', Pattern: 'TODO'),
        (Pattern: 'FIXME', Id: 'fixme'),
        (Id: 'issue', Pattern: 'ISSUE-[0-9]+', Mode: 'regex'),
    }
) m