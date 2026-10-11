table Seed {
    Dummy: string
};

couple system.dual with table Seed as Seeds;

select s.Dummy as Dummy
from Seeds() s
