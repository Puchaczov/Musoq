select s.Team as Team, Sum(s.Amount) as Total
from values {
    (Team: 'api', Amount: 10),
    (Team: 'api', Amount: 15),
    (Team: 'worker', Amount: 5)
} s
group by s.Team
having Sum(s.Amount) >= 20
order by Team
