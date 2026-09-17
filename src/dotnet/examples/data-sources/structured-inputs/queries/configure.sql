select c.Enabled, c.Before, c.After
from #inputs.configure(
    options: (
        Enabled: true,
        Codes: array { 10, 20 },
        Window: (Before: 2, After: 3),
    )
) c
