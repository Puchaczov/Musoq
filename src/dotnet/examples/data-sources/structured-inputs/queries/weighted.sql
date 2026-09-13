select w.Value, w.Weight, w.Enabled
from #inputs.weighted(
    items: array {
        (Value: 10, Weight: 1.5),
        (Weight: 2.5, Value: 20, Enabled: false),
    }
) w
