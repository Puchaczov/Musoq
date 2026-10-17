param(root: string = './src/dotnet/Musoq.Parser')

select p.Path as Path
from search.paths($root, options: (scope: (
    include: array { '*.cs', '**/*.cs' },
    exclude: array { '**/obj/**', '**/bin/**' }
))) p
order by p.Path
take 5
