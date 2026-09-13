select o.Kind, o.Value, o.Count
from #inputs.overloaded(
    patterns: array {
        (Id: 'todo', Pattern: 'TODO'),
        (Id: 'fixme', Pattern: 'FIXME'),
    }
) o
