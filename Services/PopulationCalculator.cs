using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using PopulationCalculator.Models;
using System.IO;

namespace PopulationCalculator.Services
{
    public class PopulationCalculator
    {
        private readonly double InitialPopulation1945;
        private readonly double ModernPopulation;
        private readonly double OtlMaxPopulation;

        private readonly NumberFormatInfo nfi = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

        private readonly string _debugLogPath;

        public PopulationCalculator(
            double initialPopulation1945,
            double modernPopulation,
            double otlMaxPopulation)
        {
            InitialPopulation1945 = initialPopulation1945;
            ModernPopulation = modernPopulation;
            OtlMaxPopulation = otlMaxPopulation;
            
            // Create Results directory if it doesn't exist
            string resultsDir = Path.Combine(Directory.GetCurrentDirectory(), "Results");
            Directory.CreateDirectory(resultsDir);
            _debugLogPath = Path.Combine(resultsDir, "calculation_debug.log");
            
            // Append to the log instead of clearing it
            File.AppendAllText(_debugLogPath, $"\n=== New calculation session started at {DateTime.Now} ===\n\n");
        }

        // Default growth periods
        private readonly List<(int years, double rateModifier)> DefaultPreWarPeriods = new()
        {
            (121, 1.0),  // Full growth for 121 years
            (10, 0.5)    // Half growth for last 10 years
        };

        private readonly List<(int years, double rateModifier, double popChange)> DefaultPostWarPeriods = new()
        {
            (173, 1.0, 0.0)  // 173 years of post-war growth, no percentage population change
        };

        private readonly List<(int years, double rateModifier, double popChange)> DefaultGhoulPeriods = new()
        {
            (178, 1.0, 0.0)  // 178 years of ghoul decline, no percentage or flat population change
        };

        /// <summary>
        /// Calculates population projections for a state using various growth formulas
        /// </summary>
        /// <param name="stateName">Name of the state</param>
        /// <param name="preWarPeriods">Optional custom pre-war growth periods</param>
        /// <param name="postWarPeriods">Optional custom post-war growth periods</param>
        /// <param name="ghoulPeriods">Optional custom ghoul population periods</param>
        /// <returns>List of population results for each valid growth formula</returns>
        public List<PopulationResult> CalculatePopulations(
            string stateName,
            List<(int years, double rateModifier)>? preWarPeriods = null,
            List<(int years, double rateModifier, double popChange)>? postWarPeriods = null,
            List<(int years, double rateModifier, double popChange)>? ghoulPeriods = null)
        {
            LogDebug($"\n=== Starting calculations for {stateName} ===\n");
            LogDebug($"Initial Population (1945): {InitialPopulation1945:N0}\n");
            
            var results = new List<PopulationResult>();
            
            foreach (var formula in PopulationFormulas.PreWarFormulas)
            {
                LogDebug($"\nTrying {formula.Key}:\n");
                
                var result = new PopulationResult 
                { 
                    StateName = stateName,
                    Formula = formula.Key
                };

                // Calculate pre-war population
                double preWarPop = CalculatePreWarPopulation(InitialPopulation1945, formula.Value, preWarPeriods ?? DefaultPreWarPeriods);
                result.Population = preWarPop;
                
                // Validate population
                var (validatedPop, validationStatus) = ValidatePreWarPopulation(preWarPop, stateName, new List<double>());
                result.ValidationStatus = validationStatus;
                result.IsValid = validatedPop > 0;

                if (result.IsValid)
                {
                    LogDebug($"Found valid population: {validatedPop:N0}\n");
                    
                    // KEOFF calculations
                    decimal preWarPopDecimal = ConvertToDecimal(validatedPop, "Pre-war population");
                    var keoffRate = CalculateKeoffRate(preWarPopDecimal);
                    decimal survivingPop = preWarPopDecimal * (1M - keoffRate);
                    
                    result.KeoffRate = (double)keoffRate;
                    result.SurvivingPopulation = (double)survivingPop;
                    
                    // FYOC calculations
                    double fyocLossRate = CalculateFyocLossRate((double)survivingPop);
                    result.FyocLossRate = fyocLossRate;
                    result.PostFyocPopulation = (double)survivingPop * (1 - fyocLossRate);
                    
                    // Calculate and store post-war projections with MP stats
                    var postWarResults = CalculateAllPostWarProjections(
                        result.PostFyocPopulation, 
                        formula.Value,
                        postWarPeriods ?? DefaultPostWarPeriods);
                        
                    result.PostWarProjections = postWarResults;
                    
                    // Calculate MP statistics
                    var baseMps = postWarResults.Select(p => p.BaseMp).ToList();
                    result.MpStatsAverage = baseMps.Average();
                    result.MpStatsMedian = CalculateMedian(baseMps);
                    
                    // Calculate ghoul stats
                    result.GhoulStats = CalculateGhoulStatistics(
                        result.PostFyocPopulation,
                        ghoulPeriods ?? DefaultGhoulPeriods);
                }
                
                results.Add(result);
            }
            
            return results;
        }

