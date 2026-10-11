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

with packets as (
    select FromHexToBytes('0903AABBCC') as Content
    from system.dual()
)
select
    p.Type as Type,
    p.Payload.Case as PayloadKind,
    Length(p.Payload.Raw) as PayloadBytes
from packets f
cross apply Interpret<Packet>(f.Content) p
