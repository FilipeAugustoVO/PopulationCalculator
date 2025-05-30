namespace PopulationCalculator.Models;

public static class PopulationFormulas
{
    // Change from private to public
    public static readonly Dictionary<string, double> PreWarFormulas = new();

    public static void AddHistoricalRates(
        double rate4660, double rate4670, double rate5060,
        double rate5565, double rate5070, double rate6070,
        double rate7080)
    {
        PreWarFormulas.Clear();

        void AddRate(string key, double rate)
        {
            // Simply store the rate as provided - no conversion needed
            PreWarFormulas[key] = rate;
        }

        // Add rates
        AddRate("46-60 formula", rate4660);
        AddRate("46-70 formula", rate4670);
        AddRate("50-60 formula", rate5060);
        AddRate("55-65 formula", rate5565);
        AddRate("50-70 formula", rate5070);
        AddRate("60-70 formula", rate6070);
        AddRate("70-80s formula", rate7080);
    }

    public static readonly Dictionary<string, double> PostApocFormulas = new()
    {
        { "0.25% formula", 0.0025 },
        { "0.5% formula", 0.005 },
        { "0.75% formula", 0.0075 },
        { "1% formula", 0.01 },
        { "1.10% formula", 0.011 },
        { "1.25% formula", 0.0125 },
        { "1.5% formula", 0.015 },
        { "1.75% formula", 0.0175 },
        { "2% formula", 0.02 },
        { "-12% formula", -0.12 },
        { "-25% formula", -0.25 },
        { "-50% formula", -0.50 },
        { "-75% formula", -0.75 }
    };
}