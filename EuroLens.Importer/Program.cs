// This program runs daily and downloads the ECB history file via an HTTP request
using System.IO.Compression;
using System.Globalization;
using Npgsql;
using NpgsqlTypes;

using var client = new HttpClient();

string url = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-hist.zip";
byte[] data;

try {
    data = await client.GetByteArrayAsync(url);     // Download the file as raw bytes
    Console.WriteLine($"Downloaded {data.Length} bytes at {DateTime.Now}");

} catch (HttpRequestException ex) {
    Console.WriteLine($"ERROR: The program couldn't get the updated file - {ex.Message}");
    return;
}

// Open the ZIP file and read the CSV inside it
using var stream = new MemoryStream(data);            // Wrap the bytes in a stream 
using var zip = new ZipArchive(stream);               // Open the stream as a ZIP archive
var csvEntry = zip.Entries[0];                        // Open the first and only entry
using var reader = new StreamReader(csvEntry.Open()); // Read the CSV as text

var header = await reader.ReadLineAsync();            // Read only the header to extract info
if (header == null) {
    Console.WriteLine("ERROR: The CSV file is empty.");
    return;
}

string[] currencies = header.Split(",");                // Get each currency + date
Console.WriteLine($"There are {currencies.Length - 2} currencies available");
                                                        // Minus the date and the trailing comma

var rates = new List<ExchangeRate>();                   // Create a new list of Exchange Rates records

string?line;
while ((line = await reader.ReadLineAsync())!= null) {  // Read async until null
    string[] lineInfo = line.Split(",");

    DateOnly date = DateOnly.Parse(lineInfo[0], CultureInfo.InvariantCulture);      // Parse the date using the invariant culture (ECB format: yyyy-MM-dd)

    for (int i = 1; i < currencies.Length; i++) {

        if (lineInfo[i] == "N/A" || lineInfo[i] == "") continue;                    // Ignore N/A and empty space

        decimal value = decimal.Parse(lineInfo[i], CultureInfo.InvariantCulture);   // Parse with the invariant culture (ECB file uses a dot as decimal separator)
        rates.Add(new ExchangeRate(date, currencies[i], value));                    // Currency stay as a string
    }
}

Console.WriteLine($"Read {rates.Count} rates");

// Read the connection string from an environment variable (keeps the password out of the code)
string? connectionString = Environment.GetEnvironmentVariable("EUROLENS_DB");
if (connectionString == null)
{
    Console.WriteLine("ERROR: Environment variable EUROLENS_DB is not set.");
    return;
}

// Open connection to PostgreSQL
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

// --------------- Query 1: get the last date already in the database
await using var lastDateCommand = new NpgsqlCommand(
    "SELECT MAX(rate_date) FROM exchange_rates",
    connection);

var result = await lastDateCommand.ExecuteScalarAsync();   // Single value: the latest date, or NULL if the table is empty

// The latest date as DateOnly, or null if the table is empty (first run)
DateOnly? lastDate = result as DateOnly?;

// --------------- Query 2: insert only the rates newer than lastDate
// Single transaction: nothing is saved until the commit, and a failure saves nothing
await using var transaction = await connection.BeginTransactionAsync();

await using var loadLastRatesCommand = new NpgsqlCommand(
    "INSERT INTO exchange_rates (rate_date, currency, rate) VALUES (@date, @currency, @rate) " +
    "ON CONFLICT (rate_date, currency) DO NOTHING",   // Safety net: duplicates are ignored, never inserted twice
    connection, transaction);

// Create the parameters once; only their values change for each rate
var dateParam = loadLastRatesCommand.Parameters.Add("date", NpgsqlDbType.Date);
var currencyParam = loadLastRatesCommand.Parameters.Add("currency", NpgsqlDbType.Char);
var rateParam = loadLastRatesCommand.Parameters.Add("rate", NpgsqlDbType.Numeric);

await loadLastRatesCommand.PrepareAsync();   // PostgreSQL parses the SQL once and reuses it for every rate

int inserted = 0;

foreach (var rate in rates)
{
    // First run (no lastDate): insert everything. Otherwise: only dates newer than lastDate
    if (lastDate == null || rate.Date > lastDate)
    {
        dateParam.Value = rate.Date;
        currencyParam.Value = rate.Currency;
        rateParam.Value = rate.Rate;
        inserted += await loadLastRatesCommand.ExecuteNonQueryAsync();   // 1 if inserted, 0 if it already existed
    }
}

await transaction.CommitAsync();   // Save all inserts at once

// --------------- Query 3: count the total rows in the database
await using var totalRowsCommand = new NpgsqlCommand(
    "SELECT COUNT(*) FROM exchange_rates",
    connection);

var rows = Convert.ToInt64(await totalRowsCommand.ExecuteScalarAsync());   // Single value: the number of rows

Console.WriteLine($"Last date in database: {lastDate?.ToString() ?? "first run: full load"}");
Console.WriteLine($"Rates inserted in database: {inserted}");
Console.WriteLine($"Total rows in database: {rows}");