using System.Text.Json;

class Program
{
    static void Main()
    {
        Console.Write("Enter the full path of the .har file: ");
        string harPath = Console.ReadLine();

        if (!File.Exists(harPath))
        {
            Console.WriteLine("HAR file does not exist.");
            return;
        }

        string allUrlsCsvPath = "./a_allUrls.csv";
        string apiUrlsCsvPath = "./b_apiFilteredUrls.csv";
        string finalFilteredCsvPath = "./c_uniqueApiFilteredUrls.csv";

        try
        {
            string jsonString = File.ReadAllText(harPath);

            using JsonDocument doc = JsonDocument.Parse(jsonString);

            if (!doc.RootElement.TryGetProperty("log", out JsonElement logElement) ||
                !logElement.TryGetProperty("entries", out JsonElement entriesElement))
            {
                Console.WriteLine("Invalid HAR file structure.");
                return;
            }

            List<string> allUrls = new List<string>();

            foreach (JsonElement entry in entriesElement.EnumerateArray())
            {
                if (entry.TryGetProperty("request", out JsonElement requestElement) &&
                    requestElement.TryGetProperty("url", out JsonElement urlElement))
                {
                    allUrls.Add(urlElement.GetString());
                }
            }

            // writes all the URLs to CSV
            WriteUrlsToCsv(allUrlsCsvPath, allUrls);

            // filters all URLs containing "/api"
            List<string> apiUrls = allUrls.FindAll(url => url.Contains("/api", StringComparison.OrdinalIgnoreCase));
            WriteUrlsToCsv(apiUrlsCsvPath, apiUrls);

            // patterns to exclude
            string[] exclusionPatterns = new string[]
            {
                "/hub",
                "/Translation",
                ".well-known/openid-configuration",
                "/connect/token",
                "/errors/unknown-error"
            };

            // Gives list after excluding the exclusionPatterns above
            List<string> finalFilteredUrls = apiUrls.FindAll(url =>
                !ContainsAny(url, exclusionPatterns));

            // Extract substring starting from "/api" for final filtered URLs
            List<string> finalFilteredApiPaths = new List<string>();
            foreach (var url in finalFilteredUrls)
            {
                int index = url.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    finalFilteredApiPaths.Add(url.Substring(index));
                }
                else
                {
                    finalFilteredApiPaths.Add(url); //in case it didnt find /api, it adds complete url
                }
            }

            // Removing duplicate urls
            var uniqueFinalFilteredApiPaths = new HashSet<string>(finalFilteredApiPaths);

            // Writing final urls
            WriteUrlsToCsv(finalFilteredCsvPath, new List<string>(uniqueFinalFilteredApiPaths));

            Console.WriteLine($"All URLs saved to: {allUrlsCsvPath} ({allUrls.Count} URLs)");
            Console.WriteLine($"/api URLs saved to: {apiUrlsCsvPath} ({apiUrls.Count} URLs)");
            Console.WriteLine($"Filtered unique /api URLs saved to: {finalFilteredCsvPath} ({uniqueFinalFilteredApiPaths.Count} unique URLs)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static bool ContainsAny(string source, string[] patterns)
    {
        foreach (var p in patterns)
        {
            if (source.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    static void WriteUrlsToCsv(string path, List<string> urls)
    {
        using StreamWriter writer = new StreamWriter(path);
        writer.WriteLine("Request URLs");

        foreach (string url in urls)
        {
            string escapedUrl = "\"" + url.Replace("\"", "\"\"") + "\"";
            writer.WriteLine(escapedUrl);
        }
    }
}
