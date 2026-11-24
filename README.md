<p align="center">
  <picture>
    <img src="docs/logo_dark_noback.png" alt="EuroLens logo" width="300">
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

Each rate means: **1 euro = X units of the foreign currency**.
When the value goes up, the euro has strengthened against that currency.

## How it works

1. Downloads the ECB history file (ZIP with a CSV) over HTTP.
2. Parses the CSV with the invariant culture, so results don't depend on the computer's language settings.
3. Skips missing values (`N/A`), including currencies that no longer exist (e.g. currencies replaced by the euro).
4. Converts the data from wide format (one column per currency) to long format (one row per date and currency).
5. Loads the rates into PostgreSQL:
   - **Incremental:** only dates newer than the latest one in the database are inserted.
   - **Idempotent:** `ON CONFLICT DO NOTHING` on the primary key (date + currency) means re-running never creates duplicates.
   - **Fast and safe:** parameterized, prepared statements inside a single transaction.

## Current status

- [x] Download the ECB history file
- [x] Read and clean the CSV
- [x] Save rates to PostgreSQL
- [x] Incremental, idempotent load
- [ ] Daily scheduled run
- [ ] SQL analysis
- [ ] REST API (ASP.NET Core)
- [ ] Charts
- [ ] Unit tests (xUnit)

## How to run

Requirements: [.NET SDK](https://dotnet.microsoft.com/download) and [Docker](https://www.docker.com/).

**1. Start PostgreSQL**

```
docker run --name eurolens-db -e POSTGRES_PASSWORD=your-password -e POSTGRES_DB=eurolens -p 5433:5432 -v eurolens-data:/var/lib/postgresql/data -d postgres:17
```

**2. Create the table** (using `db/schema.sql`)

```
docker exec -i eurolens-db psql -U postgres -d eurolens < db/schema.sql
```

**3. Set the connection string**

```
setx EUROLENS_DB "Host=localhost;Port=5433;Database=eurolens;Username=postgres;Password=your-password"
```

Open a new terminal after this, so the variable is available.

**4. Run the importer**

```
dotnet run --project EuroLens.Importer
```

The first run loads the full history (220k+ rates). Later runs only add new dates.
