using OfficeOpenXml;
using PopulationCalculator.Models;
using System.Globalization;

namespace PopulationCalculator.Services
{
    public class ExcelSettingsReader
    {
        private static readonly CultureInfo ParsingCulture = CultureInfo.InvariantCulture;

#pragma warning disable CS0618
        static ExcelSettingsReader()
        {
            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            ExcelPackage.License.SetNonCommercialPersonal("Brazilian Slaughter");
        }
#pragma warning restore CS0618

        public static List<StateSettings> ReadStateSettings(string filePath)
        {
            var states = new List<StateSettings>();
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Settings file not found: {filePath}");
            
            using var package = new ExcelPackage(new FileInfo(filePath));
            var statesSheet = package.Workbook.Worksheets["States"] 
                ?? throw new InvalidOperationException("States sheet not found in Excel file");

            int rowCount = statesSheet.Dimension?.Rows ?? 0;
            if (rowCount < 2)
                throw new InvalidOperationException("No data rows found in States sheet");

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var stateName = statesSheet.Cells[row, 1].GetValue<string>();
                    bool isOffMap = bool.Parse(statesSheet.Cells[row, 2].Text);

                    // Skip validation for off-map states
                    if (isOffMap)
                        continue;

                    var state = new StateSettings
                    {
                        StateName = stateName,
                        IsOffMap = isOffMap,
                        InitialPopulation1945 = GetValidDouble(statesSheet.Cells[row, 3], "1945 population", stateName),
                        ModernPopulation = stateName == "Panama Canal Zone" ? 
        0 : // Panama Canal Zone doesn't need Modern population
        GetValidDouble(statesSheet.Cells[row, 4], "Modern population", stateName),
    OtlMaxPopulation = stateName == "Panama Canal Zone" ? 
        0 : // Panama Canal Zone doesn't need OTL Max population
        GetValidDouble(statesSheet.Cells[row, 5], "OTL Max population", stateName),

                        // Historical growth rates
                        Rate4660 = ValidateGrowthRate(statesSheet.Cells[row, 6].GetValue<double>(), "46-60", stateName),
                        Rate4670 = ValidateGrowthRate(statesSheet.Cells[row, 7].GetValue<double>(), "46-70", stateName),
                        Rate5060 = ValidateGrowthRate(statesSheet.Cells[row, 8].GetValue<double>(), "50-60", stateName),
                        Rate5565 = ValidateGrowthRate(statesSheet.Cells[row, 9].GetValue<double>(), "55-65", stateName),
                        Rate5070 = ValidateGrowthRate(statesSheet.Cells[row, 10].GetValue<double>(), "50-70", stateName),
                        Rate6070 = ValidateGrowthRate(statesSheet.Cells[row, 11].GetValue<double>(), "60-70", stateName),
                        Rate7080 = ValidateGrowthRate(statesSheet.Cells[row, 12].GetValue<double>(), "70-80", stateName),

                        UseCustomPreWarPeriods = statesSheet.Cells[row, 13].GetValue<bool>(),
                        UseCustomPostWarPeriods = statesSheet.Cells[row, 14].GetValue<bool>(),
                        UseCustomGhoulPeriods = statesSheet.Cells[row, 15].GetValue<bool>(),
                        PreWarPeriods = ParsePreWarPeriods(statesSheet.Cells[row, 16].GetValue<string>()),
                        PostWarPeriods = ParsePostWarPeriods(statesSheet.Cells[row, 17].GetValue<string>()),
                        GhoulPeriods = ParseGhoulPeriods(statesSheet.Cells[row, 18].GetValue<string>())
                    };

                    // Validate required fields
                    ValidateStateData(state);
                    states.Add(state);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error reading row {row}: {ex.Message}");
                }
            }

