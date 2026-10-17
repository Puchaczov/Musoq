binary User {
    UserId: int le
};

binary Packet {
    Kind: byte,
    User: User
};

with input as (
    select FromHexToBytes('012A000000') as Data
    from system.dual()
)
select p.Kind as Kind, p.User.UserId as UserId
from input i
cross apply Interpret<Packet>(i.Data) p
