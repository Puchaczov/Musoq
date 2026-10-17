binary Header {
    Kind: byte,
    Size: short le,
    Payload: byte[Size]
};

with input as (
    select s.Id as Id, FromHexToBytes(s.Hex) as Data
    from values {
        (Id: 1, Hex: '010300AABBCC'),
        (Id: 2, Hex: '010300AA')
    } s
    cross join system.dual() seed
)
select i.Id as Id, h.Kind as Kind, h.Size as Size
from input i
outer apply TryInterpret<Header>(i.Data) h
order by i.Id
