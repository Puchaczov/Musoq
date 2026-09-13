select m.Value, m.Row, m.Column
from #inputs.matrix(
    values: array {
        array { 1, 2 },
        array { 3 },
        array {},
    }
) m
