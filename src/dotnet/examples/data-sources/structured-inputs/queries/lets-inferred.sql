let todo = (Id: 'todo', Pattern: 'TODO');
let fixme = (Id: 'fixme', Pattern: 'FIXME');
let patterns = array { $todo, $fixme };

select m.PatternId, m.MatchText
from #inputs.match('TODO FIXME', patterns: $patterns) m
