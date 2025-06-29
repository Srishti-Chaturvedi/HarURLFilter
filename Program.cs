using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        Console.Write("Enter the full path of the CSV file containing all URLs (no header): ");
        string allUrlsCsvPath = Console.ReadLine();

        if (!File.Exists(allUrlsCsvPath))
        {
            Console.WriteLine("CSV file does not exist.");
            return;
        }

        string apiUrlsCsvPath = "./b_apiFilteredUrls.csv";
        string finalFilteredCsvPath = "./c_uniqueApiFilteredUrls.csv";
        string sortedCsvPath = "./d_sortedUrls.csv";

        try
        {
            // Read URLs from the CSV file (no header, all lines are URLs)
            List<string> allUrls = File.ReadAllLines(allUrlsCsvPath)
                                       .Select(line => line.Trim('"')) // Remove surrounding quotes if any
                                       .Where(line => !string.IsNullOrWhiteSpace(line))
                                       .ToList();

            // Filter URLs containing "/api"
            List<string> apiUrls = allUrls.FindAll(url => url.Contains("/api", StringComparison.OrdinalIgnoreCase));
            WriteUrlsToCsv(apiUrlsCsvPath, apiUrls);

            // Patterns to exclude
            string[] exclusionPatterns = new string[]
            {
                "/hub",
                "/Translation",
                ".well-known/openid-configuration",
                "/connect/token",
                "/errors/unknown-error"
            };

            // Filter out URLs containing any exclusion patterns
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
                    finalFilteredApiPaths.Add(url); // fallback, if no "/api" found
                }
            }

            // Remove duplicates
            var uniqueFinalFilteredApiPaths = new HashSet<string>(finalFilteredApiPaths);

            // Write final filtered unique URLs
            WriteUrlsToCsv(finalFilteredCsvPath, new List<string>(uniqueFinalFilteredApiPaths));

            // Write sorted URLs
            var sortedUrls = uniqueFinalFilteredApiPaths.OrderBy(url => url).ToList();
            WriteUrlsToCsv(sortedCsvPath, sortedUrls);

            Console.WriteLine($"All URLs read from: {allUrlsCsvPath} ({allUrls.Count} URLs)");
            Console.WriteLine($"/api URLs saved to: {apiUrlsCsvPath} ({apiUrls.Count} URLs)");
            Console.WriteLine($"Filtered unique /api URLs saved to: {finalFilteredCsvPath} ({uniqueFinalFilteredApiPaths.Count} unique URLs)");
            Console.WriteLine($"Sorted unique /api URLs saved to: {sortedCsvPath} ({sortedUrls.Count} sorted URLs)");
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
