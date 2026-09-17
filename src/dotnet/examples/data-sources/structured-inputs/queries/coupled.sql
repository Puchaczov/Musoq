table MatchRows {
    PatternId: string
};

couple #inputs.match with table MatchRows as Matches;

select m.PatternId
from Matches(
    'TODO',
    patterns: array { (Id: 'todo', Pattern: 'TODO') }
) m