        private List<PostWarProjection> CalculateAllPostWarProjections(
            double postFyocPop,
            double preWarRate,
            List<(int years, double rateModifier, double popChange)> periods)
        {
            var projections = new List<PostWarProjection>();
            
            // Convert to list to maintain order and avoid multiple enumeration
            var orderedFormulas = PopulationFormulas.PostApocFormulas.ToList();
            
            foreach (var formula in orderedFormulas)
            {
                var population = CalculatePostWarPopulation(
                    postFyocPop,
                    formula.Value,
                    preWarRate,
                    periods);
                    
                if (!double.IsNaN(population))
                {
                    var projection = new PostWarProjection
                    {
                        Formula = formula.Key,
                        Population = population,
                        BaseMp = population / 10,
                    };
                    
                    // Fix MP calculations:
                    projection.BaseMp = projection.BaseMp / 1000;
                    projection.OnMapMp = (int)Math.Floor(projection.BaseMp);
                    projection.OffMapMp = projection.BaseMp - projection.OnMapMp;
                    projection.DailyOffMap = projection.OffMapMp / 100;
                    
                    projections.Add(projection);
                }
            }
            
            return projections;
        }

        private GhoulStats CalculateGhoulStatistics(
            double postFyocPop,
            List<(int years, double rateModifier, double popChange)> periods)
        {
            var stats = new GhoulStats
            {
                InitialPopulation = postFyocPop * 0.15 // 15% become ghouls
            };
            
            stats.FinalPopulation = CalculateGhoulPopulation(
                stats.InitialPopulation,
                -0.005, // -0.5% decline rate
                periods);
                
            stats.OffMapMp = Math.Floor(stats.FinalPopulation / 10);
            stats.DailyOffMap = stats.OffMapMp / 100;
            
            return stats;
        }

        private void LogDebug(string message)
        {
            File.AppendAllText(_debugLogPath, $"{message}\n");
        }

        private double CalculatePreWarPopulation(
            double initialPop, 
            double growthRate, 
            List<(int years, double rateModifier)> periods)
        {
            double population = initialPop;
            
            // Historical rates come in as decimals like 0.7072 meaning 70.72%
            // Pre-defined rates are already converted when defined (like 0.015 for 1.5%)
            bool isHistoricalRate = Math.Abs(growthRate) > 0.1; // If rate > 10%, it's a historical rate
            if (isHistoricalRate)
            {
                growthRate = growthRate / 100.0;
            }
            
            // Initial debug info
            LogDebug($"\n=== Pre-War Population Calculation for {population:N0} ===");
            LogDebug($"Initial: {population:N0}");
            LogDebug($"Growth Rate: {growthRate.ToString(nfi)}");
            
            foreach (var (years, modifier) in periods)
            {
                var adjustedRate = growthRate * modifier;
                
                LogDebug($"Years: {years}, Modifier: {modifier}, Adjusted Rate: {adjustedRate.ToString(nfi)}");
                LogDebug($"Before: {population:N0}");
                
                population *= Math.Pow(1 + adjustedRate, years);
                
                LogDebug($"After: {population:N0}\n");
            }
            
            return population;
        }

