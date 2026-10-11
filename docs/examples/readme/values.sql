select s.Name as Name, s.Price * s.Quantity as Total
from values {
    (Name: 'A', Price: 12, Quantity: 2),
    (Name: 'B', Price: 8, Quantity: 1)
} s
where s.Price >= 10
order by Name
