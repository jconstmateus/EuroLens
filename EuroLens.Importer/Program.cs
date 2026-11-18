// This program runs daily and downloads the ECB history file via an HTTP request

using var client = new HttpClient();

string url = "https://www.ecb.europa.eu/stats/euroxref/eurofxref-hist.zip";

try {
    byte[] data = await client.GetByteArrayAsync(url); // Download the file as raw bytes
    Console.WriteLine($"Downloaded {data.Length} bytes at {DateTime.Now}");
} catch (HttpRequestException ex) {
    Console.WriteLine($"ERROR: The program couldn't get the updated file - {ex.Message}");
}