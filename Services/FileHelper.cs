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
            // Change file extension from .csv to .txt
            outputFile = Path.ChangeExtension(outputFile, ".txt");
            
            // Create Results directory if it doesn't exist
            Directory.CreateDirectory(ResultsFolder);
            string fullPath = Path.Combine(ResultsFolder, outputFile);

            // Format each result as a text line
            var lines = results.Select(r => 
                $"{r.Description} ({r.Year}): {r.Population:N3}");

            // Write the results to the file
            File.WriteAllLines(fullPath, lines);
        }
    }
}