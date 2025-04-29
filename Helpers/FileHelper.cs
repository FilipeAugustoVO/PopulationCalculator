using System;
using System.Collections.Generic;
using System.IO;

namespace PopulationCalculator.Helpers;

public static class FileHelper
{
    public static void WriteResults(List<string> results, string outputPath = "population_results.txt")
    {
        try
        {
            File.WriteAllLines(outputPath, results);
            Console.WriteLine($"Results written to {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to file: {ex.Message}");
        }
    }
}