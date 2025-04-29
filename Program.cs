using PopulationCalculator.Services;
using PopulationCalculator.Helpers;
using PopulationCalculator.Models;  // Add this line at the top

#region User Customization Settings

#region Growth Rate Calculations
/*
To calculate these rates:
1. Get population data for both years
2. Use formula: rate = (Math.Pow((P2/P1), 1/years) - 1) * 100
   where: P1 = initial population
          P2 = final population
          years = time period
   Note: Result is already in percentage form (e.g., 2.88163 for 2.88163%)

Example for 46-60 formula:
P1 (1946) = initial population
P2 (1960) = final population
years = 14
*/

// Historical Growth Rates (To Be Calculated) - Enter as percentages
const double Rate4660 = 4.622;   // Growth rate 1946-1960
const double Rate4670 = 4.9348;  // Growth rate 1946-1970
const double Rate5060 = 5.801;    // Growth rate 1950-1960
const double Rate5565 = 6.959;    // Growth rate 1955-1965
const double Rate5070 = 5.612;   // Growth rate 1950-1970
const double Rate6070 = 5.303;    // Growth rate 1960-1970
const double Rate7080 = 4.8827;     // Growth rate 1970-1980
#endregion

// Population Settings
const double InitialPopulation1945 = 149000;
const double ModernPopulation = 3194176;
const double OtlMaxPopulation = 3194176;

// Growth Period Settings
bool useCustomPreWarPeriods = false;    // Enable custom pre-war periods
bool useCustomPostWarPeriods = true;   // Enable custom post-war periods
bool useCustomGhoulPeriods = true;     // Enable custom ghoul periods

// Custom Pre-War Growth Periods
var customPreWarPeriods = new List<(int years, double rateModifier)>
{
    (100, 1.0),  // 100 years full growth
    (21, 0.8),   // 21 years at 80%
    (11, 0.5)    // 11 years at 50%
};

// Custom Post-War Growth Periods
var customPostWarPeriods = new List<(int years, double rateModifier, int popChange)>
{
    (103, 1.0, 200),     
    (70, 1.0, 0) 

    // 
    //100, 1.0, 0),     // 100 years normal growth
    //(73, 0.8, 5000)   // 73 years at 80% + population influx
};


// Custom Ghoul Growth Periods
var customGhoulPeriods = new List<(int years, double rateModifier, int popChange)>
{
    (108, 1.0, 200),    // 167 years normal decline
    (50, 1.0, 500),    // 10 years at half decline + population influx
    (20, 1.0, 0)

    //
    //(167, 1.0, 0),     // 167 years normal decline
    // (10, 0.5, 1000)   // 10 years at half decline + population influx
};



// Output Settings
const string CustomOutputFile = "custom_results.txt";
const string DefaultOutputFile = "default_results.txt";
#endregion

// Add historical rates to formulas before calculator initialization
PopulationFormulas.AddHistoricalRates(
    Rate4660, Rate4670, Rate5060, 
    Rate5565, Rate5070, Rate6070, 
    Rate7080);

// Main program execution
var calculator = new PopulationCalculator.Services.PopulationCalculator(
    InitialPopulation1945, 
    ModernPopulation, 
    OtlMaxPopulation);

// Use null for default periods, custom periods if enabled
Console.WriteLine("Calculating populations with selected period settings...");
var results = calculator.CalculatePopulations(
    useCustomPreWarPeriods ? customPreWarPeriods : null,
    useCustomPostWarPeriods ? customPostWarPeriods : null,
    useCustomGhoulPeriods ? customGhoulPeriods : null
);

// Write results to appropriate file
string outputFile = (useCustomPreWarPeriods || useCustomPostWarPeriods || useCustomGhoulPeriods) 
    ? CustomOutputFile 
    : DefaultOutputFile;
FileHelper.WriteResults(results, outputFile);
