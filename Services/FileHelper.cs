using PopulationCalculator.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;

namespace PopulationCalculator.Services
{
    public static class FileHelper
    {
        private const string ResultsFolder = "Results";

        public static void WriteResults(List<PopulationResult> results, string outputFile)
        {
            // Sanitize file name by removing invalid characters
            outputFile = SanitizeFileName(outputFile);
            outputFile = Path.ChangeExtension(outputFile, ".txt");
            
            // Create Results directory if it doesn't exist
            Directory.CreateDirectory(ResultsFolder);
            string fullPath = Path.Combine(ResultsFolder, outputFile);

            var sb = new StringBuilder();
            foreach (var result in results)
            {
                // Formula header and validation
                sb.AppendLine($"Formula: {result.Formula}");
                sb.AppendLine($"Pre-war population: {result.Population:N0}");
                sb.AppendLine("Validation Status:");
                sb.AppendLine($"  {result.ValidationStatus}");

                if (result.IsValid)
                {
                    // Pre-war and initial calculations
                    sb.AppendLine($"Final pre-war population: {result.Population:N0}");
                    sb.AppendLine($"KEOFF death rate: {result.KeoffRate:P3}");
                    sb.AppendLine($"Surviving population: {result.SurvivingPopulation:N4}");
                    sb.AppendLine($"FYOC loss rate: {result.FyocLossRate:P1}");
                    sb.AppendLine($"Post-FYOC population: {result.PostFyocPopulation:N6}");
                    sb.AppendLine();

                    // Post-War Population Projections
                    sb.AppendLine("Post-War Population Projections:");
                    
                    // Order projections in the desired sequence
                    var orderedProjections = result.PostWarProjections
                        .OrderBy(p => 
                        {
                            // Extract the rate value from the formula name
                            var formulaText = p.Formula.Replace(" formula", "").Replace("%", "");
                            var rate = double.Parse(formulaText, CultureInfo.InvariantCulture);
                            
                            // Put negative rates at the end in descending order
                            if (p.Formula.StartsWith("-"))
                                return -rate + 1000; // Changed from rate + 1000
                                
                            // Special case for 1% to come before 1.10%
                            if (Math.Abs(rate - 1.0) < 0.001)
                                return 1.09;
                                
                            return rate;
                        });

                    foreach (var proj in orderedProjections)
                    {
                        sb.AppendLine($"    {proj.Formula,-14}: {proj.Population:N6}");
                        sb.AppendLine($"        - Base MP       : {proj.BaseMp:N6}");
                        sb.AppendLine($"        - On-map MP     : {proj.OnMapMp}");
                        sb.AppendLine($"        - Off-map MP    : {proj.OffMapMp:N6}");
                        sb.AppendLine($"        - Daily Off-map : {proj.DailyOffMap:N8}");
                    }

                    // MP Statistics
                    sb.AppendLine();
                    sb.AppendLine("Base MP Statistics for this Pre-War Formula:");
                    sb.AppendLine($"    Average: {result.MpStatsAverage:N6}");
                    sb.AppendLine($"    Median : {result.MpStatsMedian:N6}");

                    // Ghoul Stats
                    sb.AppendLine();
                    sb.AppendLine("Ghoul MP Statistics:");
                    sb.AppendLine($"    Initial Population: {result.GhoulStats.InitialPopulation:N6}");
                    sb.AppendLine($"    Final Population : {result.GhoulStats.FinalPopulation:N6}");
                    sb.AppendLine($"    Off-map MP       : {result.GhoulStats.OffMapMp:N6}");
                    sb.AppendLine($"    Daily Off-map    : {result.GhoulStats.DailyOffMap:N8}");
                }
                sb.AppendLine("\n");
            }

            // Write the results to the file
            File.WriteAllText(fullPath, sb.ToString());
        }

        public static void WriteResults(PopulationResult result, string fileName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Formula: {result.Formula}");
            sb.AppendLine($"Pre-war population: {result.Population:N0}");
            sb.AppendLine($"Validation Status:");
            sb.AppendLine($"  {result.ValidationStatus}");

            if (result.IsValid)
            {
                sb.AppendLine($"Final pre-war population: {result.Population:N0}");
                sb.AppendLine($"KEOFF death rate: {result.KeoffRate:P3}");
                sb.AppendLine($"Surviving population: {result.SurvivingPopulation:N4}");
                sb.AppendLine($"FYOC loss rate: {result.FyocLossRate:P1}");
                sb.AppendLine($"Post-FYOC population: {result.PostFyocPopulation:N6}");
                sb.AppendLine();

                // Post-War Population Projections
                sb.AppendLine("Post-War Population Projections:");
                foreach (var proj in result.PostWarProjections)
                {
                    sb.AppendLine($"    {proj.Formula,-14}: {proj.Population:N6}");
                    sb.AppendLine($"        - Base MP       : {proj.BaseMp:N6}");
                    sb.AppendLine($"        - On-map MP     : {proj.OnMapMp}");
                    sb.AppendLine($"        - Off-map MP    : {proj.OffMapMp:N6}");
                    sb.AppendLine($"        - Daily Off-map : {proj.DailyOffMap:N8}");
                }

                // MP Statistics
                sb.AppendLine();
                sb.AppendLine($"Base MP Statistics for this Pre-War Formula:");
                sb.AppendLine($"    Average: {result.MpStatsAverage:N6}");
                sb.AppendLine($"    Median : {result.MpStatsMedian:N6}");

                // Ghoul Stats
                sb.AppendLine();
                sb.AppendLine("Ghoul MP Statistics:");
                sb.AppendLine($"    Initial Population: {result.GhoulStats.InitialPopulation:N6}");
                sb.AppendLine($"    Final Population : {result.GhoulStats.FinalPopulation:N6}");
                sb.AppendLine($"    Off-map MP       : {result.GhoulStats.OffMapMp:N6}");
                sb.AppendLine($"    Daily Off-map    : {result.GhoulStats.DailyOffMap:N8}");
            }
            sb.AppendLine("\n");

            string path = Path.Combine("Results", fileName);
            File.WriteAllText(path, sb.ToString());
        }

        private static string SanitizeFileName(string fileName)
        {
            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = string.Join("_", 
                fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            
            // Replace spaces and other problematic characters
            sanitized = sanitized
                .Replace(" ", "_")
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(":", "_");

            return sanitized;
        }
    }
}