            return states;
        }

        private static void ValidateStateData(StateSettings state)
        {
            // Basic validation
            if (string.IsNullOrEmpty(state.StateName))
                throw new InvalidOperationException("State name cannot be empty");
            
            // Population validation    
            if (state.InitialPopulation1945 <= 0)
                throw new InvalidOperationException($"Invalid 1945 population for {state.StateName}");

            // Only validate Modern and OTL Max populations for non-Panama Canal Zone states
            if (state.StateName != "Panama Canal Zone")
            {
                if (state.ModernPopulation <= 0)
                    throw new InvalidOperationException($"Invalid Modern population for {state.StateName}");
                if (state.OtlMaxPopulation <= 0)
                    throw new InvalidOperationException($"Invalid OTL Max population for {state.StateName}");
            }
                
            // Custom periods validation
            if (state.UseCustomPreWarPeriods && !state.PreWarPeriods.Any())
                throw new InvalidOperationException($"Custom pre-war periods enabled but none provided");

            // Add validation for post-war and ghoul periods
            if (state.UseCustomPostWarPeriods && !state.PostWarPeriods.Any())
                throw new InvalidOperationException($"Custom post-war periods enabled but none provided for {state.StateName}");

            if (state.UseCustomGhoulPeriods && !state.GhoulPeriods.Any())
                throw new InvalidOperationException($"Custom ghoul periods enabled but none provided for {state.StateName}");
        }

        private static bool ValidatePeriodFormat(string periodsText)
        {
            // Updated patterns to handle negative decimals and spaces
            var preWarPattern = @"^\(\s*\d+\s*,\s*-?\d*\.?\d*\s*\)(\s*,\s*\(\s*\d+\s*,\s*-?\d*\.?\d*\s*\))*$";
            var postWarPattern = @"^\(\s*\d+\s*,\s*-?\d*\.?\d*\s*,\s*-?\d*\.?\d*\s*\)(\s*,\s*\(\s*\d+\s*,\s*-?\d*\.?\d*\s*,\s*-?\d*\.?\d*\s*\))*$";
            
            return System.Text.RegularExpressions.Regex.IsMatch(periodsText, preWarPattern) ||
                   System.Text.RegularExpressions.Regex.IsMatch(periodsText, postWarPattern);
        }

        private static List<T> ParsePeriods<T>(string periodsText, Func<string[], T> parser, string periodType)
        {
            var periods = new List<T>();
            
            // Return empty list for null, empty or whitespace
            if (string.IsNullOrWhiteSpace(periodsText)) 
                return periods;
                
            // Skip parsing if cell contains only "()" or similar empty patterns
            if (periodsText.Trim().Replace("(", "").Replace(")", "").Replace(" ", "").Length == 0)
                return periods;

            try
            {
                // Validate format before parsing
                if (!ValidatePeriodFormat(periodsText))
                    throw new FormatException($"Invalid {periodType} period format: {periodsText}");

                var periodStrings = periodsText.Split(')').Where(p => !string.IsNullOrWhiteSpace(p));
                foreach (var period in periodStrings)
                {
                    // Trim spaces when splitting parts
                    var parts = period.Trim('(', ')', ' ').Split(',').Select(p => p.Trim()).ToArray();
                    if (parts.Any() && !string.IsNullOrWhiteSpace(parts[0]))
                    {
                        periods.Add(parser(parts));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FormatException($"Error parsing {periodType} periods: {ex.Message}");
            }

            return periods;
        }

        private static List<(int years, double rateModifier)> ParsePreWarPeriods(string periodsText)
        {
            return ParsePeriods(periodsText, parts => (
                int.Parse(parts[0], ParsingCulture),
                double.Parse(parts[1], ParsingCulture)
            ), "pre-war");
        }

        private static List<(int years, double rateModifier, double popChange)> ParsePostWarPeriods(string periodsText)
        {
            return ParsePeriods(periodsText, parts => (
                int.Parse(parts[0], ParsingCulture),
                double.Parse(parts[1], ParsingCulture),
                double.Parse(parts[2], ParsingCulture)  // Changed from int.Parse to double.Parse
            ), "post-war");
        }

        private static List<(int years, double rateModifier, double popChange)> ParseGhoulPeriods(string periodsText)
        {
            // Ghoul periods use the same format as post-war periods
            return ParsePostWarPeriods(periodsText);
        }

        private static double GetValidDouble(ExcelRange cell, string fieldName, string stateName)
        {
            var value = cell.GetValue<double>();
            if (value <= 0)
                throw new InvalidOperationException($"Invalid {fieldName} for {stateName}: {value}");
            return value;
        }

        private static double ValidateGrowthRate(double rate, string periodName, string stateName)
        {
            if (double.IsNaN(rate) || double.IsInfinity(rate))
                throw new InvalidOperationException(
                    $"Invalid growth rate for {periodName} in {stateName}: {rate}");
            
            // If the rate is greater than 1, assume it's a percentage and convert it
            if (Math.Abs(rate) > 1)
                rate = rate / 100;
                
            return rate;
        }
    }
}