using PopulationCalculator.Services;
using PopulationCalculator.Models;
using System.Globalization;

#region User Customization Settings

var states = ExcelSettingsReader.ReadStateSettings(@"C:\Filipe\FODD\Scripts\PopulationCalculator\PopulationCalculationData.xlsx");

foreach (var state in states)
{
    // Add historical rates to formulas
    PopulationFormulas.AddHistoricalRates(
        state.Rate4660, 
        state.Rate4670, 
        state.Rate5060,
        state.Rate5565, 
        state.Rate5070, 
        state.Rate6070,
        state.Rate7080);

    var calculator = new PopulationCalculator.Services.PopulationCalculator(
        state.InitialPopulation1945,
        state.ModernPopulation,
        state.OtlMaxPopulation);

    var results = calculator.CalculatePopulations(
        state.StateName,
        preWarPeriods: state.UseCustomPreWarPeriods ? state.PreWarPeriods : null,
        postWarPeriods: state.UseCustomPostWarPeriods ? 
            state.PostWarPeriods.Select(p => (p.years, p.rateModifier, (double)p.popChange)).ToList() : null,
        ghoulPeriods: state.UseCustomGhoulPeriods ? 
            state.GhoulPeriods.Select(p => (p.years, p.rateModifier, (double)p.popChange)).ToList() : null
    );

    if (results != null && results.Any())
    {
        string outputFile = (state.UseCustomPreWarPeriods || state.UseCustomPostWarPeriods || state.UseCustomGhoulPeriods) 
            ? $"{state.StateName}_custom.csv"
            : $"{state.StateName}_default.csv";
        FileHelper.WriteResults(results, outputFile);
    }
}

#endregion
