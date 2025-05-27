using PopulationCalculator.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PopulationCalculator.Services
{
    public static class FileHelper
    {
        private const string ResultsFolder = "Results";

        public static void WriteResults(List<PopulationResult> results, string outputFile)
        {
            // Create Results directory if it doesn't exist
            Directory.CreateDirectory(ResultsFolder);

            // Combine the Results folder path with the output filename
            string fullPath = Path.Combine(ResultsFolder, outputFile);

            // Create CSV header and data
            var lines = new List<string>
            {
                "State,Year,Population,Description" // CSV header
            };

            // Add each result as a CSV line
            lines.AddRange(results.Select(r => 
                $"{r.StateName},{r.Year},{r.Population:N3},{r.Description}"));

            // Write the results to the file in the Results folder
            File.WriteAllLines(fullPath, lines);
        }
    }
}