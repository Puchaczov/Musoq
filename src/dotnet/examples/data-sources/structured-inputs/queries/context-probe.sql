select c.QueryId, c.SourceContextId, c.Alias
from #inputs.contextprobe() c
