using PopulationCalculator.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PopulationCalculator.Services
{
    public static class FileHelper
    {
        public static void WriteResults(List<PopulationResult> results, string outputFile)
        {
            File.WriteAllLines(outputFile, results.Select(r => r.ToString())!);
        }
    }
}