using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using PopulationCalculator.Models;

namespace PopulationCalculator.Services
{
    public class PopulationCalculator
    {
        private readonly double InitialPopulation1945;
        private readonly double ModernPopulation;
        private readonly double OtlMaxPopulation;

        private readonly NumberFormatInfo nfi = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

        public PopulationCalculator(
            double initialPopulation1945,
            double modernPopulation,
            double otlMaxPopulation)
        {
            InitialPopulation1945 = initialPopulation1945;
            ModernPopulation = modernPopulation;
            OtlMaxPopulation = otlMaxPopulation;
        }

        // Default growth periods
        private readonly List<(int years, double rateModifier)> DefaultPreWarPeriods = new()
        {
            (121, 1.0),  // Full growth for 121 years
            (10, 0.5)    // Half growth for last 10 years
        };

        private readonly List<(int years, double rateModifier, int popChange)> DefaultPostWarPeriods = new()
        {
            (173, 1.0, 0)  // 173 years of post-war growth
        };

        private readonly List<(int years, double rateModifier, int popChange)> DefaultGhoulPeriods = new()
        {
            (177, 1.0, 0)  // 177 years of ghoul "growth" at -0.1% with no flat population change
        };

        public List<string> CalculatePopulations(
            List<(int years, double rateModifier)>? preWarPeriods = null,
            List<(int years, double rateModifier, int popChange)>? postWarPeriods = null,
            List<(int years, double rateModifier, int popChange)>? ghoulPeriods = null)
        {
            var results = new List<string>();
            var validationResults = new List<(double pop, string reason)>();

            // Process all formulas first, storing validation results
            foreach (var formula in PopulationFormulas.PreWarFormulas)
            {
                // Calculate pre-war population
                double preWarPop = CalculatePreWarPopulation(InitialPopulation1945, formula.Value, preWarPeriods ?? DefaultPreWarPeriods);

                // Validate but don't apply Red Rule yet
                var (validatedPop, reason) = ValidatePreWarPopulation(preWarPop, new List<double>());
                validationResults.Add((preWarPop, reason));

                if (validatedPop > 0)
                {
                    // Output formula results as normal if valid
                    results.Add($"\nFormula: {formula.Key}");
                    results.Add($"Pre-war population: {preWarPop:N0}");
                    results.Add($"Validation Status:");
                    results.Add($"  {reason.Replace("\n", "\n  ")}");  // Indent multiple lines

                    results.Add($"Final pre-war population: {validatedPop:N0}");

                    // Create list to store Base MP values
                    var baseMpValues = new List<decimal>();

                    // KEOFF calculations with maximum precision
                    decimal preWarPopDecimal = (decimal)validatedPop;
                    var keoffRate = CalculateKeoffRate(preWarPopDecimal);
                    decimal survivingPop = preWarPopDecimal * (1M - keoffRate);

                    results.Add($"KEOFF death rate: {keoffRate:P3}");
                    results.Add($"Surviving population: {survivingPop:N4}");

                    // Calculate Ghoul population
                    decimal initialGhoulPop = survivingPop * 0.40M; // 40% of KEOFF survivors
                    double finalGhoulPop = CalculateGhoulPopulation(
                        (double)initialGhoulPop,
                        -0.001, // -0.1% yearly decline
                        ghoulPeriods ?? DefaultGhoulPeriods
                    );

                    // FYOC calculations with precision
                    var fyocLossRate = CalculateFyocLossRate((double)survivingPop);
                    decimal postFyocPop = survivingPop * (1M - (decimal)fyocLossRate);

                    results.Add($"FYOC loss rate: {fyocLossRate:P1}");
                    results.Add($"Post-FYOC population: {postFyocPop:N6}");  // Show 6 decimal places

                    // Post-war calculations
                    results.Add("\nPost-War Population Projections:");

                    foreach (var postFormula in PopulationFormulas.PostApocFormulas)
                    {
                        var postPeriods = postWarPeriods ?? DefaultPostWarPeriods;
                        double finalPop;

                        if (postFormula.Value < 0)
                        {
                            // For negative rates, calculate direct reduction from post-FYOC population
                            double reduction = Math.Abs(postFormula.Value);
                            finalPop = (double)postFyocPop * (1 - reduction);
                        }
                        else
                        {
                            // For positive rates, use normal post-war calculation
                            finalPop = CalculatePostWarPopulation(
                                (double)postFyocPop,
                                postFormula.Value,
                                formula.Value,
                                postPeriods
                            );
                        }

                        // Regular calculation code continues...
                        decimal baseManpower = (decimal)(finalPop / 10.0);
                        baseMpValues.Add(baseManpower); // Store Base MP value

                        int onMapMp = (int)(baseManpower / 1000.0M);
                        decimal fullThousands = baseManpower / 1000.0M;
                        decimal offMapMp = fullThousands - Math.Floor(fullThousands);
                        decimal dailyOffMapMp = offMapMp / 100.0M;

                        // Format outputs
                        string formattedPop = finalPop.ToString("#,##0.000000", nfi);
                        string formattedBase = baseManpower.ToString("#,##0.000000", nfi);

                        // Format off-map values without rounding
                        var offMapStr = offMapMp.ToString(nfi).TrimStart('0').TrimStart(',');
                        var dailyStr = dailyOffMapMp.ToString(nfi).TrimStart('0').TrimStart(',');

                        // Ensure consistent precision - 6 for off-map, 8 for daily
                        string formattedOffMap = $"0,{offMapStr.Substring(0, Math.Min(6, offMapStr.Length))}";
                        string formattedDaily = $"0,{dailyStr.Substring(0, Math.Min(8, dailyStr.Length))}";

                        results.Add($"    {postFormula.Key,-15} : {formattedPop}");
                        results.Add($"        - Base MP       : {formattedBase}");
                        results.Add($"        - On-map MP     : {onMapMp}");
                        results.Add($"        - Off-map MP    : {formattedOffMap}");
                        results.Add($"        - Daily Off-map : {formattedDaily}");
                    }

                    // Add statistics after all formulas have been processed
                    if (baseMpValues.Any())
                    {
                        decimal average = baseMpValues.Average();
                        decimal median = CalculateMedian(baseMpValues);

                        results.Add($"\nBase MP Statistics for this Pre-War Formula:");
                        results.Add($"    Average: {average.ToString("#,##0.000000", nfi)}");
                        results.Add($"    Median : {median.ToString("#,##0.000000", nfi)}");
                        results.Add("");

                        // Add Ghoul statistics after Base MP Statistics
                        decimal ghoulOffMapMP = (decimal)(finalGhoulPop / 10000.0);   // Calculate total Ghoul MP
                        decimal ghoulDailyMP = ghoulOffMapMP / 100.0M;              // Daily MP is 1% of total MP

                        results.Add($"\nGhoul MP Statistics:");
                        results.Add($"    Initial Population: {initialGhoulPop.ToString("#,##0.000000", nfi)}");
                        results.Add($"    Final Population : {finalGhoulPop.ToString("#,##0.000000", nfi)}");

                        // Format MP values
                        string formattedGhoulMP = ghoulOffMapMP.ToString("#,##0.000000", nfi);
                        string formattedGhoulDaily = $"0,{ghoulDailyMP.ToString(nfi).Split(',')[1].Substring(0, 8)}";

                        results.Add($"    Off-map MP       : {formattedGhoulMP}");
                        results.Add($"    Daily Off-map    : {formattedGhoulDaily}");
                        results.Add("");
                    }

                    results.Add("----------------------------------------");
                }
            }

            // After all formulas, check if Red Rule should be applied
            if (!validationResults.Any(r => r.pop <= OtlMaxPopulation * 10 && r.pop > OtlMaxPopulation))
            {
                double redRulePop = OtlMaxPopulation * 5;
                var baseMpValues = new List<decimal>();

                // Calculate 1945-2077 growth rate
                double growthRate = (Math.Pow((redRulePop / InitialPopulation1945), 1.0 / 132) - 1);
                double yearlyPercentage = growthRate * 100;

                // Add headers with growth rate prominently displayed
                results.Add("\nRed Rule Corollary Formula");
                results.Add($"All pre-war populations were invalid.");
                results.Add($"Using 5x OTL Max instead: {redRulePop.ToString("#,##0.000000", nfi)}");
                results.Add($"Pre-war yearly growth rate: {yearlyPercentage:N3}%"); // Added this line
                results.Add($"Final pre-war population: {redRulePop.ToString("#,##0.000000", nfi)}");

                // KEOFF calculations
                decimal preWarPopDecimal = (decimal)redRulePop;
                var keoffRate = CalculateKeoffRate(preWarPopDecimal);
                decimal survivingPop = preWarPopDecimal * (1M - keoffRate);

                results.Add($"KEOFF death rate: {keoffRate:P3}");
                results.Add($"Surviving population: {survivingPop:N4}");

                // Calculate Ghoul population
                decimal initialGhoulPop = survivingPop * 0.40M; // 40% of KEOFF survivors
                double finalGhoulPop = CalculateGhoulPopulation(
                    (double)initialGhoulPop,
                    -0.001, // -0.1% yearly decline
                    ghoulPeriods ?? DefaultGhoulPeriods
                );

                // FYOC calculations
                var fyocLossRate = CalculateFyocLossRate((double)survivingPop);
                decimal postFyocPop = survivingPop * (1M - (decimal)fyocLossRate);

                results.Add($"FYOC loss rate: {fyocLossRate:P1}");
                results.Add($"Post-FYOC population: {postFyocPop:N6}");
                results.Add("");

                results.Add("Post-War Population Projections:");

                foreach (var postFormula in PopulationFormulas.PostApocFormulas)
                {
                    var postPeriods = postWarPeriods ?? DefaultPostWarPeriods;
                    double finalPop;

                    if (postFormula.Value < 0)
                    {
                        // For negative rates, use calculated pre-war rate
                        finalPop = CalculatePostWarPopulation(
                            (double)postFyocPop,
                            postFormula.Value,
                            growthRate,
                            postPeriods
                        );
                    }
                    else
                    {
                        // For positive rates, continue using normal calculation
                        finalPop = CalculatePostWarPopulation(
                            (double)postFyocPop,
                            postFormula.Value,
                            0,  // Don't use pre-war rate for positive formulas
                            postPeriods
                        );
                    }

                    // Regular calculation code continues...
                    decimal baseManpower = (decimal)(finalPop / 10.0);
                    baseMpValues.Add(baseManpower);

                    int onMapMp = (int)(baseManpower / 1000.0M);
                    decimal fullThousands = baseManpower / 1000.0M;
                    decimal offMapMp = fullThousands - Math.Floor(fullThousands);
                    decimal dailyOffMapMp = offMapMp / 100.0M;

                    string formattedPop = finalPop.ToString("#,##0.000000", nfi);
                    string formattedBase = baseManpower.ToString("#,##0.000000", nfi);

                    var offMapStr = offMapMp.ToString(nfi).TrimStart('0').TrimStart(',');
                    var dailyStr = dailyOffMapMp.ToString(nfi).TrimStart('0').TrimStart(',');

                    string formattedOffMap = $"0,{offMapStr.Substring(0, Math.Min(6, offMapStr.Length))}";
                    string formattedDaily = $"0,{dailyStr.Substring(0, Math.Min(8, dailyStr.Length))}";

                    results.Add($"    {postFormula.Key,-15} : {formattedPop}");
                    results.Add($"        - Base MP       : {formattedBase}");
                    results.Add($"        - On-map MP     : {onMapMp}");
                    results.Add($"        - Off-map MP    : {formattedOffMap}");
                    results.Add($"        - Daily Off-map : {formattedDaily}");
                }

                // Add Base MP Statistics
                if (baseMpValues.Any())
                {
                    decimal average = baseMpValues.Average();
                    decimal median = CalculateMedian(baseMpValues);

                    results.Add($"\nBase MP Statistics for this Pre-War Formula:");
                    results.Add($"    Average: {average.ToString("#,##0.000000", nfi)}");
                    results.Add($"    Median : {median.ToString("#,##0.000000", nfi)}");
                    results.Add("");

                    // Add Ghoul statistics
                    decimal ghoulOffMapMP = (decimal)(finalGhoulPop / 10000.0);
                    decimal ghoulDailyMP = ghoulOffMapMP / 100.0M;

                    results.Add($"\nGhoul MP Statistics:");
                    results.Add($"    Initial Population: {initialGhoulPop.ToString("#,##0.000000", nfi)}");
                    results.Add($"    Final Population : {finalGhoulPop.ToString("#,##0.000000", nfi)}");

                    string formattedGhoulMP = ghoulOffMapMP.ToString("#,##0.000000", nfi);
                    string formattedGhoulDaily = $"0,{ghoulDailyMP.ToString(nfi).Split(',')[1].Substring(0, 8)}";

                    results.Add($"    Off-map MP       : {formattedGhoulMP}");
                    results.Add($"    Daily Off-map    : {formattedGhoulDaily}");
                    results.Add("");
                }

                results.Add("----------------------------------------");
            }

            return results;
        }

        private double CalculatePreWarPopulation(
            double initialPop, 
            double growthRate, 
            List<(int years, double rateModifier)> periods)
        {
            double population = initialPop;
            
            foreach (var (years, modifier) in periods)
            {
                var adjustedRate = growthRate * modifier;
                population *= Math.Pow(1 + adjustedRate, years);
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
            List<(int years, double rateModifier, int popChange)> periods)
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
            List<(int years, double rateModifier, int popChange)> periods)
        {
            double population = initialPop;

            foreach (var (years, modifier, change) in periods)
            {
                var adjustedRate = declineRate * modifier;
                population *= Math.Pow(1 + adjustedRate, years);
                population += change;
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

        private (double validatedPopulation, string reason) ValidatePreWarPopulation(double preWarPop, List<double> allPreWarPops)
        {
            // Rule 1: Must be larger than OTL Max
            if (preWarPop <= OtlMaxPopulation)
                return (0, $"FAILED Rule 1: Population {preWarPop:N0} must be larger than OTL Max {OtlMaxPopulation:N0}");

            // Rule 2: Must be between OTL Max and 5x OTL Max
            double maxAllowed = OtlMaxPopulation * 5;
            if (preWarPop <= maxAllowed)
                return (preWarPop, $"PASSED: Population {preWarPop:N0} is within acceptable range ({OtlMaxPopulation:N0} to {maxAllowed:N0})");

            // Red Rule Check: Are there any valid populations?
            bool anyValidPopulations = allPreWarPops.Any(pop => pop > OtlMaxPopulation && pop <= maxAllowed);
            
            if (!anyValidPopulations)
            {
                // Try multipliers from 6x to 10x
                for (int multiplier = 6; multiplier <= 10; multiplier++)
                {
                    double currentMax = OtlMaxPopulation * multiplier;
                    if (preWarPop <= currentMax)
                        return (preWarPop, 
                            $"RED RULE APPLIED: No valid populations under 5x. Using {multiplier}x multiplier.\n" +
                            $"Population {preWarPop:N0} is within {multiplier}x limit ({currentMax:N0})");
                }

                // Beyond 10x, use Red Rule Corollary
                return (OtlMaxPopulation * 5, 
                    $"RED RULE COROLLARY: Population {preWarPop:N0} exceeds 10x OTL Max.\n" +
                    $"Using 5x OTL Max instead: {maxAllowed:N0}");
            }

            // Some populations are valid, so this one is invalid
            return (0, 
                $"FAILED Rule 2: Population {preWarPop:N0} exceeds 5x OTL Max ({maxAllowed:N0})\n" +
                "Other valid populations exist, so Red Rule does not apply");
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

        private void CalculateGhoulStats(List<string> results, double population, List<(int years, double rateModifier, int popChange)> periods)
        {
            // Placeholder for additional ghoul stats calculation logic
        }

        private void CalculateAndOutputProjections(List<string> results, double postFyocPop)
        {
            // Placeholder for additional post-war projections logic
        }

        private void CalculateAndOutputMPStats(List<string> results, double postFyocPop)
        {
            // Placeholder for additional MP statistics logic
        }

        private void CalculateGhoulStatistics(List<string> results, double redRulePop, List<(int years, double rateModifier, int popChange)> ghoulPeriods)
        {
            // Placeholder for additional ghoul statistics logic
        }

        private void CalculateAndAddGhoulStats(List<string> results, double redRulePop, List<(int years, double rateModifier, int popChange)> ghoulPeriods)
        {
            // Placeholder for additional ghoul statistics logic
        }
    }
}