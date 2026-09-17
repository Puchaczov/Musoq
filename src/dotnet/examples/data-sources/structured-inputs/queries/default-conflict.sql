param(input: (Id: string, Pattern: string, Mode: string = 'regex') = (Id: 'todo', Pattern: 'TODO'))

select d.Id, d.Mode
from #inputs.defaultconflict(input: $input) d