        private double CalculateFyocLossRate(double survivingPop)
        {
            // Base rate for populations under 10k
            if (survivingPop <= 10_000)
                return 0.50;  // Flat 50% for populations under 10k

            // For populations between 10k and 100k
            if (survivingPop <= 100_000)
            {
                // Start counting brackets AFTER the first 10k
                // First bracket (10k-20k) should be 0.525 (52.5%)
                // Second bracket (20k-30k) should be 0.55 (55%)
                // And so on...
                int brackets = (int)Math.Floor((survivingPop - 10_000) / 10_000);
                return 0.50 + ((brackets + 1) * 0.025); // Add 1 to brackets to start at 52.5%
            }

            // For populations between 100k and 350k (1% per 10k)
            if (survivingPop <= 350_000)
            {
                int brackets = (int)Math.Floor((survivingPop - 100_000) / 10_000);
                return 0.75 + (brackets * 0.01);
            }

            // For populations over 350k (0.1% per 10k)
            int extraBrackets = (int)Math.Floor((survivingPop - 350_000) / 10_000);
            return Math.Min(0.99 + (extraBrackets * 0.001), 0.999);
        }

        private double CalculatePostWarPopulation(
            double initialPop, 
            double growthRate, 
            double preWarRate,
            List<(int years, double rateModifier, double popChange)> periods)
        {
            double population = initialPop;
            double effectiveRate;
            
            // Handle negative modifiers (-12%, -25%, -50%, -75%)
            if (growthRate < 0)
            {
                // growthRate comes in as -0.12, -0.25, -0.50, -0.75 (already in decimal form)
                double reduction = Math.Abs(growthRate * 100.0); // Convert to percentage (12, 25, 50, 75)
                double keepPercent = (100.0 - reduction) / 100.0; // Convert to decimal (0.88, 0.75, 0.50, 0.25)
                effectiveRate = (preWarRate * keepPercent) / 100.0;
                
                LogDebug($"\nNegative modifier calculation:");
                LogDebug($"Pre-war rate: {preWarRate:F5}%");
                LogDebug($"Keep percent: {keepPercent:F4} (reducing by {reduction:F0}%)");
                LogDebug($"Final rate: {effectiveRate:F6}");
            }
            else
            {
                effectiveRate = growthRate;
            }
            
            LogDebug($"\nPost-war calculation:");
            LogDebug($"Initial population: {population:N0}");
            
            foreach (var (years, modifier, change) in periods)
            {
                var yearlyRate = effectiveRate * modifier;
                population *= Math.Pow(1 + yearlyRate, years);
                population += change;
                
                LogDebug($"After {years} years at {yearlyRate:P4}: {population:N0}");
            }
            
            return population;
        }

        private double CalculateGhoulPopulation(
            double initialPop,
            double declineRate,
            List<(int years, double rateModifier, double popChange)> periods)
        {
            double population = initialPop;

            foreach (var (years, modifier, change) in periods)
            {
                // Apply growth/decline rate
                var adjustedRate = declineRate * modifier;
                population *= Math.Pow(1 + adjustedRate, years);
                
                // If change is >= 1 or <= -1, treat as absolute number
                // Otherwise treat as percentage
                if (Math.Abs(change) >= 1)
                {
                    population += change;  // Add/subtract absolute number
                }
                else if (change != 0)
                {
                    population *= (1 + change);  // Apply percentage
                }
            }

            return population;
        }

        private decimal CalculateKeoffRate(decimal preWarPop)
        {
            // Convert population to millions for calculation
            decimal millions = preWarPop / 1_000_000M;
            
            // Base death rate is 99%
            decimal rate = 0.99M;
            
            // Add 0.01% (0.0001) for each million up to 90M
            if (millions <= 90M)
            {
                decimal millionBrackets = Math.Floor(millions);
                rate += millionBrackets * 0.0001M;
            }
            else
            {
                // First add for first 90M
                rate += 90M * 0.0001M;
                
                // Then 0.001% (0.00001) for each million over 90M
                decimal excessMillions = Math.Floor(millions - 90M);
                rate += excessMillions * 0.00001M;
            }
            
            return rate;
        }

