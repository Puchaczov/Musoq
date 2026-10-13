binary LoginPayload {
    UserId: int le
};

binary Packet {
    Type: byte,
    Length: byte,
    Payload: switch Type {
        1 => Login: LoginPayload,
        _ => Raw: byte[Length]
    }
};

with input as (
    select 1 as Id, FromHexToBytes('01042A000000') as Data
    from system.dual()
    union all
    select 2 as Id, FromHexToBytes('0903AABBCC') as Data
    from system.dual()
)
select
    p.Payload.Case as PayloadKind,
    case when p.Payload.Case = 'Login'
        then p.Payload.Login.UserId
        else null
    end as UserId
from input i
cross apply Interpret<Packet>(i.Data) p
order by PayloadKind
