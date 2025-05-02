using PopulationCalculator.Services;
using PopulationCalculator.Models;

#region User Customization Settings

var settings = ExcelSettingsReader.ReadSettings("Settings.xlsx");

// Add historical rates to formulas
PopulationFormulas.AddHistoricalRates(
    settings.Rate4660, 
    settings.Rate4670, 
    settings.Rate5060,
    settings.Rate5565, 
    settings.Rate5070, 
    settings.Rate6070,
    settings.Rate7080);

var calculator = new PopulationCalculator.Services.PopulationCalculator(
    settings.InitialPopulation1945,
    settings.ModernPopulation,
    settings.OtlMaxPopulation);

var results = calculator.CalculatePopulations(
    settings.UseCustomPreWarPeriods ? settings.CustomPreWarPeriods : null,
    settings.UseCustomPostWarPeriods ? settings.CustomPostWarPeriods : null,
    settings.UseCustomGhoulPeriods ? settings.CustomGhoulPeriods : null
);

// Write results to appropriate file
string outputFile = (settings.UseCustomPreWarPeriods || settings.UseCustomPostWarPeriods || settings.UseCustomGhoulPeriods) 
    ? settings.CustomOutputFile 
    : settings.DefaultOutputFile;
FileHelper.WriteResults(results, outputFile);
