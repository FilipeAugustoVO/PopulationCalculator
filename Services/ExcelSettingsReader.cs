using OfficeOpenXml;
using PopulationCalculator.Models;

namespace PopulationCalculator.Services
{
    public class ExcelSettingsReader
    {
        public static CalculationSettings ReadSettings(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage(new FileInfo(filePath));
            var settings = new CalculationSettings();

            // Read Basic Settings sheet
            var basicSheet = package.Workbook.Worksheets["BasicSettings"];
            settings.InitialPopulation1945 = basicSheet.Cells["B2"].GetValue<double>();
            settings.ModernPopulation = basicSheet.Cells["B3"].GetValue<double>();
            settings.OtlMaxPopulation = basicSheet.Cells["B4"].GetValue<double>();

            // Read Growth Rates sheet
            var ratesSheet = package.Workbook.Worksheets["GrowthRates"];
            settings.Rate4660 = ratesSheet.Cells["B2"].GetValue<double>();
            settings.Rate4670 = ratesSheet.Cells["B3"].GetValue<double>();
            // ... read other rates

            // Read Custom Periods sheets
            var periodsSheet = package.Workbook.Worksheets["CustomPeriods"];
            settings.UseCustomPreWarPeriods = periodsSheet.Cells["B2"].GetValue<bool>();
            // ... read other settings and periods

            return settings;
        }

        public static List<StateSettings> ReadStateSettings(string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var states = new List<StateSettings>();
            
            using var package = new ExcelPackage(new FileInfo(filePath));
            var statesSheet = package.Workbook.Worksheets["States"];

            int rowCount = statesSheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                var state = new StateSettings
                {
                    StateName = statesSheet.Cells[row, 1].GetValue<string>(),
                    Region = statesSheet.Cells[row, 2].GetValue<string>(),
                    IsOffMap = statesSheet.Cells[row, 3].GetValue<string>()?.ToLower() == "off-map",
                    InitialPopulation1945 = statesSheet.Cells[row, 4].GetValue<double>(),
                    ModernPopulation = statesSheet.Cells[row, 5].GetValue<double>(),
                    OtlMaxPopulation = statesSheet.Cells[row, 6].GetValue<double>(),

                    // Historical growth rates
                    Rate4660 = statesSheet.Cells[row, 7].GetValue<double>(),
                    Rate4670 = statesSheet.Cells[row, 8].GetValue<double>(),
                    Rate5060 = statesSheet.Cells[row, 9].GetValue<double>(),
                    Rate5565 = statesSheet.Cells[row, 10].GetValue<double>(),
                    Rate5070 = statesSheet.Cells[row, 11].GetValue<double>(),
                    Rate6070 = statesSheet.Cells[row, 12].GetValue<double>(),
                    Rate7080 = statesSheet.Cells[row, 13].GetValue<double>(),

                    UseCustomPreWarPeriods = statesSheet.Cells[row, 14].GetValue<bool>(),
                    UseCustomPostWarPeriods = statesSheet.Cells[row, 15].GetValue<bool>(),
                    UseCustomGhoulPeriods = statesSheet.Cells[row, 16].GetValue<bool>(),
                    PreWarPeriods = ParsePreWarPeriods(statesSheet.Cells[row, 17].GetValue<string>()),
                    PostWarPeriods = ParsePostWarPeriods(statesSheet.Cells[row, 18].GetValue<string>()),
                    GhoulPeriods = ParseGhoulPeriods(statesSheet.Cells[row, 19].GetValue<string>())
                };

                if (!state.IsOffMap)
                    states.Add(state);
            }

            return states;
        }

        private static List<(int years, double rateModifier)> ParsePreWarPeriods(string periodsText)
        {
            var periods = new List<(int, double)>();
            if (string.IsNullOrWhiteSpace(periodsText)) return periods;

            var periodStrings = periodsText.Split(')').Where(p => !string.IsNullOrWhiteSpace(p));
            foreach (var period in periodStrings)
            {
                var parts = period.Trim('(', ')', ' ').Split(',');
                if (parts.Length >= 2)
                {
                    periods.Add((
                        int.Parse(parts[0]),
                        double.Parse(parts[1])
                    ));
                }
            }
            return periods;
        }

        private static List<(int years, double rateModifier, int popChange)> ParsePostWarPeriods(string periodsText)
        {
            var periods = new List<(int, double, int)>();
            if (string.IsNullOrWhiteSpace(periodsText)) return periods;

            var periodStrings = periodsText.Split(')').Where(p => !string.IsNullOrWhiteSpace(p));
            foreach (var period in periodStrings)
            {
                var parts = period.Trim('(', ')', ' ').Split(',');
                if (parts.Length >= 3)
                {
                    periods.Add((
                        int.Parse(parts[0]),
                        double.Parse(parts[1]),
                        int.Parse(parts[2])
                    ));
                }
            }
            return periods;
        }

        private static List<(int years, double rateModifier, int popChange)> ParseGhoulPeriods(string periodsText)
        {
            return ParsePostWarPeriods(periodsText);
        }
    }
}