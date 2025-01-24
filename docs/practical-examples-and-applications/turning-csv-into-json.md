---
title: Turning CSV into JSON
layout: default
parent: Practical Examples and Applications
nav_order: 8
---

# Converting CSV to JSON with Musoq - Quick Guide

This guide shows you how to convert CSV files to JSON using Musoq, with special attention to creating structured JSON objects.

## Basic Table View

To view your CSV data in table format, use this command:

```powershell
./Musoq.exe run query "select * from #separatedvalues.comma('cities.csv', true, 0)"
```

You'll see your data in a clear table format:

```cli
┌────────┬──────────┬────────────────┬──────────┬─────────────┬─────────┬───────────┬───────────────────┐
│ cityId │ cityName │ cityPopulation │ cityArea │ postOffices │ schools │ isCapitol │ isVoivodeshipCity │
├────────┼──────────┼────────────────┼──────────┼─────────────┼─────────┼───────────┼───────────────────┤
│ 1      │ Warsaw   │ 1793579        │ 517.24   │ 218         │ 456     │ true      │ true              │
│ 2      │ Krakow   │ 779115         │ 326.85   │ 156         │ 324     │ false     │ true              │
│ 3      │ Lodz     │ 679941         │ 293.25   │ 98          │ 278     │ false     │ true              │
│ 4      │ Zakopane │ 27000          │ 84.23    │ 12          │ 15      │ false     │ false             │
│ 5      │ Gdansk   │ 470907         │ 262.58   │ 87          │ 198     │ false     │ true              │
└────────┴──────────┴────────────────┴──────────┴─────────────┴─────────┴───────────┴───────────────────┘
```

## Simple JSON Output

To convert the same data to flat JSON, add the --format json flag:

```powershell
./Musoq.exe run query "select * from #separatedvalues.comma('cities.csv', true, 0)" --format json
```

This produces:

```json
[{"cityId":"1","cityName":"Warsaw","cityPopulation":"1793579","cityArea":"517.24","postOffices":"218","schools":"456","isCapitol":"true","isVoivodeshipCity":"true"},{"cityId":"2","cityName":"Krakow","cityPopulation":"779115","cityArea":"326.85","postOffices":"156","schools":"324","isCapitol":"false","isVoivodeshipCity":"true"},{"cityId":"3","cityName":"Lodz","cityPopulation":"679941","cityArea":"293.25","postOffices":"98","schools":"278","isCapitol":"false","isVoivodeshipCity":"true"},{"cityId":"4","cityName":"Zakopane","cityPopulation":"27000","cityArea":"84.23","postOffices":"12","schools":"15","isCapitol":"false","isVoivodeshipCity":"false"},{"cityId":"5","cityName":"Gdansk","cityPopulation":"470907","cityArea":"262.58","postOffices":"87","schools":"198","isCapitol":"false","isVoivodeshipCity":"true"}]
```

## Nested JSON Output

The interpreted_json format allows you to treat column headers as a hierarchy of a JSON object and thus, interpret it to create complex objects. Here's how to use it:

```powershell
./Musoq.exe run query "select cityId as [city.id], cityName as [city.name], cityPopulation as [city.features.population], cityArea as [city.features.area], postOffices as [city.features.postOffices], schools as [city.features.schools], isCapitol as [city.features.isCapitol], isVoivodeshipCity as [city.features.isVoivodeship] from #separatedvalues.comma('cities.csv', true, 0)" --format interpreted_json
```

This creates a structured JSON output:

```json
[{"city":{"id":1,"name":"Warsaw","features":{"population":1793579,"area":517.24,"postOffices":218,"schools":456,"isCapitol":true,"isVoivodeship":true}}},{"city":{"id":2,"name":"Krakow","features":{"population":779115,"area":326.85,"postOffices":156,"schools":324,"isCapitol":false,"isVoivodeship":true}}},{"city":{"id":3,"name":"Lodz","features":{"population":679941,"area":293.25,"postOffices":98,"schools":278,"isCapitol":false,"isVoivodeship":true}}},{"city":{"id":4,"name":"Zakopane","features":{"population":27000,"area":84.23,"postOffices":12,"schools":15,"isCapitol":false,"isVoivodeship":false}}},{"city":{"id":5,"name":"Gdansk","features":{"population":470907,"area":262.58,"postOffices":87,"schools":198,"isCapitol":false,"isVoivodeship":true}}}]
```