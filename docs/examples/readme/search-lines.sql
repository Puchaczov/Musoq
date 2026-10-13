param(root: string = './docs/examples/readme/fixtures')

select s.Path as Path, s.LineNumber as Line, s.LineText as Text
from search.lines($root, 'ERROR',
    options: (scope: (include: array { '*.log' }))) s
order by s.Path, s.LineNumber
