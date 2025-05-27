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
            
            // Write the results to the file in the Results folder
            File.WriteAllLines(fullPath, results.Select(r => r.ToString())!);
        }
    }
}