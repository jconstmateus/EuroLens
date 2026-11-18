// This program runs daily and downloads the ECB history file via an HTTP request
using System.IO.Compression;

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
var csvEntry = zip.Entries[0];                        // Open first and only entry
using var reader = new StreamReader(csvEntry.Open()); // Read the CSV as text

// Show the first 3 lines
for (int i = 0; i < 3; i++)
{
    var line = reader.ReadLine(); 
    Console.WriteLine(line);
}