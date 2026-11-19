<p align="center">
  <picture>
    <img src="docs/logo_dark_no_back.png" alt="EuroLens logo" width="300">
  </picture>
</p>

# EuroLens

Collects the full history of euro exchange rates from the European Central Bank,
stores it in PostgreSQL and analyses it with SQL.

> 🚧 Work in progress

## Data source

Euro foreign exchange reference rates published by the European Central Bank (ECB),
available since 1999.

These are **reference rates**, published for information purposes only.
They are not transaction rates.

## Current status

- [x] Download the ECB history file
- [x] Read and clean the CSV
- [ ] Save rates to PostgreSQL
- [ ] Daily incremental import
- [ ] SQL analysis
- [ ] REST API (ASP.NET Core)
- [ ] Charts
- [ ] Unit tests (xUnit)

## How to run

```
dotnet run --project EuroLens.Importer
```
