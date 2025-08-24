---
title: Time Utilization
layout: default
parent: Practical Examples and Applications
nav_order: 4
---

# Calculating Startup Time

Sometimes it's necessary to perform some action periodically. To specify when our script should run, we can use a `CRON` expression or alternatively express it more descriptively in SQL language. Let's assume that we need to run a task at midnight on the first Wednesday of every month. The pattern corresponding to this requirement is `0 0 1-7 * 3`. To achieve the same pattern using `Musoq`, the query looks like this:

```sql
SELECT 
    DateTime,
    DayOfWeek
FROM @time.interval('01/01/2024', '01/01/2025', 'days')
WHERE 
    Day >= 1 AND 
    Day <= 7 AND 
    DayOfWeek = 3
```