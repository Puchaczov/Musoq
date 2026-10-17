param(root: string = './docs/examples/readme/fixtures')

select c.Text as Service, Count(*) as Records
from search.matches($root,
    'service=(?<service>[a-z]+)', options: (
        text: (mode: 'regex'),
        records: (mode: 'physical-line', maxBytes: 1048576),
        scope: (include: array { '*.log' })
    )) m
cross apply m.Captures c
where c.GroupName = 'service' and c.Success = true
group by c.Text
order by Service
