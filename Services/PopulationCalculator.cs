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

        public List<PopulationResult> CalculatePopulations(
            string stateName,
            List<(int years, double rateModifier)>? preWarPeriods = null,
            List<(int years, double rateModifier, double popChange)>? postWarPeriods = null,
            List<(int years, double rateModifier, double popChange)>? ghoulPeriods = null)
        {
            // Append state calculations instead of clearing the file
            LogDebug($"\n=== Starting calculations for {stateName} ===\n");
            
            var results = new List<PopulationResult>();
            
            // Add initial population and log it
            results.Add(new PopulationResult
            {
                StateName = stateName,
                Year = 1945,
                Population = InitialPopulation1945,
                Description = "Initial Population"
            });
            
            LogDebug($"Initial Population (1945): {InitialPopulation1945:N0}");
            
            // Calculate pre-war population using growth rate from formulas
            double preWarPop = 0;

            // Try all growth rates
            foreach (var formula in PopulationFormulas.PreWarFormulas)
            {
                double growthRate = formula.Value;
                LogDebug($"\nTrying {formula.Key}:");
                
                preWarPop = CalculatePreWarPopulation(InitialPopulation1945, growthRate, preWarPeriods ?? DefaultPreWarPeriods);
                
                var (validatedPop, reason) = ValidatePreWarPopulation(preWarPop, stateName, new List<double>());
                LogDebug($"Validation result: {reason}\n");

                if (validatedPop > 0)
                {
                    preWarPop = validatedPop;
                    LogDebug($"Found valid population: {preWarPop:N0}");
                }
            }

            // After trying all formulas, if none worked, apply Red Rule
            if (preWarPop == 0)
            {
                // Try multipliers from x6 to x10
                for (int multiplier = 6; multiplier <= 10; multiplier++)
                {
                    double redRulePop = OtlMaxPopulation * multiplier;
                    LogDebug($"Trying Red Rule x{multiplier}: {redRulePop:N0}");
                    
                    var (validatedPop, reason) = ValidatePreWarPopulation(redRulePop, stateName, new List<double>());
                    LogDebug($"Validation result: {reason}");
                    
                    if (validatedPop > 0)
                    {
                        preWarPop = validatedPop;
                    }
                }

                // If still no valid population, use x5 of OTL max as per the corollary
                if (preWarPop == 0)
                {
                    preWarPop = OtlMaxPopulation * 5;
                    LogDebug($"Using Red Rule corollary (x5): {preWarPop:N0}");
                    
                    // Validate corollary result
                    var (validatedPop, reason) = ValidatePreWarPopulation(preWarPop, stateName, new List<double>());
                    LogDebug($"Validation result: {reason}");
                    
                    if (validatedPop > 0)
                    {
                        preWarPop = validatedPop;
                    }
                }
            }

            // Debug output
            LogDebug($"\nFinal Results:");
            LogDebug($"State: {stateName}");
            LogDebug($"Initial Pop: {InitialPopulation1945:N0}");
            LogDebug($"Pre-war Pop: {preWarPop:N0}");
            LogDebug($"OTL Max: {OtlMaxPopulation:N0}");

            // Continue with KEOFF calculations if we have a valid population
            if (preWarPop > 0)
            {
                results.Add(new PopulationResult
                {
                    StateName = stateName,
                    Year = 2077,
                    Population = preWarPop,
                    Description = "Pre-War Population"
                });

                // KEOFF calculations using safe decimal conversion
                decimal preWarPopDecimal = ConvertToDecimal(preWarPop, "Pre-war population");
                var keoffRate = CalculateKeoffRate(preWarPopDecimal);
                decimal survivingPop = preWarPopDecimal * (1M - keoffRate);
                
                results.Add(new PopulationResult
                {
                    StateName = stateName,
                    Year = 2077,
                    Population = (double)survivingPop,
                    Description = $"KEOFF Survivors ({keoffRate:P1} death rate)"
                });

                // Add initial ghoul population result
                decimal initialGhoulPop = survivingPop * 0.40M;
                results.Add(new PopulationResult
                {
                    StateName = stateName,
                    Year = 2077,
                    Population = (double)initialGhoulPop,
                    Description = "Initial Ghoul Population (40% of survivors)"
                });

                // Final ghoul population (already being calculated)
                double finalGhoulPop = CalculateGhoulPopulation(
                    (double)initialGhoulPop,
                    -0.001, // -0.1% yearly decline
                    ghoulPeriods ?? DefaultGhoulPeriods
                );

                // Use only the main ghoul stats method
                CalculateGhoulStats(results, finalGhoulPop, ghoulPeriods ?? DefaultGhoulPeriods);
            }

            return results;
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
            double preWarRate, // Add parameter for pre-war rate
            List<(int years, double rateModifier, double popChange)> periods)
        {
            double population = initialPop;
            
            // Only check negative modifiers for duplicates
            if (growthRate < 0)
            {
                // Use the pre-war formula's rate as base
                double reduction = Math.Abs(growthRate); // e.g., -12% becomes 0.12
                double actualRate = preWarRate * (1 - reduction); // e.g., preWarRate * (1 - 0.12)
                
                // Check if this matches any positive growth rate
                var positiveRates = PopulationFormulas.PostApocFormulas
                    .Where(f => f.Value > 0)
                    .Select(f => f.Value);

                if (positiveRates.Any(r => Math.Abs(r - actualRate) < 0.0001))
                {
                    return double.NaN; // Signal that this is a duplicate rate
                }

                growthRate = actualRate;
            }

            // Calculate population growth
            foreach (var (years, modifier, change) in periods)
            {
                population = population * Math.Pow(1 + (growthRate * modifier), years);
                population += change;
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

        private void CalculatePostWarProjections(List<string> results, double population, List<(int years, double rateModifier, int popChange)> periods)
        {
            // Placeholder for additional post-war projections logic
        }

        private void CalculateGhoulStats(List<PopulationResult> results, double population, List<(int years, double rateModifier, double popChange)> periods)
        {
            results.Add(new PopulationResult
            {
                StateName = results[0].StateName,  // Use the original state name instead of "Ghouls"
                Year = 2077 + periods.Sum(p => p.years),
                Population = population,
                Description = "Final Ghoul Population"
            });
        }

        private void CalculateAndOutputProjections(List<string> results, double postFyocPop)
        {
            // Placeholder for additional post-war projections logic
        }

        private void CalculateAndOutputMPStats(List<string> results, double postFyocPop)
        {
            // Placeholder for additional MP statistics logic
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
    }
}