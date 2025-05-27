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
        // Clear existing rates first
        PreWarFormulas.Clear();

        // Add rates with percentage conversion
        PreWarFormulas["46-60 formula"] = rate4660 / 100;
        PreWarFormulas["46-70 formula"] = rate4670 / 100;
        PreWarFormulas["50-60 formula"] = rate5060 / 100;
        PreWarFormulas["55-65 formula"] = rate5565 / 100;
        PreWarFormulas["50-70 formula"] = rate5070 / 100;
        PreWarFormulas["60-70 formula"] = rate6070 / 100;
        PreWarFormulas["70-80s formula"] = rate7080 / 100;
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