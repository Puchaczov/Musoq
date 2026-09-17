select m.PatternId
from #inputs.match('TODO', patterns: { (Id: 'todo', Pattern: 'TODO') }) m
