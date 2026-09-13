select m.PatternId
from #inputs.match('TODO', patterns: array { ('todo', 'TODO') }) m
