namespace PopulationCalculator.Models;

public static class PopulationFormulas
{
    public static readonly Dictionary<string, double> PreWarFormulas = new()
    {
        { "0.25% formula", 0.25 / 100 },
        { "0.5% formula", 0.5 / 100 },
        { "0.75% formula", 0.75 / 100 },
        { "1% formula", 1.0 / 100 },
        { "1.10% formula", 1.1 / 100 },
        { "1.25% formula", 1.25 / 100 },
        { "1.5% formula", 1.5 / 100 },
        { "1.75% formula", 1.75 / 100 },
        { "2% formula", 2.0 / 100 }
    };

    public static void AddHistoricalRates(
        double rate4660, double rate4670, double rate5060,
        double rate5565, double rate5070, double rate6070,
        double rate7080)
    {
        // Debug logging to see actual values
        Console.WriteLine($"46-60 rate received: {rate4660}");
        
        var historicalRates = new Dictionary<string, double>
        {
            { "46-60 formula", rate4660 },  // Should be 5.38, not 53800
            { "46-70 formula", rate4670 },  // Should be 2.7763, not 27763
            { "50-60 formula", rate5060 },
            { "55-65 formula", rate5565 },
            { "50-70 formula", rate5070 },
            { "60-70 formula", rate6070 },
            { "70-80s formula", rate7080 }
        };

        foreach (var rate in historicalRates)
        {
            if (PreWarFormulas.ContainsKey(rate.Key))
            {
                PreWarFormulas[rate.Key] = rate.Value;
            }
            else
            {
                PreWarFormulas.Add(rate.Key, rate.Value);
            }
        }
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