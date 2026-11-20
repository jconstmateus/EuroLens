// This program runs daily and downloads the ECB history file via an HTTP request
using System.IO.Compression;
using System.Globalization;

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