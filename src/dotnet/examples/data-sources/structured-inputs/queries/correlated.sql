param(suffix: string = '!')

select m.PatternId
from values {
    (Pattern: 'TODO'),
} p
cross apply #inputs.match(
    'TODO!',
    patterns: array {
        (Id: 'todo', Pattern: p.Pattern + $suffix),
    }
) m