        private (double validatedPopulation, string reason) ValidatePreWarPopulation(
            double preWarPop, 
            string stateName,
            List<double> allPreWarPops)
        {
            // Panama Canal Zone Rule: Use 10x 1945 population as max
            if (stateName == "Panama Canal Zone")
            {
                double pczMaxAllowed = InitialPopulation1945 * 10;
                if (preWarPop <= pczMaxAllowed)
                {
                    return (preWarPop, $"PASSED Panama Canal Zone Rule: Population {preWarPop:N0} is within range (0 to {pczMaxAllowed:N0})");
                }

                return (0, $"FAILED Panama Canal Zone Rule: Population {preWarPop:N0} exceeds 10x 1945 population ({pczMaxAllowed:N0})");
            }

            // Alaska Rule: Check population before Sino-American War losses
            if (stateName == "Alaska")
            {
                // Calculate pre-Sino-American War population (after first 121 years)
                double preWarGrowthRate = PopulationFormulas.PreWarFormulas.First().Value; // Use first formula's rate
                double preSinoWarPop = InitialPopulation1945 * Math.Pow(1 + preWarGrowthRate, 121);
                
                if (preSinoWarPop <= OtlMaxPopulation)
                {
                    return (0, $"FAILED Alaska Rule: Pre-Sino-American War population {preSinoWarPop:N0} must be larger than OTL Max {OtlMaxPopulation:N0}");
                }

                double alaskaMaxAllowed = OtlMaxPopulation * 5;
                if (preSinoWarPop <= alaskaMaxAllowed)
                {
                    return (preWarPop, $"PASSED Alaska Rule: Pre-Sino-American War population {preSinoWarPop:N0} is within range ({OtlMaxPopulation:N0} to {alaskaMaxAllowed:N0})");
                }

                return (0, $"FAILED Alaska Rule: Pre-Sino-American War population {preSinoWarPop:N0} exceeds 5x OTL Max ({alaskaMaxAllowed:N0})");
            }

            // Normal Red Rule validation for all other states
            if (preWarPop <= OtlMaxPopulation)
            {
                return (0, $"FAILED Rule 1: Population {preWarPop:N0} must be larger than OTL Max {OtlMaxPopulation:N0}");
            }

            double maxAllowed = OtlMaxPopulation * 5;
            if (preWarPop <= maxAllowed)
            {
                return (preWarPop, $"PASSED: Population {preWarPop:N0} is within acceptable range ({OtlMaxPopulation:N0} to {maxAllowed:N0})");
            }

            return (0, $"FAILED Rule 2: Population {preWarPop:N0} exceeds 5x OTL Max ({maxAllowed:N0})");
        }

        private decimal CalculateMedian(List<decimal> values)
        {
            var sortedValues = values.OrderBy(x => x).ToList();
            int count = sortedValues.Count;
            if (count == 0) return 0;
            
            if (count % 2 == 0)
                return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2;
            else
                return sortedValues[count / 2];
        }

        private decimal ConvertToDecimal(double value, string description)
        {
            try
            {
                if (value > 7.922816E+28)
                {
                    LogDebug($"WARNING: {description} {value:E2} exceeds decimal maximum. Capping at maximum value.");
                    return 7.922816E+28M;
                }
                return (decimal)value;
            }
            catch (OverflowException)
            {
                LogDebug($"WARNING: {description} {value:E2} caused overflow. Capping at maximum decimal value.");
                return 7.922816E+28M;
            }
        }

        private static double CalculateMedian(List<double> values)
        {
            var sortedValues = values.OrderBy(v => v).ToList();
            int count = sortedValues.Count;
            if (count == 0)
                return 0;
            
            if (count % 2 == 0)
                return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2;
            else
                return sortedValues[count / 2];
        }

        private static T CalculateMedian<T>(IEnumerable<T> values) where T : IComparable<T>
        {
            var sortedValues = values.OrderBy(v => v).ToList();
            int count = sortedValues.Count;
            if (count == 0)
                throw new ArgumentException("Cannot calculate median of an empty sequence");
            
            if (count % 2 == 0)
            {
                dynamic a = sortedValues[count / 2 - 1];
                dynamic b = sortedValues[count / 2];
                return (T)((a + b) / 2);
            }
            return sortedValues[count / 2];
        }
    }
}